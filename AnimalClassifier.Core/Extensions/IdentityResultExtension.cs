namespace AnimalClassifier.Core.Extensions
{
    using AnimalClassifier.Core.Exceptions;
    using Microsoft.AspNetCore.Identity;
    using static Constants.MessageConstants;

    public static class IdentityResultExtension
    {
        /// <summary>
        /// Turns what Identity turned down into a refusal the caller is told
        /// about. Its descriptions are written for the user, such as which
        /// rule a new password fails.
        /// </summary>
        /// <exception cref="RequestRefusedException">
        /// When the result is a failure.
        /// </exception>
        public static void ThrowIfFailed(this IdentityResult result)
        {
            if (!result.Succeeded)
            {
                throw new RequestRefusedException(string.Join(Space, result.Errors.Select(e => e.Description)));
            }
        }
    }
}
