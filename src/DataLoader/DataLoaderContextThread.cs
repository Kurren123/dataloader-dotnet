using System;
using System.Collections.Concurrent;
using System.Threading;

namespace DataLoader
{
    public class DataLoaderContextThread
    {
        private static Thread _thread;
        private static BlockingCollection<IDataLoader> _queue;

        static DataLoaderContextThread()
        {
            _thread = new Thread(ThreadLoop);
            _queue = new BlockingCollection<IDataLoader>();
        }

        public static void QueueDataLoader(IDataLoader loader)
        {
            if (_queue.IsAddingCompleted)
                throw new InvalidOperationException();
                
            _queue.Add(loader);
        }

        public static void Complete()
        {
            _queue.CompleteAdding();
        }

        private static void ThreadLoop()
        {
            foreach (var loader in _queue.GetConsumingEnumerable())
            {
                // do something with a loader...
                loader.ExecuteAsync().Wait();
            }
        }
    }
}