namespace AnimalClassifier.Core.Services
{
    using AnimalClassifier.Core.Contracts;
    using AnimalClassifier.Infrastructure.Data.Models;
    using Microsoft.AspNetCore.Identity;
    using static Constants.MessageConstants;

    public class PasswordConfirmer : IPasswordConfirmer
    {
        private readonly SignInManager<ApplicationUser> signInManager;

        public PasswordConfirmer(SignInManager<ApplicationUser> signInManager)
        {
            this.signInManager = signInManager;
        }

        public async Task ConfirmAsync(ApplicationUser user, string password)
        {
            var result = await signInManager.CheckPasswordSignInAsync(user, password, lockoutOnFailure: true);

            if (result.IsLockedOut)
            {
                throw new InvalidOperationException(LockedOutAccount);
            }

            if (!result.Succeeded)
            {
                throw new InvalidOperationException(IncorrectCurrentPassword);
            }
        }
    }
}
