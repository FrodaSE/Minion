using System;
using System.Threading.Tasks;

namespace Minion.Core.Interfaces
{
    public interface ITestingBatchStore : IBatchStore
    {
        Task<DateTime?> GetNextJobDueTimeAsync();
    }
}