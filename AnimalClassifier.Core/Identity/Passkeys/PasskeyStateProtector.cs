namespace AnimalClassifier.Core.Identity.Passkeys
{
    using AnimalClassifier.Core.Common.Exceptions;
    using Microsoft.AspNetCore.DataProtection;
    using System.Security.Cryptography;
    using static AnimalClassifier.Core.Identity.Passkeys.PasskeyMessages;

    public class PasskeyStateProtector : IPasskeyStateProtector
    {
        // Versioned, so that a change to what the state holds can be made
        // without anything in flight being read under the old meaning.
        private const string AttestationPurpose = "AnimalClassifier.Passkey.Attestation.v1";
        private const string AssertionPurpose = "AnimalClassifier.Passkey.Assertion.v1";

        /// <summary>
        /// How much longer than the authenticator's own timeout a state stays
        /// valid. The authenticator starts counting once the browser has been
        /// handed the options, so the state is always the older of the two,
        /// and this covers the round trips either side of it.
        /// </summary>
        private static readonly TimeSpan StateGrace = TimeSpan.FromMinutes(1);

        // The authenticator is already giving up at this point, so a state
        // that outlived it by more than the round trips is of no use to the
        // user it was issued to.
        private static readonly TimeSpan Lifetime = PasskeySettings.AuthenticatorTimeout + StateGrace;

        private readonly ITimeLimitedDataProtector attestationProtector;
        private readonly ITimeLimitedDataProtector assertionProtector;

        public PasskeyStateProtector(IDataProtectionProvider dataProtectionProvider)
        {
            attestationProtector = CreateProtector(dataProtectionProvider, AttestationPurpose);
            assertionProtector = CreateProtector(dataProtectionProvider, AssertionPurpose);
        }

        public string Protect(PasskeyCeremony ceremony, string state) =>
            ProtectorFor(ceremony).Protect(state, Lifetime);

        public string Unprotect(PasskeyCeremony ceremony, string state)
        {
            try
            {
                return ProtectorFor(ceremony).Unprotect(state);
            }
            catch (CryptographicException)
            {
                throw new RequestRefusedException(ExpiredPasskeyCeremony);
            }
        }

        private ITimeLimitedDataProtector ProtectorFor(PasskeyCeremony ceremony) =>
            ceremony == PasskeyCeremony.Attestation ? attestationProtector : assertionProtector;

        private static ITimeLimitedDataProtector CreateProtector(IDataProtectionProvider provider, string purpose) =>
            provider.CreateProtector(purpose).ToTimeLimitedDataProtector();
    }
}
