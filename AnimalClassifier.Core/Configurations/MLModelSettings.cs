namespace AnimalClassifier.Core.Configurations
{
    using AnimalClassifier.Core.Common.Settings;

    public class MLModelSettings : ISettings
    {
        public static string SectionName => "MLModel";

        public string Path { get; set; } = string.Empty;
    }
}
