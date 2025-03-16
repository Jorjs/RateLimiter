using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using RateLimiter;

public class RateLimiterApplication
{
    /*
     * @author Amir Malul
     *
     * For this task, I chose the sliding window technique. I chose this technique because 
     * it allows for a better distribution of requests and fits real world scenarios, even though 
     * it is harder to maintain and handle compared to a fixed window strategy.
     *
     * How does the rate limiter work?
     * - A queue stores the date and time of each request.
     * - The queue sets a timer for the oldest request based on the limiter's delay. When the timer finish, 
     *   it removes the expired request from the queue and sets a new timer for the next oldest request.
     *
     * Each time a new request arrives, the Perform method loops through all the limiters. When all limiters 
     * are free, the request is processed. If not, there is an await delay for the longest delay among all the limiters.
     *
     * The example below uses 3 rate limits: 10 per second, 100 per minute, and 1000 per day. It will create 
     * 250 action threads that will be processed by Perform. The result should be that the first ten requests 
     * are sent every second. After reaching 100, the rate limiter will limit further requests until the next minute.
     */
    public static async Task Main(string[] args)
    {
        // Creating 3 rate limits: 10 requests per second, 100 requests per minute, 1000 requests per day.
        List<RateLimit> rateLimits = new List<RateLimit>();

        rateLimits.Add(new RateLimit(10, TimeSpan.FromSeconds(1)));
        rateLimits.Add(new RateLimit(100, TimeSpan.FromMinutes(1)));
        rateLimits.Add(new RateLimit(1000, TimeSpan.FromDays(1)));

        Func<string, Task> action = async (arg) =>
        {
            Console.WriteLine($"Running action: {arg}");
            await Task.Delay(100);
        };

        RateLimiter<string> limiter = new RateLimiter<string>(action, rateLimits);

        // Create a list to hold all concurrent tasks.
        List<Task> tasks = new List<Task>();

        // Launch 250 concurrent calls to limiter.Perform.
        for (int i = 0; i < 250; i++)
        {
            int currentTask = i;
            tasks.Add(Task.Run(() => limiter.Perform($"Task {currentTask}")));
        }

        // Wait for all tasks to complete.
        await Task.WhenAll(tasks);
    }
}