namespace AnimalClassifier.Core.Common.Settings
{
    /// <summary>
    /// Settings read from one section of the configuration, which they name
    /// themselves, so that whatever binds or quotes them never has to be told
    /// where they live.
    /// </summary>
    public interface ISettings
    {
        /// <summary>
        /// The configuration section the settings are read from, such as
        /// <c>Jwt</c> for <c>Jwt:SecretKey</c>.
        /// </summary>
        static abstract string SectionName { get; }
    }
}
