using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DataLoader
{
    public interface IDataLoader<in TKey, TReturn> : IDataLoader
    {
        Task<IEnumerable<TReturn>> LoadAsync(TKey key);
    }

    public interface IDataLoader
    {
        // DataLoaderStatus Status { get; }
        Task ExecuteAsync();
        ILookup<object, object> GetResult();
    }
}