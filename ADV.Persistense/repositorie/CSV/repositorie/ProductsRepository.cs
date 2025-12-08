using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ADV.Application.Interface;
using ADV.Domain.Entities.CSV;
using ADV.Domain.Entities.DB;
using ADV.Persistense.repositorie.CSVbase;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ADV.Persistense.repositorie.CSV.repositorie
{
    public sealed class ProductsRepository : BaseCsvRepository, IExtractor<Product>
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<ProductsRepository> _logger;
        private readonly string _filePath;

        public string SourceName => "CsvProducts";

        public ProductsRepository(IConfiguration configuration,
                                  ILogger<ProductsRepository> logger)
        {
            _configuration = configuration;
            _logger = logger;
            _filePath = _configuration.GetSection("CsvFilePaths:Products").Value ?? string.Empty;
        }

        // CAMBIO: Devuelve IEnumerable<Products>
        public async Task<IEnumerable<Product>> ExtractAsync()
        {
            _logger.LogInformation("Extracting data from {Source} at path: {FilePath}", SourceName, _filePath);

            // CAMBIO: Lee <Products>
            List<Product> productsList = await ReadCsvFileAsync<Product>(_filePath, _logger);

            return productsList;
        }
    }
}
