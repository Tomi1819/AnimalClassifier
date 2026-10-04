namespace AnimalClassifier.Core.Recognitions.Classification
{
    using AnimalClassifier.Core.Common.Exceptions;
    using Microsoft.Extensions.Options;
    using System.Threading.RateLimiting;
    using static AnimalClassifier.Core.Recognitions.Classification.ClassificationMessages;

    public sealed class ClassificationLimiter : IClassificationLimiter, IDisposable
    {
        private readonly ConcurrencyLimiter limiter;

        public ClassificationLimiter(IOptions<ClassificationLimitSettings> settingsOptions)
        {
            var settings = settingsOptions.Value;

            limiter = new ConcurrencyLimiter(new ConcurrencyLimiterOptions
            {
                PermitLimit = settings.ConcurrentClassificationLimit,
                QueueLimit = settings.ClassificationQueueLimit,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst
            });
        }

        public async Task<IDisposable> WaitTurnAsync(CancellationToken cancellationToken)
        {
            var lease = await limiter.AcquireAsync(cancellationToken: cancellationToken);

            if (!lease.IsAcquired)
            {
                lease.Dispose();
                throw new ServiceBusyException(TooManyUploads);
            }

            return lease;
        }

        public void Dispose() => limiter.Dispose();
    }
}
