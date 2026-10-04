namespace AnimalClassifier.Core.Recognitions.Classification
{
    using AnimalClassifier.Core.Common.Settings;
    using System.ComponentModel.DataAnnotations;

    /// <summary>
    /// The limit <see cref="ClassificationLimiter"/> enforces. It sits in the
    /// same section as the limits on the API's endpoints, so that every limit
    /// is configured in one place.
    /// </summary>
    public class ClassificationLimitSettings : ISettings
    {
        public static string SectionName => "RateLimiting";

        /// <summary>
        /// How many uploads are worked on at once, one for each processor
        /// unless set.
        /// </summary>
        [Range(1, int.MaxValue)]
        public int ConcurrentClassificationLimit { get; set; } = Environment.ProcessorCount;

        /// <summary>
        /// How many more may wait for a turn. Beyond that an upload is turned
        /// away at once, rather than kept waiting for longer than its caller
        /// would.
        /// </summary>
        [Range(0, int.MaxValue)]
        public int ClassificationQueueLimit { get; set; } = 20;
    }
}
