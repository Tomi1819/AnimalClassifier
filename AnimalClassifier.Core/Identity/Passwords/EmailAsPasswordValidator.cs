namespace AnimalClassifier.Core.Identity.Passwords
{
    using AnimalClassifier.Core.Data.Entities;
    using Microsoft.AspNetCore.Identity;
    using static AnimalClassifier.Core.Identity.Passwords.PasswordMessages;

    /// <summary>
    /// Refuses a password that is the account's own email. Anyone trying to
    /// get in already knows the address, which makes it the first guess.
    /// </summary>
    public class EmailAsPasswordValidator : IPasswordValidator<ApplicationUser>
    {
        public Task<IdentityResult> ValidateAsync(UserManager<ApplicationUser> manager, ApplicationUser user, string? password)
        {
            // Whatever the case, since changing it makes the guess no harder.
            var isEmail = string.Equals(password, user.Email, StringComparison.OrdinalIgnoreCase);

            return Task.FromResult(isEmail
                ? IdentityResult.Failed(new IdentityError { Code = nameof(PasswordIsEmail), Description = PasswordIsEmail })
                : IdentityResult.Success);
        }
    }
}
