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
            await _semaphore.WaitAsync();
            try { 
            while (true)
            {
                TimeSpan delay = TimeSpan.Zero;

                    bool canProceed = true;
                    //cheaks all the limiters
                    foreach (var limit in _rateLimits)
                    {
                        if (limit.Count >= limit.MaxCount)
                        {
                            canProceed = false;
                            TimeSpan timeUntilExpire = limit.NextExpire;

                            if (timeUntilExpire > delay)
                            {
                                delay = timeUntilExpire;
                            }
                        }
                    }

                    if (canProceed)
                    {
                        foreach (var limit in _rateLimits)
                        {
                            limit.Add();
                        }
                        _action(arg);
                        break;
                    }
                    await Task.Delay(delay);
                }
            }
            finally
            {
                _semaphore.Release();
            }
        }
    }
}
