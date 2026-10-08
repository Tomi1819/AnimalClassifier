namespace AnimalClassifier.Infrastructure.Storage
{
    using AnimalClassifier.Core.Common.Settings;
    using System.ComponentModel.DataAnnotations;

    public class UploadSettings : ISettings
    {
        public static string SectionName => "FileUploadSettings";

        /// <summary>
        /// The folder uploads are kept in, relative to the content root unless
        /// it is absolute.
        /// </summary>
        [Required]
        public string UploadPath { get; set; } = string.Empty;
    }
}
