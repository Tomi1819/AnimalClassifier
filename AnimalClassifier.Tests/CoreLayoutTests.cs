namespace AnimalClassifier.Tests
{
    using AnimalClassifier.Core.Common.Models;
    using AnimalClassifier.Tests.Support;

    /// <summary>
    /// Keeps Core's areas depending on each other in one direction only; see
    /// <see cref="DependencyOrder"/> for what counts. Common is what every
    /// area uses and uses none of them, and administration, which manages the
    /// accounts, comes after identity.
    /// </summary>
    public class CoreLayoutTests
    {
        private static readonly DependencyOrder Order = new(
            typeof(MessageResponse).Assembly,
            "AnimalClassifier.Core",
            "Common", "Identity", "Recognitions", "Admin");

        [Fact]
        public void EveryAreaOfCore_IsInTheDependencyOrder() =>
            Order.AssertEveryPartIsListed();

        [Fact]
        public void EachAreaOfCore_UsesOnlyTheAreasBeforeIt() =>
            Order.AssertEachPartUsesOnlyThePartsBeforeIt();
    }
}
