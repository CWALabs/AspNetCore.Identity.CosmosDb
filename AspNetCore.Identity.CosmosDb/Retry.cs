using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace AspNetCore.Identity.CosmosDb
{

    //https://stackoverflow.com/questions/1563191/cleanest-way-to-write-retry-logic
    public static class Retry
    {
        /// <summary>
        /// Do action
        /// </summary>
        /// <param name="action"></param>
        /// <param name="retryInterval"></param>
        /// <param name="maxAttemptCount"></param>
        public static void Do(
            Action action,
            TimeSpan retryInterval,
            int maxAttemptCount = 5)
        {
            Do<object>(() =>
            {
                action();
                return null;
            }, retryInterval, maxAttemptCount);
        }

        /// <summary>
        /// Do action
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="action"></param>
        /// <param name="retryInterval"></param>
        /// <param name="maxAttemptCount"></param>
        /// <returns></returns>
        /// <exception cref="AggregateException"></exception>
        public static T Do<T>(
            Func<T> action,
            TimeSpan retryInterval,
            int maxAttemptCount = 5)
        {
            var exceptions = new List<Exception>();

            for (var attempted = 0; attempted < maxAttemptCount; attempted++)
                try
                {
                    if (attempted > 0) Thread.Sleep(retryInterval);
                    return action();
                }
                catch (Exception ex)
                {
                    exceptions.Add(ex);
                }

            throw new AggregateException(exceptions);
        }

        /// <summary>
        /// Executes an async action with retry behavior.
        /// </summary>
        /// <param name="action">Async action to execute.</param>
        /// <param name="retryInterval">Delay between attempts.</param>
        /// <param name="maxAttemptCount">Maximum number of attempts.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <exception cref="AggregateException"></exception>
        public static async Task DoAsync(
            Func<Task> action,
            TimeSpan retryInterval,
            int maxAttemptCount = 5,
            CancellationToken cancellationToken = default)
        {
            await DoAsync<object>(
                async () =>
                {
                    await action();
                    return null;
                },
                retryInterval,
                maxAttemptCount,
                cancellationToken);
        }

        /// <summary>
        /// Executes an async function with retry behavior and returns the result.
        /// </summary>
        /// <typeparam name="T">Return type.</typeparam>
        /// <param name="action">Async function to execute.</param>
        /// <param name="retryInterval">Delay between attempts.</param>
        /// <param name="maxAttemptCount">Maximum number of attempts.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Function result.</returns>
        /// <exception cref="AggregateException"></exception>
        public static async Task<T> DoAsync<T>(
            Func<Task<T>> action,
            TimeSpan retryInterval,
            int maxAttemptCount = 5,
            CancellationToken cancellationToken = default)
        {
            var exceptions = new List<Exception>();

            for (var attempted = 0; attempted < maxAttemptCount; attempted++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    if (attempted > 0)
                    {
                        await Task.Delay(retryInterval, cancellationToken);
                    }

                    return await action();
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    exceptions.Add(ex);
                }
            }

            throw new AggregateException(exceptions);
        }
    }
}
