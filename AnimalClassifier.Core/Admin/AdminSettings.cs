namespace AnimalClassifier.Core.Admin
{
    using AnimalClassifier.Core.Common.Settings;

    public class AdminSettings : ISettings
    {
        public static string SectionName => "Admin";

        /// <summary>
        /// The account made an administrator on startup, so that a new
        /// installation has someone to manage it. The account has to be
        /// registered first. Removing the setting later demotes no one.
        /// </summary>
        public string Email { get; set; } = string.Empty;
    }
}
