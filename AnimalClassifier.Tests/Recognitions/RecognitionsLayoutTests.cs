namespace AnimalClassifier.Tests.Recognitions
{
    using AnimalClassifier.Core.Recognitions;
    using AnimalClassifier.Tests.Support;

    /// <summary>
    /// Keeps the parts of recognitions depending on each other in one
    /// direction only; see <see cref="DependencyOrder"/> for what counts.
    /// Uploading runs the classifier, and what reads recognitions back uses
    /// neither.
    /// </summary>
    public class RecognitionsLayoutTests
    {
        private static readonly DependencyOrder Order = new(
            typeof(MediaFile).Assembly,
            typeof(MediaFile).Namespace!,
            "Classification", "Uploads", "History", "Search", "Statistics");

        [Fact]
        public void EveryPartOfRecognitions_IsInTheDependencyOrder() =>
            Order.AssertEveryPartIsListed();

        [Fact]
        public void EachPartOfRecognitions_UsesOnlyThePartsBeforeIt() =>
            Order.AssertEachPartUsesOnlyThePartsBeforeIt();
    }
}
