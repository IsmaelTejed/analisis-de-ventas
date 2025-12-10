using Microsoft.Data.SqlClient; // Necesitas este using (o Microsoft.EntityFrameworkCore)
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ADV.Application.Interface;
using Dapper; // Recomiendo usar Dapper para llamar SPs fácilmente, o ADO.NET puro

namespace ADV.Application.Services
{
    // Primero define la interfaz en IDataLoader.cs
    public interface IDataLoader
    {
        Task LoadDataAsync(CancellationToken cancellationToken);
    }

    public class DataLoader : IDataLoader
    {
        private readonly ILogger<DataLoader> _logger;
        private readonly string _connectionString;

        public DataLoader(ILogger<DataLoader> logger, IConfiguration configuration)
        {
            _logger = logger;
            // Asegúrate de tener esta cadena en appsettings.json apuntando a ventas_DW
            _connectionString = configuration.GetConnectionString("DataWarehouseConnection");
        }

        public async Task LoadDataAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("🚛 Iniciando Fase de Carga (Load) al Data Warehouse...");

            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync(cancellationToken);

                    // Ejecutamos el Stored Procedure que creaste
                    // Aumentamos el Timeout porque mover datos puede tardar
                    await connection.ExecuteAsync(
                        "ETL_LoadDataWarehouse",
                        commandType: System.Data.CommandType.StoredProcedure,
                        commandTimeout: 300);
                }

                _logger.LogInformation("✅ Carga al Data Warehouse completada exitosamente.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error crítico durante la carga al DW.");
                throw;
            }
        }
    }
}