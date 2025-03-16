using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RateLimiter
{
    public class RateLimiter<TArg>
    {
        private readonly Func<TArg, Task> _action;
        private readonly List<RateLimit> _rateLimits;
        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);

        public RateLimiter(Func<TArg, Task> action, List<RateLimit> rateLimits)
        {
            _action = action;
            _rateLimits = rateLimits;
        }

        public async Task Perform(TArg arg)
        {
            while (true)
            {
                TimeSpan delay = TimeSpan.Zero;

                await _semaphore.WaitAsync();
                try
                {
                    //cheaks all the limiters
                    foreach (var limit in _rateLimits)
                    {
                        if (limit.Count >= limit.MaxCount)
                        {
                            TimeSpan timeUntilExpire = limit.NextExpire;

                            if (timeUntilExpire > delay)
                            {
                                delay = timeUntilExpire;
                            }
                        }
                    }

                    if (delay == TimeSpan.Zero)
                    {
                        foreach (var limit in _rateLimits)
                        {
                            limit.Add();
                        }
                        break;
                    }
                }
                finally
                {
                    _semaphore.Release();
                }

                await Task.Delay(delay);
            }

            await _action(arg);
        }
    }
}
