namespace AnimalClassifier.Tests.Recognitions
{
    using AnimalClassifier.Core.Recognitions.Classification.Models;
    using AnimalClassifier.Core.Recognitions.Uploads;

    public class VideoSummaryTests
    {
        [Fact]
        public void AnAnimalSeenClearlyInEnoughFrames_Counts_WithItsAverageScore()
        {
            var topAnimals = VideoSummary.TopAnimals([Frame("Cat", 0.7f), Frame("Cat", 0.8f), Frame("Cat", 0.9f)]);

            var cat = Assert.Single(topAnimals);
            Assert.Equal("Cat", cat.Animal);
            Assert.Equal(0.8f, cat.Score, precision: 5);
        }

        // A glimpse is not enough, however sure the model is of it.
        [Fact]
        public void AnAnimalSeenInTooFewFrames_DoesNotCount()
        {
            var frames = Enumerable.Repeat(Frame("Cat", 0.99f), VideoSummary.MinFrames - 1);

            Assert.Empty(VideoSummary.TopAnimals(frames));
        }

        // Nor is a blur, however often it shows up.
        [Fact]
        public void FramesTheModelIsUnsureOf_DoNotCount()
        {
            var frames = Enumerable.Repeat(Frame("Cat", VideoSummary.MinFrameScore - 0.01f), 10);

            Assert.Empty(VideoSummary.TopAnimals(frames));
        }

        [Fact]
        public void TheAnimals_AreStrongestFirst()
        {
            var frames = Enumerable.Repeat(Frame("Dog", 0.7f), 3).Concat(Enumerable.Repeat(Frame("Cat", 0.9f), 3));

            Assert.Equal(["Cat", "Dog"], VideoSummary.TopAnimals(frames).Select(animal => animal.Animal));
        }

        private static Prediction Frame(string animal, float score) => new() { Animal = animal, Score = score };
    }
}
