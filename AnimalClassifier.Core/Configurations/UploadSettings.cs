namespace AnimalClassifier.Core.Configurations
{
    using AnimalClassifier.Core.Common.Settings;

    public class UploadSettings : ISettings
    {
        public static string SectionName => "FileUploadSettings";

        public string UploadPath { get; set; } = string.Empty;
        public string RequestPath { get; set; } = string.Empty;
    }
}
