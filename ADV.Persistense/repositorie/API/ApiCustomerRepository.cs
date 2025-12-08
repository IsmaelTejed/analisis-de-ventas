
using ADV.Application.Interface;
using ADV.Domain.Entities.Api;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Clients.ActiveDirectory;

namespace ADV.Persistense.repositorie.API
{
    public class ApiCustomerRepository : IExtractor<ApiCustomers>
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly ILogger<ApiCustomerRepository> _logger;

        public string SourceName => "ApiCustomers";

        public ApiCustomerRepository(IHttpClientFactory httpClientFactory,
                                     IConfiguration configuration,
                                     ILogger<ApiCustomerRepository> logger)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<IEnumerable<ApiCustomers>> ExtractAsync()
        {
            _logger.LogInformation("Extracting data from {Source}", SourceName);
            var customers = new List<ApiCustomers>();

            try
            {
                var client = _httpClientFactory.CreateClient();
                var url = _configuration["ApiSettings:CustomersUrl"];

                if (string.IsNullOrEmpty(url))
                {
                    _logger.LogWarning("API URL for customers is missing.");
                    return customers;
                }

                var response = await client.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<List<ApiCustomers>>();
                    if (result != null)
                    {
                        customers = result;
                    }
                }
                else
                {
                    _logger.LogError("API request failed: {StatusCode}", response.StatusCode);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error extracting data from API Customers");
            }

            return customers;
        }
    }
}
