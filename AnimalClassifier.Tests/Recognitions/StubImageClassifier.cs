namespace AnimalClassifier.Tests.Recognitions
{
    using AnimalClassifier.Core.Recognitions.Classification;
    using AnimalClassifier.Core.Recognitions.Classification.Models;

    /// <summary>
    /// Stands in for the model, which takes seconds to load and cannot be
    /// told what to see. It sees whatever a test has set, in every image, and
    /// keeps the images it was given.
    /// </summary>
    public class StubImageClassifier : IImageClassifier
    {
        public IReadOnlyList<string> KnownAnimals { get; } = ["Cat", "Coyote", "Dog", "Fox", "Wolf"];

        public string Animal { get; set; } = "Cat";

        public float Score { get; set; } = 0.9f;

        /// <summary>
        /// Set to have the model fail, as it does on an image it cannot read.
        /// </summary>
        public bool Fails { get; set; }

        public Prediction Classify(byte[] image)
        {
            if (Fails)
            {
                throw new InvalidOperationException("The model could not read the image.");
            }

            return new Prediction { Animal = Animal, Score = Score };
        }
    }
}
