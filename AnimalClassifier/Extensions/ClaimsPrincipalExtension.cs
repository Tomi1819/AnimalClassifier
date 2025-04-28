namespace AnimalClassifier.Extensions
{
    using System.Security.Claims;
    public static class ClaimsPrincipalExtension
    {
        public static string? Id(this ClaimsPrincipal user)
            => user.FindFirstValue(ClaimTypes.NameIdentifier);
    }
}

