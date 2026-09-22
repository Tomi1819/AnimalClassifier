namespace AnimalClassifier.Core.Services.Helpers
{
    using System.Security.Cryptography;
    using System.Text;

    /// <summary>
    /// The claim that ties a token to the state of the account it was issued
    /// for. It carries a hash of the user's security stamp rather than the
    /// stamp itself, because anyone holding a token can read it and Identity
    /// derives one-time codes from the stamp.
    ///
    /// Issuing and validating both read the format from here, so a token is
    /// never checked against a claim built a different way.
    /// </summary>
    internal static class SecurityStampClaim
    {
        public const string Type = "security_stamp";

        public static string From(string securityStamp) =>
            Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(securityStamp)));
    }
}
