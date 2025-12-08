using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ADV.Application.Interface
{
    public interface IEtlService
    {
        Task RunEtlProcessAsync(CancellationToken cancellationToken);
    }
}
