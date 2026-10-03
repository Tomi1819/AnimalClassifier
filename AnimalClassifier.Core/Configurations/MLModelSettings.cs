namespace AnimalClassifier.Core.Configurations
{
    using AnimalClassifier.Core.Common.Settings;
    using System.ComponentModel.DataAnnotations;

    public class MLModelSettings : ISettings
    {
        public static string SectionName => "MLModel";

        /// <summary>
        /// The trained model's file.
        /// </summary>
        [Required]
        public string Path { get; set; } = string.Empty;
    }
}
