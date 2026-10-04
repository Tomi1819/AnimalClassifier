namespace AnimalClassifier.Extensions
{
    public static class HostEnvironmentExtension
    {
        private const string ParentFolder = "..";

        /// <summary>
        /// Whether a folder lies outside the one the app is deployed to, which a
        /// new deployment may replace whole, along with anything kept in it.
        /// </summary>
        public static bool IsOutsideContentRoot(this IHostEnvironment environment, string path)
        {
            var relativePath = Path.GetRelativePath(environment.ContentRootPath, path);

            return Path.IsPathRooted(relativePath)
                || relativePath == ParentFolder
                || relativePath.StartsWith(ParentFolder + Path.DirectorySeparatorChar, StringComparison.Ordinal);
        }
    }
}
