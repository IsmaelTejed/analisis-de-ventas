using ADV.Application.Interface;
using ADV.Application.Repositories_Dwh;
using ADV.Domain.Entities.Api;
using ADV.Domain.Entities.DB;
using ADV.Domain.Entities.Dimencion;
using ADV.Domain.Entities.Facts;
using Microsoft.Extensions.Logging;


namespace ADV.Application.Services
{
    public class EtlService : IEtlService
    {
        private readonly ILogger<EtlService> _logger;

        private readonly IExtractor<DbSales> _dbSalesExtractor;
        private readonly IExtractor<DBSales> _csvSalesExtractor;
        private readonly IExtractor<DBProducts> _csvProductsExtractor;
        private readonly IExtractor<ApiProducts> _apiProductsExtractor;
        private readonly IExtractor<CsvCustomers> _csvCustomersExtractor;
        private readonly IExtractor<ApiCustomers> _apiCustomersExtractor;
        private readonly IDimProductRepository _dimProductRepo;
        private readonly IDimCustomersRepository _dimCustomerRepo;
        private readonly IDimDateRepository _dimDateRepo;
        private readonly IDimStatusRepository _dimStatusRepo;
        private readonly IFactSalesRepository _factSalesRepo;

        public EtlService(
            ILogger<EtlService> logger,
            IExtractor<DbSales> dbSalesExtractor,
            IExtractor<DBSales> csvSalesExtractor,
            IExtractor<DBProducts> csvProductsExtractor,
            IExtractor<ApiProducts> apiProductsExtractor,
            IExtractor<CsvCustomers> csvCustomersExtractor,
            IExtractor<ApiCustomers> apiCustomersExtractor,
            IDimProductRepository dimProductRepo,
            IDimCustomersRepository dimCustomerRepo,
            IDimDateRepository dimDateRepo,
            IDimStatusRepository dimStatusRepo,
            IFactSalesRepository factSalesRepo)
        {
            _logger = logger;
            _dbSalesExtractor = dbSalesExtractor;
            _csvSalesExtractor = csvSalesExtractor;
            _csvProductsExtractor = csvProductsExtractor;
            _apiProductsExtractor = apiProductsExtractor;
            _csvCustomersExtractor = csvCustomersExtractor;
            _apiCustomersExtractor = apiCustomersExtractor;
            _dimProductRepo = dimProductRepo;
            _dimCustomerRepo = dimCustomerRepo;
            _dimDateRepo = dimDateRepo;
            _dimStatusRepo = dimStatusRepo;
            _factSalesRepo = factSalesRepo;
        }

        public async Task RunEtlProcessAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation(">>> INICIANDO PROCESO ETL: CARGA DE DIMENSIONES Y HECHOS <<<");

            try
            {
                var sqlSales = await _dbSalesExtractor.ExtractAsync();
                var csvSales = await _csvSalesExtractor.ExtractAsync();

                var csvProducts = await _csvProductsExtractor.ExtractAsync();
                var apiProducts = await _apiProductsExtractor.ExtractAsync();

                var csvCustomers = await _csvCustomersExtractor.ExtractAsync();
                var apiCustomers = await _apiCustomersExtractor.ExtractAsync();

                _logger.LogInformation("Extracción completada. Datos en memoria.");


                await LoadDimProducts(csvProducts, apiProducts);

                await LoadDimCustomers(csvCustomers, apiCustomers);

                var allSales = sqlSales.Concat(csvSales.Select(MapCsvToDbSales)).ToList();

                await LoadDimStatus(allSales);

                await LoadDimDate(allSales);


                _logger.LogInformation("Dimensiones cargadas. Iniciando carga de FactSales...");

                await LoadFactSales(allSales);

                _logger.LogInformation(">>> PROCESO ETL COMPLETADO EXITOSAMENTE <<<");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ERROR FATAL durante el proceso ETL.");
            }
        }

        /// <summary>
        /// Convierte una venta de CSV (CsvSales) a la entidad común (DbSales).
        /// Esto permite tratar ambas fuentes de ventas como una sola lista.
        /// </summary>
        private DbSales MapCsvToDbSales(DBSales csv)
        {
            return new DbSales
            {
                OrderID = csv.OrderID,
                CustomerID = csv.CustomerID,
                OrderDate = csv.OrderDate,
                Status = csv.Status,
                ProductID = csv.ProductID,
                Quantity = csv.Quantity,
                Price = csv.Price
            };
        }

        private async Task LoadDimProducts(IEnumerable<DBProducts> csvProds, IEnumerable<ApiProducts> apiProds)
        {
            var existingIds = (await _dimProductRepo.GetAll())
                              .Select(p => p.ProductId)
                              .ToHashSet();

            var newProducts = new List<DimProducts>();

            var fromCsv = csvProds
                .Where(p => !existingIds.Contains(p.ProductID))
                .Select(p => new DimProducts
                {
                    ProductId = p.ProductID,
                    ProductName = p.ProductName,
                    Category = p.Category,
                    Price = p.Price
                });
            newProducts.AddRange(fromCsv);

            foreach (var p in fromCsv) existingIds.Add(p.ProductId);

            var fromApi = apiProds
                .Where(p => !existingIds.Contains(p.Id))
                .Select(p => new DimProducts
                {
                    ProductId = p.Id,
                    ProductName = p.Title,
                    Category = p.Category,
                    Price = p.Price
                });
            newProducts.AddRange(fromApi);

            if (newProducts.Any())
            {
                await _dimProductRepo.SaveAll(newProducts.ToArray());
                _logger.LogInformation("DimProducts: {Count} nuevos registros insertados.", newProducts.Count);
            }
            else
            {
                _logger.LogInformation("DimProducts: No hay nuevos registros.");
            }
        }

        private async Task LoadDimCustomers(IEnumerable<CsvCustomers> csvCusts, IEnumerable<ApiCustomers> apiCusts)
        {
            var existingIds = (await _dimCustomerRepo.GetAll())
                              .Select(c => c.CustomerId)
                              .ToHashSet();

            var newCustomers = new List<DimCustomers>();

            var fromCsv = csvCusts
                .Where(c => !existingIds.Contains(c.CustomerID))
                .Select(c => new DimCustomers
                {
                    CustomerId = c.CustomerID,
                    FirstName = c.FirstName,
                    LastName = c.LastName,
                    Email = c.Email,
                    Phone = c.Phone,
                    City = c.City,
                    Country = c.Country
                });
            newCustomers.AddRange(fromCsv);

            foreach (var c in fromCsv) existingIds.Add(c.CustomerId);

            var fromApi = apiCusts
                .Where(c => !existingIds.Contains(c.Id))
                .Select(c => new DimCustomers
                {
                    CustomerId = c.Id,
                    FirstName = c.Name,
                    Email = c.Email,
                    Country = c.Country,
                    City = "Unknown",
                    Phone = "Unknown"
                });
            newCustomers.AddRange(fromApi);

            if (newCustomers.Any())
            {
                await _dimCustomerRepo.SaveAll(newCustomers.ToArray());
                _logger.LogInformation("DimCustomers: {Count} nuevos registros insertados.", newCustomers.Count);
            }
        }

        private async Task LoadDimStatus(IEnumerable<DbSales> sales)
        {
            var statuses = sales.Select(s => s.Status)
                                .Where(s => !string.IsNullOrEmpty(s))
                                .Distinct();

            var existingStatuses = (await _dimStatusRepo.GetAll())
                                   .Select(s => s.Status)
                                   .ToHashSet();

            var statusToLoad = statuses
                .Where(status => !existingStatuses.Contains(status))
                .Select(status => new DimStatus { Status = status })
                .ToArray();

            if (statusToLoad.Any())
            {
                await _dimStatusRepo.SaveAll(statusToLoad);
                _logger.LogInformation("DimStatus: {Count} nuevos registros insertados.", statusToLoad.Length);
            }
        }

        private async Task LoadDimDate(IEnumerable<DbSales> sales)
        {
            var dates = sales.Select(s => s.OrderDate.Date).Distinct();

            var existingDateKeys = (await _dimDateRepo.GetAll())
                                   .Select(d => d.DateKey)
                                   .ToHashSet();

            var newDates = dates
                .Select(d => new
                {
                    DateObj = d,
                    Key = (d.Year * 10000) + (d.Month * 100) + d.Day
                })
                .Where(x => !existingDateKeys.Contains(x.Key))
                .Select(x => new DimDate
                {
                    DateKey = x.Key,
                    CompleteDate = x.DateObj,
                    Year = x.DateObj.Year,
                    Month = x.DateObj.Month,
                    MonthName = x.DateObj.ToString("MMMM")
                })
                .ToArray();

            if (newDates.Any())
            {
                await _dimDateRepo.SaveAll(newDates);
                _logger.LogInformation("DimDate: {Count} nuevos registros insertados.", newDates.Length);
            }
        }

        private async Task LoadFactSales(IEnumerable<DbSales> sales)
        {
            _logger.LogInformation("Iniciando carga de FactSales... Creando diccionarios de búsqueda.");


            var productsList = await _dimProductRepo.GetAll();
            var customersList = await _dimCustomerRepo.GetAll();
            var statusList = await _dimStatusRepo.GetAll();
            var dateList = await _dimDateRepo.GetAll();

            var productMap = productsList.ToDictionary(k => k.ProductId, v => v.ProductKey);
            var customerMap = customersList.ToDictionary(k => k.CustomerId, v => v.CustomerKey);
            var statusMap = statusList.ToDictionary(k => k.Status, v => v.StatusKey);

            var dateMap = dateList.ToDictionary(k => k.CompleteDate.Date, v => v.DateKey);

            var factsToLoad = sales
                .Where(s => s.ProductID.HasValue && s.CustomerID.HasValue && !string.IsNullOrEmpty(s.Status))
                .Select(sale => new
                {
                    Sale = sale,
                    ProductKey = productMap.GetValueOrDefault(sale.ProductID!.Value),
                    CustomerKey = customerMap.GetValueOrDefault(sale.CustomerID!.Value),
                    StatusKey = statusMap.GetValueOrDefault(sale.Status!),
                    DateKey = dateMap.GetValueOrDefault(sale.OrderDate.Date)
                })

                .Where(x => x.ProductKey != 0 && x.CustomerKey != 0 && x.DateKey != 0 && x.StatusKey != 0)
                .Select(x => new FactSales
                {
                    ProductKey = x.ProductKey,
                    CustomerKey = x.CustomerKey,
                    StatusKey = x.StatusKey,
                    DateKey = x.DateKey,
                    Quantity = x.Sale.Quantity,
                    TotalPrice = x.Sale.Price
                })
                .ToArray();

            if (factsToLoad.Any())
            {
                _logger.LogInformation("Insertando {Count} hechos en FactSales...", factsToLoad.Length);
                await _factSalesRepo.SaveAll(factsToLoad);
                _logger.LogInformation("Carga de FactSales completada exitosamente.");
            }
            else
            {
                _logger.LogWarning("No se generaron hechos para insertar. Verifique las coincidencias de fechas o IDs.");
            }
        }
    }
}