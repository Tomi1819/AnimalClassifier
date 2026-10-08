namespace AnimalClassifier.Identity
{
    using AnimalClassifier.Core.Data.Entities;
    using AnimalClassifier.Core.Identity.Authentication;
    using Microsoft.AspNetCore.Identity;

    /// <summary>
    /// Checks with Identity's sign-in manager, which also refuses an account
    /// its options keep from signing in, such as an unconfirmed one where
    /// confirmation is required.
    /// </summary>
    public class SignInManagerPasswordChecker : IPasswordSignInChecker
    {
        private readonly SignInManager<ApplicationUser> signInManager;

        public SignInManagerPasswordChecker(SignInManager<ApplicationUser> signInManager)
        {
            this.signInManager = signInManager;
        }

        public async Task<PasswordSignInResult> CheckAsync(ApplicationUser user, string password)
        {
            var result = await signInManager.CheckPasswordSignInAsync(user, password, lockoutOnFailure: true);

            if (result.IsLockedOut)
            {
                return PasswordSignInResult.LockedOut;
            }

            return result.Succeeded ? PasswordSignInResult.Succeeded : PasswordSignInResult.Failed;
        }
    }
}
