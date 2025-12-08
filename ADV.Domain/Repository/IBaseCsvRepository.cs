using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ADV.Domain.Repository
{

    public interface IBaseCsvRepository<TClass>
    {
        Task<List<TClass>> GetAll();
    }
}
