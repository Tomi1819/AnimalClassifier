namespace AnimalClassifier.Tests
{
    using AnimalClassifier.Infrastructure.Data.Models;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Identity;
    using Microsoft.Extensions.DependencyInjection;

    /// <summary>
    /// Stands in for an authenticator, which no test has. It answers the way
    /// Identity's own handler would once the cryptography has checked out, so
    /// that what the app does on either side of that can be tested: the
    /// account an attestation is credited to, and the session an assertion
    /// ends in.
    ///
    /// Like the real handler it puts the account into the attestation state and
    /// reads it back out, since that is the only record of who a new passkey
    /// belongs to.
    /// </summary>
    public class StubPasskeyHandler : IPasskeyHandler<ApplicationUser>
    {
        private const string OptionsJson = """{"challenge":"test"}""";

        private readonly byte[] credentialId = Guid.NewGuid().ToByteArray();

        /// <summary>
        /// The account the next attestation reports, in place of the one in the
        /// state. This is what a tampered state would look like from the app's
        /// side of the handler.
        /// </summary>
        public string? AttestationUserId { get; set; }

        /// <summary>
        /// The account the next assertion reports. Left unset, the assertion
        /// fails the way an unknown credential would.
        /// </summary>
        public string? AssertionUserId { get; set; }

        public Task<PasskeyCreationOptionsResult> MakeCreationOptionsAsync(PasskeyUserEntity userEntity, HttpContext httpContext) =>
            Task.FromResult(new PasskeyCreationOptionsResult
            {
                CreationOptionsJson = OptionsJson,
                AttestationState = userEntity.Id
            });

        public Task<PasskeyRequestOptionsResult> MakeRequestOptionsAsync(ApplicationUser? user, HttpContext httpContext) =>
            Task.FromResult(new PasskeyRequestOptionsResult
            {
                RequestOptionsJson = OptionsJson,
                AssertionState = user?.Id ?? string.Empty
            });

        public Task<PasskeyAttestationResult> PerformAttestationAsync(PasskeyAttestationContext context)
        {
            var userId = AttestationUserId ?? context.AttestationState ?? string.Empty;

            return Task.FromResult(PasskeyAttestationResult.Success(CreatePasskey(), new PasskeyUserEntity
            {
                Id = userId,
                Name = userId,
                DisplayName = userId
            }));
        }

        public async Task<PasskeyAssertionResult<ApplicationUser>> PerformAssertionAsync(PasskeyAssertionContext context)
        {
            if (AssertionUserId is null)
            {
                return PasskeyAssertionResult.Fail<ApplicationUser>(new PasskeyException("No such credential."));
            }

            // The request's own scope, so the user is tracked by the same
            // context the app goes on to use.
            var userManager = context.HttpContext.RequestServices.GetRequiredService<UserManager<ApplicationUser>>();
            var user = await userManager.FindByIdAsync(AssertionUserId);

            return user is null
                ? PasskeyAssertionResult.Fail<ApplicationUser>(new PasskeyException("No such credential."))
                : PasskeyAssertionResult.Success(CreatePasskey(), user);
        }

        private UserPasskeyInfo CreatePasskey() =>
            new(credentialId,
                publicKey: [1, 2, 3],
                createdAt: DateTimeOffset.UtcNow,
                signCount: 0,
                transports: ["internal"],
                isUserVerified: true,
                isBackupEligible: false,
                isBackedUp: false,
                attestationObject: [],
                clientDataJson: []);
    }
}
