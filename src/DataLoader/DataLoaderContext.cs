﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DataLoader
{
    /// <summary>
    /// Defines a context for <see cref="DataLoader"/> instances.
    /// </summary>
    /// <remarks>
    /// This class contains any data required by <see cref="DataLoader"/> instances and is responsible for managing their execution.
    ///
    /// Loaders enlist themselves with the context active at the time when a <code>Load</code> method is called on a loader instance.
    /// When the <see cref="DataLoaderContext.Complete"/> method is called on the context, it begins executing the enlisted loaders.
    /// Loaders are executed serially, since parallel requests to a database are generally not conducive to good performance or throughput.
    ///
    /// The context will try to wait until each loader - as well as continuations attached to each promise it hands out - finish executing
    /// before moving on to the next. The purpose of this is to allow loaders to enlist or reenlist themselves so that they too are processed
    /// as part the context's completion.
    /// </remarks>
    public sealed class DataLoaderContext
    {
        private readonly Queue<IDataLoader> _queue = new Queue<IDataLoader>();
        public ConcurrentDictionary<object, IDataLoader> Cache { get; } = new ConcurrentDictionary<object, IDataLoader>();
        private TaskCompletionSource<object> _completionSource = new TaskCompletionSource<object>();
        private bool _isCompleting;

        internal DataLoaderContext()
        {
        }

        /// <summary>
        /// Retrieves a cached loader for the given key, creating one if none is found.
        /// </summary>
        // public IDataLoader<TKey, TReturn> GetLoader<TKey, TReturn>(object key, Func<IEnumerable<TKey>, Task<ILookup<TKey, TReturn>>> fetcher)
        // {
            // return (IDataLoader<TKey, TReturn>)Cache.GetOrAdd(key, _ => new DataLoader<TKey, TReturn>(fetcher, this));
        // }

        /// <summary>
        /// Retrieves a cached loader for the given key, creating one if none is found.
        /// </summary>
        public CachedDataLoader<TKey, TReturn> GetLoader<TKey, TReturn>(object key, Func<IEnumerable<TKey>, Task<ILookup<TKey, TReturn>>> fetcher)
        {
            return (CachedDataLoader<TKey, TReturn>)Cache.GetOrAdd(key, _ => new CachedDataLoader<TKey, TReturn>(fetcher));
        }

        /// <summary>
        /// Represents whether this context has been completed.
        /// </summary>
        public Task Completion => _completionSource.Task;

        /// <summary>
        /// Begins processing the waiting loaders, firing them sequentially until there are none remaining.
        /// </summary>
        /// <remarks>
        /// Loaders are fired in the order that they are first called. Once completed the context cannot be reused.
        /// </remarks>
        public async void Complete()
        {
            if (_isCompleting) throw new InvalidOperationException();
            _isCompleting = true;

            try
            {
                while (_queue.Count > 0)
                    await _queue.Dequeue().ExecuteAsync().ConfigureAwait(false);

                _completionSource.SetResult(null);
            }
            catch (OperationCanceledException)
            {
                _completionSource.SetCanceled();
            }
            catch (Exception e)
            {
                _completionSource.SetException(e);
            }

            _isCompleting = false;
        }

        /// <summary>
        /// Queues a loader to be executed.
        /// </summary>
        internal void AddToQueue(IDataLoader loader)
        {
            _queue.Enqueue(loader);
        }

#region Ambient Context

#if NET45

        // No-ops for .NET 4.5 (so we don't have to change the remaining codebase)
        internal static DataLoaderContext Current => null;
        internal static void SetCurrentContext(DataLoaderContext context) {}

#else

        private static readonly AsyncLocal<DataLoaderContext> LocalContext = new AsyncLocal<DataLoaderContext>();

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

        /// <summary>
        /// Runs code within a new loader context before firing any pending
        /// <see cref="DataLoader{TKey,TReturn}">DataLoader</see> instances.
        /// </summary>
        public static Task<T> Run<T>(Func<Task<T>> func)
        {
            return Run(_ => func());
        }

        /// <summary>
        /// Runs code within a new loader context before firing any pending
        /// <see cref="DataLoader{TKey,TReturn}">DataLoader</see> instances.
        /// </summary>
        public static Task Run(Func<Task> func)
        {
            return Run(_ => func());
        }

#endif

        /// <summary>
        /// Runs code within a new loader context before firing any pending
        /// <see cref="DataLoader{TKey,TReturn}">DataLoader</see> instances.
        /// </summary>
        public static Task<T> Run<T>(Func<DataLoaderContext, Task<T>> func)
        {
            // TODO: Investigate the usage of Task.Run
            //
            // For some reason, using `Task.Run` causes <see cref="TaskCompletionSource{T}"/> to run continuations
            // synchronously, which prevents the main loop from continuing on to the next loader before they're done.
            //
            // I presume this is because once we're inside the ThreadPool, continuations will be scheduled using the
            // local queues (in LIFO order) instead of the global queue (which executes in FIFO order). This is really
            // a hack I think - the same thing should be accomplished using a custom TaskScheduler or custom awaiter.
            return Task.Run<T>(() =>
            {
                using (var scope = new DataLoaderScope())
                {
                    var task = func(scope.Context);
                    if (task == null) throw new InvalidOperationException("No task provided.");
                    return task;
                }
            });
        }

        /// <summary>
        /// Runs code within a new loader context before firing any pending
        /// <see cref="DataLoader{TKey,TReturn}">DataLoader</see> instances.
        /// </summary>
        public static Task Run(Func<DataLoaderContext, Task> func)
        {
            if (func == null) throw new ArgumentNullException(nameof(func));

            // TODO: see above
            return Task.Run(() =>
            {
                using (var scope = new DataLoaderScope())
                {
                    var task = func(scope.Context);
                    if (task == null) throw new InvalidOperationException("No task provided.");
                    return task;
                }
            });
        }

#endregion
    }
}