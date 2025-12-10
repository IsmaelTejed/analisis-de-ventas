using ADV.Application.Interface;
using ADV.Application.Repositories_Dwh;
using ADV.Application.Services;
using ADV.Domain.Entities.Api;
using ADV.Domain.Entities.CSV;
using ADV.Domain.Entities.DB;
using ADV.Persistense.Destination;
using ADV.Persistense.Destination.Repositories;
using ADV.Persistense.repositorie.API;
using ADV.Persistense.repositorie.CSV;
using ADV.Persistense.repositorie.CSV.repositorie;
using ADV.Persistense.repositorie.Db.Context;
using ADV.Persistense.repositorie.Db.Repositorie;
using ADV.WKS;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace ADV.WKS
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = Host.CreateApplicationBuilder(args);
            builder.Services.AddHostedService<Worker>();

            builder.Services.AddHttpClient();

            //Servicios de extracción
            builder.Services.AddScoped<IExtractor<Product>, ProductsRepository>();
            builder.Services.AddScoped<IExtractor<Customer>, CustomersRepository>();
            builder.Services.AddScoped<IExtractor<CsvSales>, SalesCsvRepository>();

            builder.Services.AddScoped<IExtractor<ApiProducts>, ApiProductRepository>();
            builder.Services.AddScoped<IExtractor<ApiCustomers>, ApiCustomerRepository>(); 

            builder.Services.AddDbContextFactory<SourceDbContext>(options =>
            {
                var connectionString = builder.Configuration.GetConnectionString("SourceDatabase");
                options.UseSqlServer(connectionString);
            });

            builder.Services.AddScoped<IExtractor<DbSales>, DbSalesRepository>();

            //Servicios de transformación

            builder.Services.AddDbContext<DwhDbContext>(options =>
            {
                var connectionString = builder.Configuration.GetConnectionString("DestinationDatabase");
                options.UseSqlServer(connectionString);
            });

            builder.Services.AddScoped<IDimProductRepository, DimProductRepository>();
            builder.Services.AddScoped<IDimCustomersRepository, DimCustomersRepository>();
            builder.Services.AddScoped<IDimDateRepository, DimDateRepository>();
            builder.Services.AddScoped<IDimStatusRepository, DimStatusRepository>();
            builder.Services.AddScoped<IFactSalesRepository, FactSalesRepository>();

            //REGISTRO DEL SERVICIO ETL 

            builder.Services.AddScoped<IEtlService, EtlService>();

            var host = builder.Build();
            host.Run();
        }
    }
}