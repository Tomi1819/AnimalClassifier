namespace AnimalClassifier.Tests.Identity
{
    using AnimalClassifier.Core.Identity;
    using AnimalClassifier.Tests.Support;

    /// <summary>
    /// Keeps the parts of identity depending on each other in one direction
    /// only; see <see cref="DependencyOrder"/> for what counts.
    /// </summary>
    public class IdentityLayoutTests
    {
        private static readonly DependencyOrder Order = new(
            typeof(AccountName).Assembly,
            typeof(AccountName).Namespace!,
            "SecurityAlerts", "Authentication", "Passwords", "Passkeys", "Account");

        [Fact]
        public void EveryPartOfIdentity_IsInTheDependencyOrder() =>
            Order.AssertEveryPartIsListed();

        [Fact]
        public void EachPartOfIdentity_UsesOnlyThePartsBeforeIt() =>
            Order.AssertEachPartUsesOnlyThePartsBeforeIt();
    }
}
