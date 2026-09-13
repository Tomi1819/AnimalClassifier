namespace AnimalClassifier.Core.Contracts
{
    using AnimalClassifier.Core.DTO;
    using System.Security.Claims;
    public interface IAuthService
    {
        Task<RegisterResponse> RegisterAsync(RegisterRequest request);
        Task<LoginResponse> LoginAsync(LogInRequest request);

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
