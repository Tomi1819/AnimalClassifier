namespace AnimalClassifier.Tests
{
    using AnimalClassifier.Core.Common.Models;
    using AnimalClassifier.Infrastructure.Data;
    using System.Reflection;

    /// <summary>
    /// Keeps the projects depending on each other in one direction: the API on
    /// Infrastructure and Core, Infrastructure on Core, and Core on neither.
    /// Core holds what the app does, so it reaches the database, the disk,
    /// mail, the model and HTTP only through interfaces of its own, which the
    /// other two implement.
    ///
    /// An assembly counts as used when Core's compiled code names one of its
    /// types, which is what the compiler records as a reference.
    /// </summary>
    public class ProjectLayoutTests
    {
        // What Core is built on: .NET itself, the abstractions under
        // Microsoft.Extensions, Identity's user manager and stores among them,
        // Data Protection, which signs the links and the passkeys' state, and
        // the library the tokens are made with. A library outside these goes
        // into Infrastructure, behind an interface of Core's.
        private static readonly string[] CoreMayUse =
        [
            "System.",
            "Microsoft.Extensions.",
            "Microsoft.AspNetCore.DataProtection",
            "Microsoft.IdentityModel."
        ];

        private static readonly Assembly Core = typeof(MessageResponse).Assembly;
        private static readonly Assembly Infrastructure = typeof(AnimalClassifierDbContext).Assembly;
        private static readonly Assembly Api = typeof(Program).Assembly;

        [Fact]
        public void Core_UsesNeitherInfrastructureNorTheApi() =>
            AssertNone(UsedBy(Core).Where(name => name == NameOf(Infrastructure) || name == NameOf(Api)),
                "Core uses:");

        [Fact]
        public void Infrastructure_DoesNotUseTheApi() =>
            AssertNone(UsedBy(Infrastructure).Where(name => name == NameOf(Api)),
                "Infrastructure uses:");

        [Fact]
        public void Core_UsesNothingButDotNetAndWhatItIsBuiltOn() =>
            AssertNone(UsedBy(Core).Where(name => !CoreMayUse.Any(name.StartsWith)),
                "Core uses assemblies it is not built on, which belong in Infrastructure:");

        private static IEnumerable<string> UsedBy(Assembly assembly) =>
            assembly.GetReferencedAssemblies().Select(reference => reference.Name!);

        private static string NameOf(Assembly assembly) => assembly.GetName().Name!;

        // Each finding on a line of its own, as DependencyOrder reports them.
        private static void AssertNone(IEnumerable<string> findings, string heading)
        {
            var found = findings.Order().ToList();

            Assert.True(found.Count == 0, string.Join(Environment.NewLine, found.Prepend(heading)));
        }
    }
}
