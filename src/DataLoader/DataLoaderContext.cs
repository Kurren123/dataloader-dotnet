using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace DataLoader
{
    /// <summary>
    /// Defines a context for <see cref="DataLoader"/> instances.
    /// </summary>
    /// <remarks>
    /// This class contains any data required by <see cref="DataLoader"/> instances and is responsible for managing their execution.
    ///e
    ///
    /// Loaders enlist themselves with the context active at the time when a <code>Load</code> method is called on a loader instance.
    /// When the <see cref="DataLoaderContext.Complete"/> method is called on the context, it begins executing the enlisted loaders.
    /// Loaders are executed serially, as parallel requests to the database are generally not conducive to good performance or throughput.
    ///
    /// The context will aim to wait until each loader has finished loading and any continuations off of the handed out promises have
    /// finished executing, before moving on to the loader. The purpose of this is to allow loaders to enlist or reenlist themselves so that
    /// they too are processed as part of the context's completion.
    /// </remarks>
    public sealed class DataLoaderContext
    {
        private readonly Queue<IDataLoader> _queue = new Queue<IDataLoader>();
        private readonly ConcurrentDictionary<object, IDataLoader> _cache = new ConcurrentDictionary<object, IDataLoader>();

        internal DataLoaderContext()
        {
        }

        /// <summary>
        /// Retrieves a cached loader for the given key, creating one if none is found.
        /// </summary>
        public IDataLoader<TKey, TReturn> GetLoader<TKey, TReturn>(object key, Func<IEnumerable<TKey>, ILookup<TKey, TReturn>> fetch)
        {
            return (IDataLoader<TKey, TReturn>)_cache.GetOrAdd(key, _ => new DataLoader<TKey, TReturn>(fetch, this));
        }

        /// <summary>
        /// Begins processing the waiting loaders, firing them sequentially until there are none remaining.
        /// </summary>
        /// <remarks>
        /// Loaders are fired in the order that they are first called. Once completed the context cannot be reused.
        /// </remarks>
        public void Complete()
        {
            while (_queue.Count > 0)
            {
                _queue.Dequeue().Execute();
            }
        }

        /// <summary>
        /// Queues a loader to be executed.
        /// </summary>
        internal void AddToQueue(IDataLoader loader)
        {
            _queue.Enqueue(loader);
        }

        #region Ambient context

        private static readonly ThreadLocal<DataLoaderContext> LocalContext = new ThreadLocal<DataLoaderContext>();

        /// <summary>
        /// Represents ambient data local to the current load operation.
        /// <seealso cref="DataLoaderContext.Run{T}(Func{Task{T}})"/>
        /// </summary>
        public static DataLoaderContext Current => LocalContext.Value;

        /// <summary>
        /// Sets the <see cref="DataLoaderContext"/> visible from the <see cref="DataLoaderContext.Current"/>  property.
        /// </summary>
        /// <param name="context"></param>
        internal static void SetCurrentContext(DataLoaderContext context)
        {
            LocalContext.Value = context;
        }

        #endregion

        /// <summary>
        /// Runs code within a new loader context before firing any pending
        /// <see cref="DataLoader{TKey,TReturn}">DataLoader</see> instances.
        /// </summary>
        public static void Run(Action action) => Run(_ => action());

        /// <summary>
        /// Runs code within a new loader context before firing any pending
        /// <see cref="DataLoader{TKey,TReturn}">DataLoader</see> instances.
        /// </summary>
        public static T Run<T>(Func<T> func) => Run(_ => func());

        /// <summary>
        /// Runs code within a new loader context before firing any pending
        /// <see cref="DataLoader{TKey,TReturn}">DataLoader</see> instances.
        /// </summary>
        public static void Run(Action<DataLoaderContext> func) => Run<object>(ctx => { func(ctx); return null; });

        /// <summary>
        /// Runs code within a new loader context before firing any pending
        /// <see cref="DataLoader{TKey,TReturn}">DataLoader</see> instances.
        /// </summary>
        public static T Run<T>(Func<DataLoaderContext, T> func)
        {
            if (func == null) throw new ArgumentNullException(nameof(func));

            using (var scope = new DataLoaderScope())
            {
                return func(scope.Context);
            }
        }
    }
}