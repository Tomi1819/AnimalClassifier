namespace AnimalClassifier.Infrastructure.Classification
{
    using AnimalClassifier.Core.Common.Settings;
    using System.ComponentModel.DataAnnotations;

    public class MLModelSettings : ISettings
    {
        public static string SectionName => "MLModel";

        /// <summary>
        /// The trained model's file, relative to the content root unless it
        /// is absolute.
        /// </summary>
        [Required]
        public string Path { get; set; } = string.Empty;
    }
}
