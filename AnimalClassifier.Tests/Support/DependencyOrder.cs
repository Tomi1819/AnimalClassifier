namespace AnimalClassifier.Tests.Support
{
    using System.Reflection;

    /// <summary>
    /// The parts of one namespace, such as the parts of identity, and the
    /// order they may depend on each other in. Each may use the parts listed
    /// before it and none listed after, so that one can be read, changed or
    /// moved knowing everything it can reach. What the parts share sits in the
    /// namespace itself, and may use none of them.
    ///
    /// A dependency counts when a type of one part names a type of another in
    /// its members: a constructor taking its service, a field, a property, or
    /// a method's parameters and result. That includes the fields the compiler
    /// gives async methods and lambdas, so most of what a method body uses as
    /// well. Constants are copied in where they are used and leave no trace.
    /// </summary>
    public class DependencyOrder
    {
        private const BindingFlags EveryMember =
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;

        private readonly Assembly assembly;
        private readonly string rootNamespace;
        private readonly string[] parts;

        /// <param name="assembly">The assembly the namespace's types are in.</param>
        /// <param name="rootNamespace">The namespace whose parts are ordered.</param>
        /// <param name="parts">
        /// The parts, each a namespace directly under the root, in the order
        /// they may depend in. A new part goes after everything it uses and
        /// before everything that uses it.
        /// </param>
        public DependencyOrder(Assembly assembly, string rootNamespace, params string[] parts)
        {
            this.assembly = assembly;
            this.rootNamespace = rootNamespace;
            this.parts = parts;
        }

        public void AssertEveryPartIsListed()
        {
            var unlisted = Types()
                .Select(PartOf)
                .Where(part => part is not null && !parts.Contains(part))
                .Distinct()
                .ToList();

            AssertNone(unlisted, $"Parts of {rootNamespace} missing from the dependency order:");
        }

        public void AssertEachPartUsesOnlyThePartsBeforeIt()
        {
            var violations =
                from type in Types()
                let part = PartOf(type)
                from used in UsedTypes(type)
                let usedPart = PartOf(used)
                where usedPart is not null && usedPart != part && !MayUse(part, usedPart)
                select $"{Describe(type)} uses {Describe(used)}";

            AssertNone(violations.Distinct().ToList(), $"Parts of {rootNamespace} used out of order:");
        }

        // Each finding on a line of its own and in full, where Assert.Empty
        // would cut them short.
        private static void AssertNone(IReadOnlyCollection<string?> findings, string heading) =>
            Assert.True(findings.Count == 0, string.Join(Environment.NewLine, findings.Prepend(heading)));

        // What every part shares sits in the namespace itself, so it may use
        // none of them.
        private bool MayUse(string? part, string usedPart) =>
            part is not null && Array.IndexOf(parts, usedPart) < Array.IndexOf(parts, part);

        private IEnumerable<Type> Types() =>
            assembly.GetTypes().Where(type => IsInRoot(type.Namespace));

        private bool IsInRoot(string? @namespace) =>
            @namespace == rootNamespace || @namespace?.StartsWith(rootNamespace + ".", StringComparison.Ordinal) == true;

        /// <returns>
        /// The part a type belongs to, such as "Passkeys" for its models too,
        /// or null for a type outside the parts.
        /// </returns>
        private string? PartOf(Type type)
        {
            if (!IsInRoot(type.Namespace) || type.Namespace == rootNamespace)
            {
                return null;
            }

            return type.Namespace!.Substring(rootNamespace.Length + 1).Split('.')[0];
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
        // state machine or closure the compiler nested inside it, and from
        // the root namespace down.
        private string Describe(Type type)
        {
            while (type.DeclaringType is not null)
            {
                type = type.DeclaringType;
            }

            return $"{type.Namespace}.{type.Name}"[(rootNamespace.LastIndexOf('.') + 1)..];
        }
    }
}
