namespace AnimalClassifier.Core.Extensions
{
    using Microsoft.AspNetCore.Identity;
    using static Constants.MessageConstants;

    public static class IdentityResultExtension
    {
        public static void ThrowIfFailed(this IdentityResult result)
        {
            if (!result.Succeeded)
            {
                throw new InvalidOperationException(string.Join(Space, result.Errors.Select(e => e.Description)));
            }
        }
    }
}
