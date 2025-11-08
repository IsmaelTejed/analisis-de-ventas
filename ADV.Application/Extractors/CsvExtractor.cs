
using ADV.Application.Interface;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ADV.Domain.Entities.CSV;


namespace TuProyecto.Application.Extractors
{
    public class CsvExtractor : IExtractor
    {
        private readonly ILogger<CsvExtractor> _logger;
        private readonly string _basePath;

     
        private readonly IGenericCsvReader<Customer> _customerReader;
        private readonly IGenericCsvReader<Product> _productReader;
        private readonly IGenericCsvReader<Order> _orderReader;
        private readonly IGenericCsvReader<OrderDetail> _orderDetailReader;

        public CsvExtractor(
            ILogger<CsvExtractor> logger,
            IConfiguration configuration,
            IGenericCsvReader<Customer> customerReader,
            IGenericCsvReader<Product> productReader,
            IGenericCsvReader<Order> orderReader,
            IGenericCsvReader<OrderDetail> orderDetailReader)
        {
            _logger = logger;
            _basePath = configuration.GetSection("DataSource:CsvSettings:BasePath").Value ?? "C:\\Datos";

            
            _customerReader = customerReader;
            _productReader = productReader;
            _orderReader = orderReader;
            _orderDetailReader = orderDetailReader;
        }

        public async Task ExtractAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Iniciando extracción de archivos CSV...");
            try
            {
                
                var customers = await _customerReader.ReadFileAsync(Path.Combine(_basePath, "customers.csv"));
                _logger.LogInformation($"Se extrajeron {customers.Count()} clientes.");

                
                var products = await _productReader.ReadFileAsync(Path.Combine(_basePath, "products.csv"));
                _logger.LogInformation($"Se extrajeron {products.Count()} productos.");

                
                var orders = await _orderReader.ReadFileAsync(Path.Combine(_basePath, "orders.csv"));
                _logger.LogInformation($"Se extrajeron {orders.Count()} órdenes.");

               
                var orderDetails = await _orderDetailReader.ReadFileAsync(Path.Combine(_basePath, "order_details.csv"));
                _logger.LogInformation($"Se extrajeron {orderDetails.Count()} detalles de órdenes.");

            

                _logger.LogInformation("Extracción de CSV completada exitosamente.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error durante la extracción de CSV.");
            }
        }
    }
}