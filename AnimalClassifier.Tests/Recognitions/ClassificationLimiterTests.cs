namespace AnimalClassifier.Tests.Recognitions
{
    using AnimalClassifier.Core.Common.Exceptions;
    using AnimalClassifier.Core.Recognitions.Classification;
    using Microsoft.Extensions.Options;

    public class ClassificationLimiterTests
    {
        [Fact]
        public async Task AnUpload_WaitsForTheTurnBeforeIt()
        {
            using var limiter = Limiter(concurrent: 1, queue: 1);
            var first = await limiter.WaitTurnAsync(CancellationToken.None);

            var second = limiter.WaitTurnAsync(CancellationToken.None);
            Assert.False(second.IsCompleted);

            first.Dispose();
            (await second).Dispose();
        }

        // Rather than kept waiting for longer than its caller would.
        [Fact]
        public async Task AnUpload_BeyondTheQueue_IsTurnedAway()
        {
            using var limiter = Limiter(concurrent: 1, queue: 0);
            using var first = await limiter.WaitTurnAsync(CancellationToken.None);

            await Assert.ThrowsAsync<ServiceBusyException>(() => limiter.WaitTurnAsync(CancellationToken.None));
        }

        // A caller who goes away gives up their place.
        [Fact]
        public async Task AnUploadWhoseCallerWentAway_StopsWaiting()
        {
            using var limiter = Limiter(concurrent: 1, queue: 1);
            using var first = await limiter.WaitTurnAsync(CancellationToken.None);
            using var goneAway = new CancellationTokenSource();

            var waiting = limiter.WaitTurnAsync(goneAway.Token);
            await goneAway.CancelAsync();

            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => waiting);
        }

        private static ClassificationLimiter Limiter(int concurrent, int queue) =>
            new(Options.Create(new ClassificationLimitSettings
            {
                ConcurrentClassificationLimit = concurrent,
                ClassificationQueueLimit = queue
            }));
    }
}
