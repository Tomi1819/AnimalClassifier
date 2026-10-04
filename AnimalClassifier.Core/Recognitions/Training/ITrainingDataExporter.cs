namespace AnimalClassifier.Core.Recognitions.Training
{
    /// <summary>
    /// The accepted feedback, as images to train the model on.
    /// </summary>
    public interface ITrainingDataExporter
    {
        /// <summary>
        /// A ZIP archive of the image of every feedback that allows training
        /// and was accepted, in a folder per animal, with a manifest of where
        /// each came from:
        /// <list type="bullet">
        /// <item><description>
        /// <c>dataset/&lt;animal&gt;/&lt;id&gt;.jpg</c> for an animal the model
        /// knows, which merges into the folder it was trained from;
        /// </description></item>
        /// <item><description>
        /// <c>unlisted/&lt;animal&gt;/&lt;id&gt;.jpg</c> for one it does not;
        /// </description></item>
        /// <item><description>
        /// <c>manifest.csv</c>, naming each image's animal, what the model
        /// took it for and how sure it was, and what its user said.
        /// </description></item>
        /// </list>
        /// It names no user. An image whose file is gone is left out.
        /// </summary>
        /// <returns>The archive, from its start, which deletes itself once closed.</returns>
        Task<Stream> ExportAsync(CancellationToken cancellationToken);
    }
}
