using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ADV.Application.Repositories_Dwh;
using ADV.Domain.Entities.Dimencion;

namespace ADV.Persistense.Destination.Repositories
{
    public class DimDateRepository : BaseDwhRepository<DimDate>, IDimDateRepository
    {
        public DimDateRepository(DwhDbContext context) : base(context)
        {
        }
    }
}
