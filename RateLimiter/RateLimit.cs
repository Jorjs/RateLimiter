using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

namespace RateLimiter
{
    public class ExpiringQueue
    {
        // Each entry is stored along with its expiration time.
        private readonly Queue<DateTime> _queue = new Queue<DateTime>();
        private Timer? _timer;
        private readonly TimeSpan _lifespan;
        private readonly object _lock = new object();

        public ExpiringQueue(TimeSpan lifespan)
        {
            _lifespan = lifespan;
        }

        // Adds an item to the queue. If it is the first item set the timer.
        public void Enqueue()
        {
            DateTime expiration = DateTime.UtcNow + _lifespan;
            lock (_lock)
            {
                _queue.Enqueue(expiration);
                if (_queue.Count == 1)
                {
                    SetTimer(expiration);
                }
            }
        }

        // Dequeues expired items automatically when the timer finishes.
        private void OnTimerElapsed(object state)
        {
            lock (_lock)
            {
                DateTime now = DateTime.UtcNow;
                // Remove all expired items.
                while (_queue.Count > 0 && _queue.Peek() <= now)
                {
                    _queue.Dequeue();
                }
                // If there are still items, set the timer for the next expiration.
                if (_queue.Count > 0)
                {
                    SetTimer(_queue.Peek());
                }
                else
                {
                    _timer?.Dispose();
                    _timer = null;
                }
            }
        }

        // Sets the timer to run at the next expiration.
        private void SetTimer(DateTime expiration)
        {
            TimeSpan delay = expiration - DateTime.UtcNow;
            if (delay < TimeSpan.Zero)
            {
                delay = TimeSpan.Zero;
            }
            _timer?.Dispose();
            _timer = new Timer(OnTimerElapsed, null, delay, Timeout.InfiniteTimeSpan);
        }

        // Provides the total number of non-expired items.
        public int Count
        {
            get
            {
                lock (_lock)
                {
                    return _queue.Count;
                }
            }
        }

        public TimeSpan NextExpiration
        {
            get
            {
                if (_queue.Count > 0)
                {
                    TimeSpan remainingTime = _queue.Peek() - DateTime.UtcNow;
                    if(remainingTime > TimeSpan.Zero)
                    {
                        return remainingTime;
                    }
                }
                return TimeSpan.Zero;
            }
        }
    }

    public class RateLimit
    {
        public int MaxCount { get; }
        private readonly TimeSpan _lifespan;
        private readonly ExpiringQueue _timestamps;

        public RateLimit(int maxCount, TimeSpan lifespan)
        {
            MaxCount = maxCount;
            _lifespan = lifespan;
            _timestamps = new ExpiringQueue(_lifespan);
        }

        public int Count => _timestamps.Count;

        // Calculates the time until the next token expires.
        public TimeSpan NextExpire => _timestamps.NextExpiration;

        public void Add()
        {
            _timestamps.Enqueue();
        }
    }
}
