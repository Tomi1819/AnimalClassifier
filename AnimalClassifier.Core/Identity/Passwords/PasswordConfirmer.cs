namespace AnimalClassifier.Core.Identity.Passwords
{
    using AnimalClassifier.Core.Common.Exceptions;
    using AnimalClassifier.Infrastructure.Data.Models;
    using Microsoft.AspNetCore.Identity;
    using static Constants.MessageConstants;

    public class PasswordConfirmer : IPasswordConfirmer
    {
        private readonly UserManager<ApplicationUser> userManager;
        private readonly IPasswordConfirmationLimiter limiter;

        public PasswordConfirmer(UserManager<ApplicationUser> userManager, IPasswordConfirmationLimiter limiter)
        {
            this.userManager = userManager;
            this.limiter = limiter;
        }

        public async Task ConfirmAsync(ApplicationUser user, string password)
        {
            // Counted before the password is looked at, right or wrong, so
            // that requests sent side by side cannot slip extra guesses past
            // the limit.
            if (!limiter.TryAcquire(user.Id))
            {
                throw new RequestRefusedException(TooManyPasswordAttempts);
            }

            // The password alone, rather than the check signing in makes,
            // which would refuse an account that is locked out and count a
            // wrong guess towards locking it.
            if (!await userManager.CheckPasswordAsync(user, password))
            {
                throw new RequestRefusedException(IncorrectCurrentPassword);
            }
        }
    }
}
