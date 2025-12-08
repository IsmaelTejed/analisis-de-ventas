using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ADV.Application.Interface;
using ADV.Domain.Entities.CSV;
using ADV.Domain.Repository;
using ADV.Persistense.repositorie.CSVbase;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ADV.Persistense.repositorie.CSV
{
    public sealed class CustomersRepository : BaseCsvRepository, IExtractor<Customer>
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<CustomersRepository> _logger;
        private readonly string _filePath;

        public string SourceName => "CsvCustomers";

        public CustomersRepository(IConfiguration configuration,
                                     ILogger<CustomersRepository> logger)
        {
            _configuration = configuration;
            _logger = logger;
            _filePath = _configuration.GetSection("CsvFilePaths:Customers").Value ?? string.Empty;
        }

        public async Task<IEnumerable<Customer>> ExtractAsync()
        {
            _logger.LogInformation("Extracting data from {Source} at path: {FilePath}", SourceName, _filePath);

            List<Customer> customersList = await ReadCsvFileAsync<Customer>(_filePath, _logger);

            return customersList;
        }
    }
}
