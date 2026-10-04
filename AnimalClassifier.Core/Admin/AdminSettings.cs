namespace AnimalClassifier.Core.Admin
{
    using AnimalClassifier.Core.Common.Settings;

    public class AdminSettings : ISettings
    {
        public static string SectionName => "Admin";

        /// <summary>
        /// The account made an administrator on startup while there is none,
        /// so that a new installation has someone to manage it. The account
        /// has to be registered and its email confirmed first. Once there is
        /// an administrator it does nothing, and removing it demotes no one.
        /// </summary>
        public string Email { get; set; } = string.Empty;
    }
}
