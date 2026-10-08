namespace AnimalClassifier.Core.Recognitions.Uploads
{
    using AnimalClassifier.Core.Recognitions.Classification;
    using AnimalClassifier.Core.Recognitions.Classification.Models;

    public class UploadClassifier : IUploadClassifier
    {
        private readonly IImageClassifier classifier;
        private readonly IImageSanitizer imageSanitizer;
        private readonly IVideoFrameSampler frameSampler;
        private readonly IClassificationLimiter classificationLimiter;

        public UploadClassifier(IImageClassifier classifier,
                                IImageSanitizer imageSanitizer,
                                IVideoFrameSampler frameSampler,
                                IClassificationLimiter classificationLimiter)
        {
            this.classifier = classifier;
            this.imageSanitizer = imageSanitizer;
            this.frameSampler = frameSampler;
            this.classificationLimiter = classificationLimiter;
        }

        public async Task<(byte[] Image, Prediction Prediction)> ClassifyImageAsync(byte[] upload, string extension, CancellationToken cancellationToken)
        {
            using var turn = await classificationLimiter.WaitTurnAsync(cancellationToken);

            var image = imageSanitizer.Sanitize(upload, extension);

            return (image, classifier.Classify(image));
        }

        // Stopped between frames, as nothing has been recorded yet.
        public async Task<IReadOnlyList<Prediction>> ClassifyVideoAsync(string videoPath, CancellationToken cancellationToken)
        {
            using var turn = await classificationLimiter.WaitTurnAsync(cancellationToken);

            var frames = new List<Prediction>();

            foreach (var frame in frameSampler.SampleFrames(videoPath))
            {
                cancellationToken.ThrowIfCancellationRequested();
                frames.Add(classifier.Classify(frame));
            }

            return frames;
        }
    }
}
