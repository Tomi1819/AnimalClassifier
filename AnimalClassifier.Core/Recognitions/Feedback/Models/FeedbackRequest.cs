namespace AnimalClassifier.Core.Recognitions.Feedback.Models
{
    using AnimalClassifier.Core.Data.Entities;
    using System.Text.Json.Serialization;

    /// <summary>
    /// What a user says of one of their recognitions, which replaces whatever
    /// they said of it before.
    /// </summary>
    public class FeedbackRequest
    {
        /// <summary>
        /// Written by its name, such as <c>"WrongAnimal"</c>.
        /// </summary>
        [JsonConverter(typeof(JsonStringEnumConverter<FeedbackVerdict>))]
        public required FeedbackVerdict Verdict { get; set; }

        /// <summary>
        /// The animal the image shows, named only when the model was wrong:
        /// one of the animals it knows for <see cref="FeedbackVerdict.WrongAnimal"/>,
        /// and any other for <see cref="FeedbackVerdict.UnlistedAnimal"/>.
        /// </summary>
        public string? ActualAnimal { get; set; }

        public string? Comment { get; set; }

        /// <summary>
        /// Whether the image may be used to train the model, once an
        /// administrator has checked what the user said of it.
        /// </summary>
        public bool AllowsTraining { get; set; }
    }
}
