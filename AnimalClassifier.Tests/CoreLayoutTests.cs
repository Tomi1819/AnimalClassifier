namespace AnimalClassifier.Tests
{
    using AnimalClassifier.Core.Common.Models;
    using AnimalClassifier.Tests.Support;

    /// <summary>
    /// Keeps Core's areas depending on each other in one direction only; see
    /// <see cref="DependencyOrder"/> for what counts. Common is what every
    /// area uses and uses none of them. Data, the entities and the
    /// repositories they are read through, is shared by the areas after it,
    /// since an account's deletion and export reach its recognitions too.
    /// Administration, which manages the accounts, comes after identity.
    /// </summary>
    public class CoreLayoutTests
    {
        private static readonly DependencyOrder Order = new(
            typeof(MessageResponse).Assembly,
            "AnimalClassifier.Core",
            "Common", "Data", "Identity", "Recognitions", "Admin");

        [Fact]
        public void EveryAreaOfCore_IsInTheDependencyOrder() =>
            Order.AssertEveryPartIsListed();

        [Fact]
        public void EachAreaOfCore_UsesOnlyTheAreasBeforeIt() =>
            Order.AssertEachPartUsesOnlyThePartsBeforeIt();
    }
}
