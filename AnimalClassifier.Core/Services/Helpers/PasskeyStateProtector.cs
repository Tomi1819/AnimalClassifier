namespace AnimalClassifier.Core.Services.Helpers
{
    using AnimalClassifier.Core.Contracts;
    using Microsoft.AspNetCore.DataProtection;
    using Microsoft.AspNetCore.Identity;
    using Microsoft.Extensions.Options;
    using System.Security.Cryptography;
    using static Constants.MessageConstants;
    using static Constants.SecurityConstants;

    public class PasskeyStateProtector : IPasskeyStateProtector
    {
        // Versioned, so that a change to what the state holds can be made
        // without anything in flight being read under the old meaning.
        private const string AttestationPurpose = "AnimalClassifier.Passkey.Attestation.v1";
        private const string AssertionPurpose = "AnimalClassifier.Passkey.Assertion.v1";

        private readonly ITimeLimitedDataProtector attestationProtector;
        private readonly ITimeLimitedDataProtector assertionProtector;
        private readonly TimeSpan lifetime;

        public PasskeyStateProtector(IDataProtectionProvider dataProtectionProvider,
                                     IOptions<IdentityPasskeyOptions> passkeyOptions)
        {
            attestationProtector = CreateProtector(dataProtectionProvider, AttestationPurpose);
            assertionProtector = CreateProtector(dataProtectionProvider, AssertionPurpose);

            // The authenticator is already giving up at this point, so a state
            // that outlived it by more than the round trips is of no use to the
            // user it was issued to.
            lifetime = passkeyOptions.Value.AuthenticatorTimeout + PasskeyStateGrace;
        }

        public string Protect(PasskeyCeremony ceremony, string state) =>
            ProtectorFor(ceremony).Protect(state, lifetime);

        public string Unprotect(PasskeyCeremony ceremony, string state)
        {
            try
            {
                return ProtectorFor(ceremony).Unprotect(state);
            }
            catch (CryptographicException)
            {
                throw new InvalidOperationException(ExpiredPasskeyCeremony);
            }
        }

        private ITimeLimitedDataProtector ProtectorFor(PasskeyCeremony ceremony) =>
            ceremony == PasskeyCeremony.Attestation ? attestationProtector : assertionProtector;

        private static ITimeLimitedDataProtector CreateProtector(IDataProtectionProvider provider, string purpose) =>
            provider.CreateProtector(purpose).ToTimeLimitedDataProtector();
    }
}
