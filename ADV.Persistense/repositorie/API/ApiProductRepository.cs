using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ADV.Application.Interface;
using ADV.Domain.Entities.Api;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Clients.ActiveDirectory;

namespace ADV.Persistense.repositorie.API
{
    public class ApiProductRepository : IExtractor<ApiProducts>
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly ILogger<ApiProductRepository> _logger;

        public string SourceName => "ApiProducts";

        public ApiProductRepository(IHttpClientFactory httpClientFactory,
                                    IConfiguration configuration,
                                    ILogger<ApiProductRepository> _logger)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            this._logger = _logger;
        }

        public async Task<IEnumerable<ApiProducts>> ExtractAsync()
        {
            _logger.LogInformation("Extracting data from {Source}", SourceName);
            var products = new List<ApiProducts>();

            try
            {
                // 1. Creamos el cliente HTTP (puedes ponerle nombre o usar el default)
                var client = _httpClientFactory.CreateClient();

                // 2. Obtenemos la URL desde appsettings.json
                var url = _configuration["ApiSettings:ProductsUrl"];

                if (string.IsNullOrEmpty(url))
                {
                    _logger.LogWarning("API URL for products is missing.");
                    return products;
                }

                // 3. Hacemos la llamada GET
                var response = await client.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    // 4. Deserializamos el JSON directamente a la lista de objetos
                    var result = await response.Content.ReadFromJsonAsync<List<ApiProducts>>();
                    if (result != null)
                    {
                        products = result;
                        _logger.LogInformation("Successfully extracted {Count} records from API", products.Count);
                    }
                }
                else
                {
                    _logger.LogError("API request failed with status code: {StatusCode}", response.StatusCode);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error extracting data from API Products");
            }

            return products;
        }
    }
}
