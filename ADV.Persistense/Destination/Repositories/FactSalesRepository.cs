using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ADV.Application.Repositories_Dwh;
using ADV.Domain.Entities.Facts;

namespace ADV.Persistense.Destination.Repositories
{
    public class FactSalesRepository : BaseDwhRepository<FactSales>, IFactSalesRepository
    {
        public FactSalesRepository(DwhDbContext context) : base(context)
        {
        }
    }
}
