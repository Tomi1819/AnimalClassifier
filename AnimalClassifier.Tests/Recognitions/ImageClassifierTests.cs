namespace AnimalClassifier.Tests.Recognitions
{
    using AnimalClassifier.Infrastructure.Classification;
    using AnimalClassifier.Infrastructure.Classification.Models;
    using AnimalClassifier.Tests.Support;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.ML;
    using OpenCvSharp;

    /// <summary>
    /// Runs the trained model itself, as the app loads it, which every other
    /// test leaves alone. It is what shows that the columns the app reads are
    /// still the model's.
    /// </summary>
    public class ImageClassifierTests : IClassFixture<ApiFactory>
    {
        private readonly ApiFactory factory;

        public ImageClassifierTests(ApiFactory factory)
        {
            this.factory = factory;
        }

        [Fact]
        public void AnImage_IsClassifiedAsOneAnimal()
        {
            var prediction = CreateClassifier().Classify(SolidImage());

            Assert.False(string.IsNullOrWhiteSpace(prediction.Animal));
            Assert.InRange(prediction.Score, 0f, 1f);
        }

        [Fact]
        public void TheKnownAnimals_AreTheOnesTheModelNames()
        {
            var classifier = CreateClassifier();

            var prediction = classifier.Classify(SolidImage());

            Assert.Contains(prediction.Animal, classifier.KnownAnimals);
            Assert.Equal(classifier.KnownAnimals.Order(StringComparer.OrdinalIgnoreCase), classifier.KnownAnimals);
            Assert.Equal(classifier.KnownAnimals.Distinct().Count(), classifier.KnownAnimals.Count);
        }

        private MLImageClassifier CreateClassifier() =>
            new(factory.Services.GetRequiredService<PredictionEnginePool<ImageData, ImagePrediction>>());

        private static byte[] SolidImage()
        {
            using var image = new Mat(224, 224, MatType.CV_8UC3, new Scalar(40, 120, 200));

            return image.ToBytes(".jpg");
        }
    }
}
