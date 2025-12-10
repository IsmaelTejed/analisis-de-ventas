using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ADV.Application.Repositories_Dwh;
using ADV.Domain.Entities.Dimencion;

namespace ADV.Persistense.Destination.Repositories
{
    public class DimStatusRepository : BaseDwhRepository<DimStatus>, IDimStatusRepository
    {
        public DimStatusRepository(DwhDbContext context) : base(context)
        {
        }
    }
}
