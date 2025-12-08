using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace ADV.Domain.Repository
{
    
    public interface IBaseDwhRepository<TEntity> where TEntity : class
    {
        Task SaveAll(TEntity[] entities);

        Task Remove(TEntity[] entities);

        Task<List<TEntity>> GetAll();

        Task<List<bool>> Exist(Expression<Func<TEntity, bool>> filter);
    }
}

