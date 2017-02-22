using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DataLoader
{
    public class CachedDataLoader<TKey, TReturn> : IDataLoader<TKey, TReturn>
    {
        private readonly Func<IEnumerable<TKey>, Task<ILookup<TKey, TReturn>>> _fetch;
        public IEnumerable<TKey> Keys { get; set; }
        public Task<ILookup<TKey, TReturn>> Task { get; set; }
        public ILookup<TKey, TReturn> Result { get; private set; }

        public CachedDataLoader(Func<IEnumerable<TKey>, Task<ILookup<TKey, TReturn>>> fetch)
        {
            _fetch = fetch;
        }

        public Task<IEnumerable<TReturn>> LoadAsync(TKey key)
        {
            if (Result != null)
            {
                return Task<IEnumerable<TReturn>>.FromResult(Result[key]);
            }

            if (Task == null)
            {
                ExecuteAsync();
            }

            return Task.ContinueWith(t => t.Result[key], TaskContinuationOptions.ExecuteSynchronously);
        }

        public Task ExecuteAsync()
        {
            Task = _fetch(Keys.Distinct().AsEnumerable());
            return Task.ContinueWith(task => { Result = task.Result; }, TaskContinuationOptions.ExecuteSynchronously);
        }

        ILookup<object, object> IDataLoader.GetResult() => (ILookup<object,object>)Result;
    }
}