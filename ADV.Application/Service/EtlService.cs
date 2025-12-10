using ADV.Application.Interface;
using ADV.Application.Repositories_Dwh;
using ADV.Domain.Entities.Api;
using ADV.Domain.Entities.CSV;
using ADV.Domain.Entities.DB;
using ADV.Domain.Entities.Dimencion;
using ADV.Domain.Entities.Facts;
using Microsoft.Extensions.Logging;


namespace ADV.Application.Services
{
    public class EtlService : IEtlService
    {
        private readonly ILogger<EtlService> _logger;

        // FUENTES 
        private readonly IExtractor<DbSales> _dbSalesExtractor;
        private readonly IExtractor<CsvSales> _csvSalesExtractor;
        private readonly IExtractor<Product> _csvProductsExtractor;
        private readonly IExtractor<ApiProducts> _apiProductsExtractor;
        private readonly IExtractor<Customer> _csvCustomersExtractor;
        private readonly IExtractor<ApiCustomers> _apiCustomersExtractor;

        // DESTINO 
        private readonly IDimProductRepository _dimProductRepo;
        private readonly IDimCustomersRepository _dimCustomerRepo;
        private readonly IDimDateRepository _dimDateRepo;
        private readonly IDimStatusRepository _dimStatusRepo;
        private readonly IFactSalesRepository _factSalesRepo;

        public EtlService(
            ILogger<EtlService> logger,
            IExtractor<DbSales> dbSalesExtractor,
            IExtractor<CsvSales> csvSalesExtractor,
            IExtractor<Product> csvProductsExtractor,
            IExtractor<ApiProducts> apiProductsExtractor,
            IExtractor<Customer> csvCustomersExtractor,
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

                _logger.LogInformation("Extracción completada. Iniciando Carga...");

                await LoadDimProducts(csvProducts, apiProducts);
                await LoadDimCustomers(csvCustomers, apiCustomers);

                var allSales = sqlSales.Concat(csvSales.Select(MapCsvToDbSales)).ToList();

                await LoadDimStatus(allSales);
                await LoadDimDate(allSales);

                _logger.LogInformation("Iniciando proceso para FactSales...");

                await CleanFactTables();

                await LoadFactSales(allSales);

                _logger.LogInformation(">>> PROCESO ETL COMPLETADO EXITOSAMENTE <<<");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ERROR FATAL en el proceso ETL.");
            }
        }

        private async Task CleanFactTables()
        {
            _logger.LogInformation("Limpiando tabla FactSales (Truncate/Delete)...");

            var allFacts = await _factSalesRepo.GetAll();

            if (allFacts.Any())
            {
                await _factSalesRepo.Remove(allFacts.ToArray());
                _logger.LogInformation("Se han eliminado {Count} registros antiguos de FactSales.", allFacts.Count);
            }
            else
            {
                _logger.LogInformation("FactSales ya estaba vacía. No se requiere limpieza.");
            }
        }


        private async Task LoadFactSales(IEnumerable<DbSales> sales)
        {
            _logger.LogInformation("Cargando FactSales... Optimizando con Diccionarios.");

            var productsMap = (await _dimProductRepo.GetAll()).ToDictionary(k => k.ProductId, v => v.ProductKey);
            var customersMap = (await _dimCustomerRepo.GetAll()).ToDictionary(k => k.CustomerId, v => v.CustomerKey);
            var statusMap = (await _dimStatusRepo.GetAll()).ToDictionary(k => k.Status, v => v.StatusKey);
            var dateMap = (await _dimDateRepo.GetAll()).ToDictionary(k => k.CompleteDate.Date, v => v.DateKey);

            var factsToLoad = sales
                .Where(s => s.ProductID.HasValue && s.CustomerID.HasValue && !string.IsNullOrEmpty(s.Status))
                .Select(sale => new
                {
                    Sale = sale,
                    ProductKey = productsMap.GetValueOrDefault(sale.ProductID!.Value),
                    CustomerKey = customersMap.GetValueOrDefault(sale.CustomerID!.Value),
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
                _logger.LogInformation("Insertando {Count} hechos en BD...", factsToLoad.Length);
                await _factSalesRepo.SaveAll(factsToLoad);
                _logger.LogInformation("Carga de FactSales completada.");
            }
        }

        private async Task LoadDimProducts(IEnumerable<Product> csvProds, IEnumerable<ApiProducts> apiProds)
        {
            var existingIds = (await _dimProductRepo.GetAll()).Select(p => p.ProductId).ToHashSet();
            var newProducts = new List<DimProducts>();

            var fromCsv = csvProds.Where(p => !existingIds.Contains(p.ProductID)).Select(p => new DimProducts
            {
                ProductId = p.ProductID,
                ProductName = p.ProductName,
                Category = p.Category,
                Price = p.Price
            });
            newProducts.AddRange(fromCsv);
            foreach (var p in fromCsv) existingIds.Add(p.ProductId);

            var fromApi = apiProds.Where(p => !existingIds.Contains(p.Id)).Select(p => new DimProducts
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
                _logger.LogInformation("DimProducts: {Count} nuevos registros.", newProducts.Count);
            }
        }

        private async Task LoadDimCustomers(IEnumerable<Customer> csvCusts, IEnumerable<ApiCustomers> apiCusts)
        {
            var existingIds = (await _dimCustomerRepo.GetAll()).Select(c => c.CustomerId).ToHashSet();
            var newCustomers = new List<DimCustomers>();

            var fromCsv = csvCusts.Where(c => !existingIds.Contains(c.CustomerID)).Select(c => new DimCustomers
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

            var fromApi = apiCusts.Where(c => !existingIds.Contains(c.Id)).Select(c => new DimCustomers
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
                _logger.LogInformation("DimCustomers: {Count} nuevos registros.", newCustomers.Count);
            }
        }

        private async Task LoadDimStatus(IEnumerable<DbSales> sales)
        {
            var existingStatuses = (await _dimStatusRepo.GetAll()).Select(s => s.Status).ToHashSet();
            var newStatuses = sales.Select(s => s.Status).Where(s => !string.IsNullOrEmpty(s)).Distinct()
                .Where(s => !existingStatuses.Contains(s))
                .Select(s => new DimStatus { Status = s }).ToArray();

            if (newStatuses.Any()) await _dimStatusRepo.SaveAll(newStatuses);
        }

        private async Task LoadDimDate(IEnumerable<DbSales> sales)
        {
            var existingDateKeys = (await _dimDateRepo.GetAll()).Select(d => d.DateKey).ToHashSet();
            var newDates = sales.Select(s => s.OrderDate.Date).Distinct()
                .Select(d => new { Date = d, Key = (d.Year * 10000) + (d.Month * 100) + d.Day })
                .Where(x => !existingDateKeys.Contains(x.Key))
                .Select(x => new DimDate { DateKey = x.Key, CompleteDate = x.Date, Year = x.Date.Year, Month = x.Date.Month, MonthName = x.Date.ToString("MMMM") })
                .ToArray();

            if (newDates.Any()) await _dimDateRepo.SaveAll(newDates);
        }

        private DbSales MapCsvToDbSales(CsvSales csv)
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
    }
}