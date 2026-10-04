namespace AnimalClassifier.Hosting
{
    using AnimalClassifier.Core.Common.Settings;

    public class DataProtectionSettings : ISettings
    {
        public static string SectionName => "DataProtection";

        /// <summary>
        /// The folder the keys are kept in, relative to the content root unless
        /// it is absolute. They sign and encrypt the password reset and email
        /// confirmation links, the passkey ceremonies' state and the media
        /// links, so losing them breaks every one of those in flight, and
        /// anyone who reads them can forge one. Development may leave it
        /// empty, and keeps them in the user's profile.
        /// </summary>
        public string KeysPath { get; set; } = string.Empty;
    }
}
