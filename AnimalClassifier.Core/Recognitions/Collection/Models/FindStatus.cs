namespace AnimalClassifier.Core.Recognitions.Collection.Models
{
    using AnimalClassifier.Core.Recognitions.Uploads;

    /// <summary>
    /// What one recognition did for its user's collection.
    /// </summary>
    public enum FindStatus
    {
        /// <summary>
        /// It finds no animal the model knows: the user said it shows one the
        /// model does not, or it is a video recorded as
        /// <see cref="UploadService.UnrecognisedAnimal"/>.
        /// </summary>
        None,

        /// <summary>
        /// The model was not sure enough of the animal it named, which the
        /// user has not found otherwise. It is found once they say the model
        /// was right.
        /// </summary>
        Unconfirmed,

        /// <summary>
        /// Its animal was found already by an earlier recognition, or, when
        /// the model was not sure enough of this one, by any other.
        /// </summary>
        AlreadyFound,

        /// <summary>
        /// It is the first of the user's recognitions to find its animal.
        /// </summary>
        FirstFind
    }
}
