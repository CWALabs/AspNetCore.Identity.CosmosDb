using System.Threading;
using AspNetCore.Identity.CosmosDb;

namespace AspNetCore.Identity.CosmosDb.Tests.Net9
{
    [TestClass]
    public class RetryTests
    {
        [TestMethod]
        public void Do_Action_RetriesAndEventuallySucceeds()
        {
            var attempts = 0;

            Retry.Do(() =>
            {
                attempts++;
                if (attempts < 3)
                {
                    throw new InvalidOperationException("transient");
                }
            }, TimeSpan.Zero, maxAttemptCount: 3);

            Assert.AreEqual(3, attempts);
        }

        [TestMethod]
        public void Do_Action_ThrowsAggregateExceptionAfterMaxAttempts()
        {
            var attempts = 0;

            Assert.ThrowsException<AggregateException>(() =>
                Retry.Do(() =>
                {
                    attempts++;
                    throw new InvalidOperationException("always fails");
                }, TimeSpan.Zero, maxAttemptCount: 2));

            Assert.AreEqual(2, attempts);
        }

        [TestMethod]
        public void Do_Func_ReturnsValue()
        {
            var result = Retry.Do(() => 42, TimeSpan.Zero, maxAttemptCount: 1);
            Assert.AreEqual(42, result);
        }

        [TestMethod]
        public async Task DoAsync_RetriesAndEventuallySucceeds()
        {
            var attempts = 0;

            await Retry.DoAsync(async () =>
            {
                attempts++;
                await Task.Yield();

                if (attempts < 3)
                {
                    throw new InvalidOperationException("transient");
                }
            }, TimeSpan.Zero, maxAttemptCount: 3);

            Assert.AreEqual(3, attempts);
        }

        [TestMethod]
        public async Task DoAsync_ThrowsAggregateExceptionAfterMaxAttempts()
        {
            var attempts = 0;

            await Assert.ThrowsExceptionAsync<AggregateException>(async () =>
                await Retry.DoAsync(async () =>
                {
                    attempts++;
                    await Task.Yield();
                    throw new InvalidOperationException("always fails");
                }, TimeSpan.Zero, maxAttemptCount: 2));

            Assert.AreEqual(2, attempts);
        }

        [TestMethod]
        public async Task DoAsync_HonorsCancellationToken()
        {
            using var cts = new CancellationTokenSource();
            cts.Cancel();

            await Assert.ThrowsExceptionAsync<OperationCanceledException>(async () =>
                await Retry.DoAsync(
                    async () => await Task.CompletedTask,
                    TimeSpan.FromMilliseconds(1),
                    maxAttemptCount: 3,
                    cancellationToken: cts.Token));
        }

        [TestMethod]
        public async Task DoAsyncOfT_ReturnsValueAfterRetry()
        {
            var attempts = 0;

            var value = await Retry.DoAsync(async () =>
            {
                attempts++;
                await Task.Yield();
                if (attempts < 2)
                {
                    throw new InvalidOperationException("transient");
                }

                return 99;
            }, TimeSpan.Zero, maxAttemptCount: 2);

            Assert.AreEqual(99, value);
            Assert.AreEqual(2, attempts);
        }
    }
}
