namespace AnimalClassifier.Core.Services
{
    using AnimalClassifier.Core.Contracts;
    using AnimalClassifier.Core.DTO;
    using AnimalClassifier.Core.Extensions;
    using AnimalClassifier.Infrastructure.Data.Models;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Identity;
    using Microsoft.AspNetCore.WebUtilities;
    using System.Text.Json.Nodes;
    using static Constants.MessageConstants;

    public class PasskeyService : IPasskeyService
    {
        private const string UnnamedPasskey = "Passkey";

        private readonly UserManager<ApplicationUser> userManager;
        private readonly IPasskeyHandler<ApplicationUser> passkeyHandler;
        private readonly IPasskeyStateProtector stateProtector;
        private readonly IAccessTokenIssuer tokenIssuer;

        public PasskeyService(UserManager<ApplicationUser> userManager,
                              IPasskeyHandler<ApplicationUser> passkeyHandler,
                              IPasskeyStateProtector stateProtector,
                              IAccessTokenIssuer tokenIssuer)
        {
            this.userManager = userManager;
            this.passkeyHandler = passkeyHandler;
            this.stateProtector = stateProtector;
            this.tokenIssuer = tokenIssuer;
        }

        public async Task<PasskeyOptionsResponse> CreateRegistrationOptionsAsync(string userId, HttpContext httpContext)
        {
            var user = await FindUserAsync(userId);

            var options = await passkeyHandler.MakeCreationOptionsAsync(new PasskeyUserEntity
            {
                Id = user.Id,
                Name = user.UserName ?? string.Empty,

                // What the authenticator shows the user when it offers them a
                // choice of accounts, so it is the name rather than the login.
                DisplayName = user.FullName
            }, httpContext);

            return Respond(options.CreationOptionsJson, PasskeyCeremony.Attestation, options.AttestationState);
        }

        public async Task<PasskeySummary> RegisterAsync(string userId, PasskeyRegistrationRequest request, HttpContext httpContext)
        {
            var user = await FindUserAsync(userId);

            var result = await passkeyHandler.PerformAttestationAsync(new PasskeyAttestationContext
            {
                HttpContext = httpContext,
                CredentialJson = ReadCredential(request),
                AttestationState = stateProtector.Unprotect(PasskeyCeremony.Attestation, request.State)
            });

            // An attestation says nothing about who it belongs to; the state it
            // answers is the only record of that. Identity cannot check the two
            // agree, having nothing to compare against, so it is checked here.
            if (!result.Succeeded || result.UserEntity.Id != user.Id)
            {
                throw new InvalidOperationException(RejectedPasskey);
            }

            var passkey = result.Passkey;
            passkey.Name = string.IsNullOrWhiteSpace(request.Name) ? UnnamedPasskey : request.Name.Trim();

            (await userManager.AddOrUpdatePasskeyAsync(user, passkey)).ThrowIfFailed();

            return ToSummary(passkey);
        }

        public async Task<PasskeyOptionsResponse> CreateLoginOptionsAsync(HttpContext httpContext)
        {
            // Naming no user leaves the browser to offer whatever it holds for
            // this site. Naming one would mean taking an address from whoever
            // asked and answering whether it has any passkeys.
            var options = await passkeyHandler.MakeRequestOptionsAsync(user: null, httpContext);

            return Respond(options.RequestOptionsJson, PasskeyCeremony.Assertion, options.AssertionState);
        }

        public async Task<LoginResponse> LoginAsync(PasskeyCredentialRequest request, HttpContext httpContext)
        {
            var result = await passkeyHandler.PerformAssertionAsync(new PasskeyAssertionContext
            {
                HttpContext = httpContext,
                CredentialJson = ReadCredential(request),
                AssertionState = stateProtector.Unprotect(PasskeyCeremony.Assertion, request.State)
            });

            if (!result.Succeeded)
            {
                throw new UnauthorizedAccessException(InvalidPasskey);
            }

            var user = result.User;

            // Identity checks the signature, not the account. Without this an
            // administrator could lock a user who still held a passkey and
            // watch them sign straight back in.
            if (await userManager.IsLockedOutAsync(user))
            {
                throw new UnauthorizedAccessException(LockedOutAccount);
            }

            // The counter moves on with every use, and storing it is what lets
            // a replayed assertion be told from a fresh one.
            (await userManager.AddOrUpdatePasskeyAsync(user, result.Passkey)).ThrowIfFailed();

            return await tokenIssuer.IssueAsync(user);
        }

        public async Task<IEnumerable<PasskeySummary>> GetPasskeysAsync(string userId)
        {
            var user = await FindUserAsync(userId);
            var passkeys = await userManager.GetPasskeysAsync(user);

            return passkeys.Select(ToSummary)
                           .OrderByDescending(passkey => passkey.DateAdded)
                           .ToList();
        }

        public async Task RemoveAsync(string userId, string passkeyId)
        {
            var user = await FindUserAsync(userId);
            var credentialId = DecodeId(passkeyId);

            // Scoped to the owner, so that knowing an id is not enough to take
            // somebody else's passkey away from them.
            if (await userManager.GetPasskeyAsync(user, credentialId) is null)
            {
                throw new KeyNotFoundException(PasskeyNotFound);
            }

            (await userManager.RemovePasskeyAsync(user, credentialId)).ThrowIfFailed();
        }

        private async Task<ApplicationUser> FindUserAsync(string userId) =>
            await userManager.FindByIdAsync(userId) ?? throw new KeyNotFoundException(UserNotFound);

        // Identity leaves room for a ceremony that needs nothing remembered,
        // which these two are not. Carrying an empty state rather than
        // refusing here lets the ceremony fail where it is checked, with the
        // answer every other unusable state gets.
        private PasskeyOptionsResponse Respond(string optionsJson, PasskeyCeremony ceremony, string? state) =>
            new()
            {
                Options = JsonNode.Parse(optionsJson),
                State = stateProtector.Protect(ceremony, state ?? string.Empty)
            };

        private static PasskeySummary ToSummary(UserPasskeyInfo passkey) =>
            new()
            {
                Id = WebEncoders.Base64UrlEncode(passkey.CredentialId),
                Name = passkey.Name ?? UnnamedPasskey,
                DateAdded = passkey.CreatedAt.UtcDateTime
            };

        private static string ReadCredential(PasskeyCredentialRequest request) =>
            request.Credential?.ToJsonString() ?? throw new InvalidOperationException(RejectedPasskey);

        private static byte[] DecodeId(string passkeyId)
        {
            try
            {
                return WebEncoders.Base64UrlDecode(passkeyId);
            }
            catch (FormatException)
            {
                // Never an id this server handed out, so there is nothing to find.
                throw new KeyNotFoundException(PasskeyNotFound);
            }
        }
    }
}
