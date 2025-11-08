using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ADV.Application.Interface
{
    public interface IGenericCsvReader<T> where T : class
    {
        Task<IEnumerable<T>> ReadFileAsync(string filePath);
    }
}
