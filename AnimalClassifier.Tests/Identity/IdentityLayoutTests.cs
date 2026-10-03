namespace AnimalClassifier.Tests.Identity
{
    using AnimalClassifier.Core.Identity;
    using System.Reflection;

    /// <summary>
    /// Keeps the parts of identity depending on each other in one direction
    /// only. Each may use the parts listed before it and none listed after,
    /// so that one can be read, changed or moved knowing everything it can
    /// reach.
    ///
    /// A dependency counts when a type of one part names a type of another in
    /// its members: a constructor taking its service, a field, a property, or
    /// a method's parameters and result. That includes the fields the compiler
    /// gives async methods and lambdas, so most of what a method body uses as
    /// well. Constants are copied in where they are used and leave no trace.
    /// </summary>
    public class IdentityLayoutTests
    {
        private const BindingFlags EveryMember =
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;

        private static readonly string IdentityNamespace = typeof(AccountName).Namespace!;

        // The order the parts may depend in. A new part goes after everything
        // it uses and before everything that uses it.
        private static readonly string[] Parts = ["SecurityAlerts", "Authentication", "Passwords", "Passkeys", "Account"];

        [Fact]
        public void EveryPartOfIdentity_IsInTheDependencyOrder()
        {
            var unlisted = IdentityTypes()
                .Select(PartOf)
                .Where(part => part is not null && !Parts.Contains(part))
                .Distinct()
                .ToList();

            AssertNone(unlisted, "Parts missing from the dependency order:");
        }

        [Fact]
        public void EachPartOfIdentity_UsesOnlyThePartsBeforeIt()
        {
            var violations =
                from type in IdentityTypes()
                let part = PartOf(type)
                from used in UsedTypes(type)
                let usedPart = PartOf(used)
                where usedPart is not null && usedPart != part && !MayUse(part, usedPart)
                select $"{Describe(type)} uses {Describe(used)}";

            AssertNone(violations.Distinct().ToList(), "Parts of identity used out of order:");
        }

        // Each finding on a line of its own and in full, where Assert.Empty
        // would cut them short.
        private static void AssertNone(IReadOnlyCollection<string?> findings, string heading) =>
            Assert.True(findings.Count == 0, string.Join(Environment.NewLine, findings.Prepend(heading)));

        // What every part shares sits in Identity itself, so it may use none
        // of them.
        private static bool MayUse(string? part, string usedPart) =>
            part is not null && Array.IndexOf(Parts, usedPart) < Array.IndexOf(Parts, part);

        private static IEnumerable<Type> IdentityTypes() =>
            typeof(AccountName).Assembly.GetTypes().Where(type => IsInIdentity(type.Namespace));

        private static bool IsInIdentity(string? @namespace) =>
            @namespace == IdentityNamespace || @namespace?.StartsWith(IdentityNamespace + ".") == true;

        /// <returns>
        /// The part a type belongs to, such as "Passkeys" for its models too,
        /// or null for a type outside the parts.
        /// </returns>
        private static string? PartOf(Type type)
        {
            if (!IsInIdentity(type.Namespace) || type.Namespace == IdentityNamespace)
            {
                return null;
            }

            return type.Namespace!.Substring(IdentityNamespace.Length + 1).Split('.')[0];
        }

        private static IEnumerable<Type> UsedTypes(Type type)
        {
            var named = new List<Type?> { type.BaseType };
            named.AddRange(type.GetInterfaces());
            named.AddRange(type.GetFields(EveryMember).Select(field => field.FieldType));
            named.AddRange(type.GetProperties(EveryMember).Select(property => property.PropertyType));

            foreach (var method in type.GetMethods(EveryMember).Cast<MethodBase>().Concat(type.GetConstructors(EveryMember)))
            {
                named.AddRange(method.GetParameters().Select(parameter => parameter.ParameterType));

                if (method is MethodInfo info)
                {
                    named.Add(info.ReturnType);
                }
            }

            return named.OfType<Type>().SelectMany(Unwrap);
        }

        // Task<LoginResponse> uses LoginResponse, as does an array or a ref of it.
        private static IEnumerable<Type> Unwrap(Type type)
        {
            if (type.HasElementType)
            {
                return Unwrap(type.GetElementType()!);
            }

            var unwrapped = new List<Type> { type };

            if (type.IsGenericType)
            {
                unwrapped.AddRange(type.GetGenericArguments().SelectMany(Unwrap));
            }

            return unwrapped;
        }

        // Named by the type a reader would look for, rather than the async
        // state machine or closure the compiler nested inside it.
        private static string Describe(Type type)
        {
            while (type.DeclaringType is not null)
            {
                type = type.DeclaringType;
            }

            return $"{type.Namespace}.{type.Name}"[(IdentityNamespace.LastIndexOf('.') + 1)..];
        }
    }
}
