using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DataLoader
{
    /// <summary>
    /// Collects keys into a batch to fetch in one request.
    /// </summary>
    /// <remarks>
    /// When a call is made to one of the <see cref="LoadAsync"/> methods, each key is stored and a
    /// promise task is handed back that represents the future result of the deferred request. The request
    /// is deferred (and keys are collected) until the loader is invoked via one of the following means:
    /// <list type="bullet">
    /// <item>The user-supplied delegate for <see cref="DataLoaderContext.Run{T}"/> returned and a Load* method was called.</item>
    /// <item><see cref="DataLoaderContext.StartLoading">StartLoading</see> was explicitly called on the governing <see cref="DataLoaderContext"/>.</item>
    /// <item>The loader was invoked explicitly by calling <see cref="ExecuteAsync"/>.</item>
    /// </list>
    /// </remarks>
    public class DataLoader<TKey, TReturn> : IDataLoader<TKey, TReturn>
    {
        private readonly SemaphoreSlim _lock = new SemaphoreSlim(1);
        private readonly FetchDelegate<TKey, TReturn> _fetchDelegate;

        private HashSet<TKey> _keys = new HashSet<TKey>();
        private TaskCompletionSource<ILookup<TKey, TReturn>> _completion =
            new TaskCompletionSource<ILookup<TKey, TReturn>>();

        private DataLoaderContext _boundContext;

        /// <summary>
        /// Creates a new <see cref="DataLoader{TKey,TReturn}"/>.
        /// </summary>
        public DataLoader(FetchDelegate<TKey, TReturn> fetchDelegate)
        {
            _fetchDelegate = fetchDelegate;
        }

        /// <summary>
        /// Creates a new <see cref="DataLoader{TKey,TReturn}"/> bound to the specified context.
        /// </summary>
        public DataLoader(FetchDelegate<TKey, TReturn> fetchDelegate, DataLoaderContext context) : this(fetchDelegate)
        {
            SetContext(context);
        }

        /// <summary>
        /// Gets the bound context if set, otherwise the current ambient context.
        /// </summary>
        public DataLoaderContext Context => _boundContext ?? DataLoaderContext.Current;

        /// <summary>
        /// Gets the keys to retrieve in the next batch.
        /// </summary>
        public IEnumerable<TKey> Keys => new ReadOnlyCollection<TKey>(_keys.ToList());

        /// <summary>
        /// Indicates the loader's current status.
        /// </summary>
        public DataLoaderStatus Status => _keys.Count == 0
            ? DataLoaderStatus.Idle
            : (_lock.CurrentCount > 0
                ? DataLoaderStatus.WaitingToExecute
                : DataLoaderStatus.Executing);

        /// <summary>
        /// Binds an instance to a particular loading context.
        /// </summary>
        public void SetContext(DataLoaderContext context)
        {
            if (Status != DataLoaderStatus.Idle)
                throw new InvalidOperationException("Cannot set context - loader must be not be queued or executing");

            _boundContext = context;
        }

        /// <summary>
        /// Loads an item.
        /// </summary>
        public async Task<IEnumerable<TReturn>> LoadAsync(TKey key)
        {
            Task<ILookup<TKey, TReturn>> task;
            await _lock.WaitAsync().ConfigureAwait(false);
            try
            {
                if (_keys.Count == 0) Context?.AddPendingLoader(this);
                _keys.Add(key);
                task = _completion.Task;
            }
            finally { _lock.Release(); }
            return (await task.ConfigureAwait(false))[key];
        }

        /// <summary>
        /// Loads many items.
        /// </summary>
        public async Task<IDictionary<TKey, IEnumerable<TReturn>>> LoadAsync(params TKey[] keys)
        {
            var tasks = keys.Select(async key =>
                new KeyValuePair<TKey, IEnumerable<TReturn>>(
                    key, await LoadAsync(key).ConfigureAwait(false)));

            var results = await Task.WhenAll(tasks).ConfigureAwait(false);
            return results.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        }

        /// <summary>
        /// Triggers the load and fulfils any handed out promises.
        /// </summary>
        public async Task ExecuteAsync()
        {
            HashSet<TKey> keysToFetch;
            TaskCompletionSource<ILookup<TKey, TReturn>> lastCompletion;

            await _lock.WaitAsync().ConfigureAwait(false);
            try
            {
                lastCompletion = Interlocked.Exchange(ref _completion, new TaskCompletionSource<ILookup<TKey, TReturn>>());
                keysToFetch = Interlocked.Exchange(ref _keys, new HashSet<TKey>());
            }
            finally { _lock.Release(); }

            var lookup = await _fetchDelegate(keysToFetch).ConfigureAwait(false);
            lastCompletion.SetResult(lookup);
            await lastCompletion.Task.ConfigureAwait(false);
        }
    }
}