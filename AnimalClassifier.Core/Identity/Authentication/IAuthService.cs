namespace AnimalClassifier.Core.Identity.Authentication
{
    using AnimalClassifier.Core.Common.Exceptions;
    using AnimalClassifier.Core.Identity.Authentication.Models;
    using System.Security.Claims;

    public interface IAuthService
    {
        /// <summary>
        /// Creates an account in the User role. Nobody is signed in by it.
        /// The name is tidied and each word capitalised; an account given
        /// none goes by <see cref="AccountName.Unknown"/>.
        /// </summary>
        /// <exception cref="RequestRefusedException">
        /// When the email is already registered, the name is too long, or the
        /// password fails the rules.
        /// </exception>
        Task<RegisterResponse> RegisterAsync(RegisterRequest request);

        /// <summary>
        /// Signs in with the account's password. A wrong one counts towards
        /// locking the account, which is what stops it being guessed.
        /// </summary>
        /// <returns>The token the new session runs on.</returns>
        /// <exception cref="AuthenticationFailedException">
        /// When the email or the password is wrong, or the account is locked.
        /// </exception>
        Task<LoginResponse> LoginAsync(LoginRequest request);

        /// <summary>
        /// Whether a signed token still speaks for its user: the account exists
        /// and its security stamp has not changed since the token was issued.
        /// Lockout is not checked, since anyone can cause one by entering wrong
        /// passwords and it would end the owner's session; locking an account on
        /// purpose must change its security stamp as well.
        /// </summary>
        Task<bool> IsSessionValidAsync(ClaimsPrincipal principal);
    }
}
