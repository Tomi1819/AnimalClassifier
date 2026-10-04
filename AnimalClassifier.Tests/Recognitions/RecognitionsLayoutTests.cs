namespace AnimalClassifier.Tests.Recognitions
{
    using AnimalClassifier.Core.Recognitions;
    using AnimalClassifier.Tests.Support;

    /// <summary>
    /// Keeps the parts of recognitions depending on each other in one
    /// direction only; see <see cref="DependencyOrder"/> for what counts.
    /// Uploading runs the classifier, and what reads recognitions back uses
    /// neither. Feedback checks a correction against the animals the
    /// classifier knows, and the history shows it. Each that hands out an
    /// image or a video links to it.
    /// </summary>
    public class RecognitionsLayoutTests
    {
        private static readonly DependencyOrder Order = new(
            typeof(MediaFile).Assembly,
            typeof(MediaFile).Namespace!,
            "Media", "Classification", "Uploads", "Feedback", "History", "Search", "Statistics");

        [Fact]
        public void EveryPartOfRecognitions_IsInTheDependencyOrder() =>
            Order.AssertEveryPartIsListed();

        [Fact]
        public void EachPartOfRecognitions_UsesOnlyThePartsBeforeIt() =>
            Order.AssertEachPartUsesOnlyThePartsBeforeIt();
    }
}
