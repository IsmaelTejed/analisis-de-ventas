using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CsvHelper;
using Microsoft.Extensions.Logging;

namespace ADV.Persistense.repositorie.CSVbase
{
    public abstract class BaseCsvRepository
    {
        protected async Task<List<T>> ReadCsvFileAsync<T>(string filePath, ILogger logger)
        {
            var records = new List<T>();

            if (!File.Exists(filePath))
            {
                logger.LogWarning("CSV file not found at path: {FilePath}", filePath);
                return records;
            }

            try
            {
                using var reader = new StreamReader(filePath);
                using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);

                await foreach (var record in csv.GetRecordsAsync<T>())
                {
                    records.Add(record);
                }

                logger.LogInformation("Successfully read {Count} records from {FilePath}", records.Count, filePath);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred while reading the CSV file at {FilePath}", filePath);
            }

            return records;
        }
    }




}


