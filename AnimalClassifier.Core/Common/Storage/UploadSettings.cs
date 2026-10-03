namespace AnimalClassifier.Core.Common.Storage
{
    using AnimalClassifier.Core.Common.Settings;
    using System.ComponentModel.DataAnnotations;

    public class UploadSettings : ISettings
    {
        public static string SectionName => "FileUploadSettings";

        /// <summary>
        /// The folder uploads are kept in, relative to the content root unless
        /// it is absolute. It is served to browsers, so it must hold nothing
        /// but uploads.
        /// </summary>
        [Required]
        public string UploadPath { get; set; } = string.Empty;

        /// <summary>
        /// The path the uploads are served under, such as <c>/uploads</c>.
        /// </summary>
        [Required]
        public string RequestPath { get; set; } = string.Empty;
    }
}
