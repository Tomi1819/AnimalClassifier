namespace AnimalClassifier.Core.Identity.Passkeys
{
    using AnimalClassifier.Core.Common.Exceptions;
    using AnimalClassifier.Core.Data.Entities;
    using AnimalClassifier.Core.Identity.Authentication;
    using AnimalClassifier.Core.Identity.Authentication.Models;
    using AnimalClassifier.Core.Identity.Passkeys.Models;
    using AnimalClassifier.Core.Identity.Passwords;
    using AnimalClassifier.Core.Identity.SecurityAlerts;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Identity;
    using Microsoft.AspNetCore.WebUtilities;
    using System.Text.Json.Nodes;
    using static AnimalClassifier.Core.Identity.Authentication.AuthenticationMessages;
    using static AnimalClassifier.Core.Identity.Passkeys.PasskeyMessages;

    public class PasskeyService : IPasskeyService
    {
        private const string UnnamedPasskey = "Passkey";

        private readonly UserManager<ApplicationUser> userManager;
        private readonly IPasskeyHandler<ApplicationUser> passkeyHandler;
        private readonly IPasskeyStateProtector stateProtector;
        private readonly IPasswordConfirmer passwordConfirmer;
        private readonly IAccessTokenIssuer tokenIssuer;
        private readonly ISecurityAlertSender securityAlertSender;

        public PasskeyService(UserManager<ApplicationUser> userManager,
                              IPasskeyHandler<ApplicationUser> passkeyHandler,
                              IPasskeyStateProtector stateProtector,
                              IPasswordConfirmer passwordConfirmer,
                              IAccessTokenIssuer tokenIssuer,
                              ISecurityAlertSender securityAlertSender)
        {
            this.userManager = userManager;
            this.passkeyHandler = passkeyHandler;
            this.stateProtector = stateProtector;
            this.passwordConfirmer = passwordConfirmer;
            this.tokenIssuer = tokenIssuer;
            this.securityAlertSender = securityAlertSender;
        }

        public async Task<PasskeyOptionsResponse> CreateRegistrationOptionsAsync(string userId, PasskeyRegistrationOptionsRequest request, HttpContext httpContext)
        {
            var user = await userManager.GetByIdAsync(userId);

            await passwordConfirmer.ConfirmAsync(user, request.Password);

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
            var user = await userManager.GetByIdAsync(userId);

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
                throw new RequestRefusedException(RejectedPasskey);
            }

            var passkey = result.Passkey;
            var name = string.IsNullOrWhiteSpace(request.Name) ? UnnamedPasskey : request.Name.Trim();
            passkey.Name = name;

            (await userManager.AddOrUpdatePasskeyAsync(user, passkey)).ThrowIfFailed();
            await securityAlertSender.PasskeyAddedAsync(user, name);

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
                throw new AuthenticationFailedException(InvalidPasskey);
            }

            var user = result.User;

            // Identity checks the signature, not the account. Without this an
            // administrator could lock a user who still held a passkey and
            // watch them sign straight back in.
            if (await userManager.IsLockedOutAsync(user))
            {
                throw new AuthenticationFailedException(LockedOutAccount);
            }

            // The counter moves on with every use, and storing it is what lets
            // a replayed assertion be told from a fresh one.
            (await userManager.AddOrUpdatePasskeyAsync(user, result.Passkey)).ThrowIfFailed();

            return await tokenIssuer.IssueAsync(user);
        }

        public async Task<IEnumerable<PasskeySummary>> GetPasskeysAsync(string userId)
        {
            var user = await userManager.GetByIdAsync(userId);
            var passkeys = await userManager.GetPasskeysAsync(user);

            return passkeys.Select(ToSummary)
                           .OrderByDescending(passkey => passkey.DateAdded)
                           .ToList();
        }

        public async Task RemoveAsync(string userId, string passkeyId)
        {
            var user = await userManager.GetByIdAsync(userId);
            var credentialId = DecodeId(passkeyId);

            // Scoped to the owner, so that knowing an id is not enough to take
            // somebody else's passkey away from them.
            var passkey = await userManager.GetPasskeyAsync(user, credentialId)
                ?? throw new NotFoundException(PasskeyNotFound);

            (await userManager.RemovePasskeyAsync(user, credentialId)).ThrowIfFailed();
            await securityAlertSender.PasskeyRemovedAsync(user, passkey.Name ?? UnnamedPasskey);
        }

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
            request.Credential?.ToJsonString() ?? throw new RequestRefusedException(RejectedPasskey);

        private static byte[] DecodeId(string passkeyId)
        {
            try
            {
                return WebEncoders.Base64UrlDecode(passkeyId);
            }
            catch (FormatException)
            {
                // Never an id this server handed out, so there is nothing to find.
                throw new NotFoundException(PasskeyNotFound);
            }
        }
    }
}
