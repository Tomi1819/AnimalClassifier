namespace AnimalClassifier.Identity
{
    using AnimalClassifier.Core.Data.Entities;
    using AnimalClassifier.Core.Identity.Passkeys;
    using AnimalClassifier.Core.Identity.Passkeys.Models;
    using Microsoft.AspNetCore.Identity;

    /// <summary>
    /// Runs the ceremonies with Identity's passkey handler, against the
    /// request being answered.
    /// </summary>
    public class IdentityWebAuthnHandler : IWebAuthnHandler
    {
        private const string NoRequest = "A passkey ceremony can only be run while a request is being answered.";

        private readonly IPasskeyHandler<ApplicationUser> passkeyHandler;
        private readonly IHttpContextAccessor httpContextAccessor;

        public IdentityWebAuthnHandler(IPasskeyHandler<ApplicationUser> passkeyHandler, IHttpContextAccessor httpContextAccessor)
        {
            this.passkeyHandler = passkeyHandler;
            this.httpContextAccessor = httpContextAccessor;
        }

        private HttpContext HttpContext => httpContextAccessor.HttpContext ?? throw new InvalidOperationException(NoRequest);

        public async Task<PasskeyChallenge> CreateRegistrationOptionsAsync(ApplicationUser user)
        {
            var options = await passkeyHandler.MakeCreationOptionsAsync(new PasskeyUserEntity
            {
                Id = user.Id,
                Name = user.UserName ?? string.Empty,

                // What the authenticator shows the user when it offers them a
                // choice of accounts, so it is the name rather than the login.
                DisplayName = user.FullName
            }, HttpContext);

            return new PasskeyChallenge { OptionsJson = options.CreationOptionsJson, State = options.AttestationState };
        }

        public async Task<VerifiedAttestation?> VerifyAttestationAsync(string credentialJson, string state)
        {
            var result = await passkeyHandler.PerformAttestationAsync(new PasskeyAttestationContext
            {
                HttpContext = HttpContext,
                CredentialJson = credentialJson,
                AttestationState = state
            });

            return result.Succeeded
                ? new VerifiedAttestation { UserId = result.UserEntity.Id, Passkey = result.Passkey }
                : null;
        }

        public async Task<PasskeyChallenge> CreateLoginOptionsAsync()
        {
            var options = await passkeyHandler.MakeRequestOptionsAsync(user: null, HttpContext);

            return new PasskeyChallenge { OptionsJson = options.RequestOptionsJson, State = options.AssertionState };
        }

        public async Task<VerifiedAssertion?> VerifyAssertionAsync(string credentialJson, string state)
        {
            var result = await passkeyHandler.PerformAssertionAsync(new PasskeyAssertionContext
            {
                HttpContext = HttpContext,
                CredentialJson = credentialJson,
                AssertionState = state
            });

            return result.Succeeded
                ? new VerifiedAssertion { User = result.User, Passkey = result.Passkey }
                : null;
        }
    }
}
