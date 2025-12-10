using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ADV.Application.Interface;
using ADV.Domain.Entities.DB;
using ADV.Persistense.repositorie.Db.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ADV.Persistense.repositorie.Db.Repositorie
{
    public class DbSalesRepository : IExtractor<DbSales>
    {
        private readonly ILogger<DbSalesRepository> _logger;
        private readonly IDbContextFactory<SourceDbContext> _dbContextFactory;
        public string SourceName => "DatabaseSales";

        public DbSalesRepository(ILogger<DbSalesRepository> logger,
                                 IDbContextFactory<SourceDbContext> dbContextFactory)
        {
            _logger = logger;
            _dbContextFactory = dbContextFactory;
        }

        public async Task<IEnumerable<DbSales>> ExtractAsync()
        {
            _logger.LogInformation("Extracting data from {Source}", SourceName);
            var salesList = new List<DbSales>();

            try
            {
                await using var dbContext = await _dbContextFactory.CreateDbContextAsync();

                var query = from detail in dbContext.OrderDetails
                            join order in dbContext.Orders
                            on detail.OrderID equals order.OrderID
                            select new DbSales
                            {
                                OrderID = order.OrderID,
                                CustomerID = order.CustomerID,
                                OrderDate = order.OrderDate,
                                Status = order.Status,
                                ProductID = detail.ProductID,
                                Quantity = detail.Quantity,
                                Price = detail.TotalPrice
                            };

                salesList = await query.AsNoTracking().ToListAsync();

                _logger.LogInformation("Successfully extracted {Count} records from {Source}", salesList.Count, SourceName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while extracting data from {Source}", SourceName);
            }

            return salesList;
        }
    }
}
