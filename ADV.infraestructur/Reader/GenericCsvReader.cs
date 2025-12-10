
using System.Globalization;
using ADV.Application.Interface;
using CsvHelper;
using Microsoft.Extensions.Logging;


namespace ADV.Application.Infrastructure
{
   
    public sealed class GenericCsvReader<T> : IGenericCsvReader<T> where T : class
    {
        
        private readonly ILogger<GenericCsvReader<T>> _logger;

        public GenericCsvReader(ILogger<GenericCsvReader<T>> logger)
        {
            _logger = logger;
        }

        public async Task<IEnumerable<T>> ReadFileAsync(string filePath)
        {
            _logger.LogInformation("Iniciando lectura de archivo CSV: {FilePath}", filePath);

            if (!File.Exists(filePath))
            {
                _logger.LogWarning("El archivo no existe en la ruta: {FilePath}", filePath);
                return Enumerable.Empty<T>(); 
            }

            try
            {
                using var reader = new StreamReader(filePath);
                using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);

                
                var records = await csv.GetRecordsAsync<T>().ToListAsync();

                _logger.LogInformation("Se leyeron {RecordCount} registros de {FilePath}", records.Count, filePath);
                return records;
            }
            catch (Exception ex)
            {
                
                _logger.LogError(ex, "Error leyendo el archivo CSV en la ruta: {FilePath}", filePath);
               
                return Enumerable.Empty<T>();
            }
        }
    }
}