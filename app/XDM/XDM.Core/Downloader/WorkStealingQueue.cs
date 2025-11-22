using System.Collections.Concurrent;

namespace XDM.Core.Downloader
{
    public class WorkStealingQueue<T>
    {
        private readonly ConcurrentQueue<T> _queue = new ConcurrentQueue<T>();

        public void Enqueue(T item)
        {
            _queue.Enqueue(item);
        }

        public bool TryDequeue(out T item)
        {
            return _queue.TryDequeue(out item);
        }

        public bool TrySteal(out T item)
        {
            return _queue.TryDequeue(out item);
        }

        public bool IsEmpty => _queue.IsEmpty;
    }
}
