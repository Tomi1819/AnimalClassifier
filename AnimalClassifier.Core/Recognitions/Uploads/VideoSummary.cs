namespace AnimalClassifier.Core.Recognitions.Uploads
{
    using AnimalClassifier.Core.Recognitions.Classification.Models;

    /// <summary>
    /// What a video's frames show, taken together. A single frame is often a
    /// blur or a glimpse, so an animal counts only when the model is fairly
    /// sure of it in several of them.
    /// </summary>
    public static class VideoSummary
    {
        /// <summary>
        /// The lowest score a frame can have for its animal to count.
        /// </summary>
        public const float MinFrameScore = 0.6f;

        /// <summary>
        /// The fewest frames an animal has to be seen in to count.
        /// </summary>
        public const int MinFrames = 3;

        /// <returns>
        /// The animals that count, strongest first, each scored with its
        /// average over the frames it was seen in.
        /// </returns>
        public static IReadOnlyList<Prediction> TopAnimals(IEnumerable<Prediction> frames) =>
            frames.Where(frame => frame.Score >= MinFrameScore)
                  .GroupBy(frame => frame.Animal)
                  .Where(animal => animal.Count() >= MinFrames)
                  .Select(animal => new Prediction
                  {
                      Animal = animal.Key,
                      Score = animal.Average(frame => frame.Score)
                  })
                  .OrderByDescending(animal => animal.Score)
                  .ToList();
    }
}
