namespace AnimalClassifier.Core.Recognitions.Feedback
{
    using AnimalClassifier.Core.Common.Exceptions;
    using AnimalClassifier.Infrastructure.Data.Models;
    using System.Text.RegularExpressions;
    using static AnimalClassifier.Core.Recognitions.Feedback.FeedbackMessages;

    /// <summary>
    /// What a feedback has to say for it to be kept, and the form it is kept
    /// in.
    ///
    /// The animal it names may become the name of a folder a model is trained
    /// from, so an animal the model does not know can only be named in
    /// letters, and is kept in lower case, as the model's own are, so that
    /// everyone who names it names the same one.
    /// </summary>
    public static partial class FeedbackValidator
    {
        private const char Space = ' ';

        /// <returns>
        /// The animal the image shows, as it is kept: the model's own name for
        /// one it knows, and the user's tidied for one it does not. Null when
        /// the model was right.
        /// </returns>
        /// <exception cref="RequestRefusedException">
        /// When the animal does not fit the verdict: one named when the model
        /// was right, none named when it was wrong, one the model does not know
        /// given as another it does, or the other way around.
        /// </exception>
        public static string? TidyActualAnimal(FeedbackVerdict verdict, string? actualAnimal, string recognizedAnimal, IReadOnlyList<string> knownAnimals)
        {
            var named = TidyWords(actualAnimal);

            return verdict switch
            {
                FeedbackVerdict.Correct => named is null ? null : throw new RequestRefusedException(AnimalNotExpected),
                FeedbackVerdict.WrongAnimal => FindKnownAnimal(Required(named), recognizedAnimal, knownAnimals),
                FeedbackVerdict.UnlistedAnimal => ValidateUnlistedAnimal(Required(named), knownAnimals),
                _ => throw new RequestRefusedException(InvalidVerdict)
            };
        }

        /// <returns>The comment without the space around it, or null for none.</returns>
        /// <exception cref="RequestRefusedException">When it is too long.</exception>
        public static string? TidyComment(string? comment)
        {
            var tidied = comment?.Trim();

            if (tidied?.Length > RecognitionFeedback.MaxCommentLength)
            {
                throw new RequestRefusedException(string.Format(CommentTooLong, RecognitionFeedback.MaxCommentLength));
            }

            return string.IsNullOrEmpty(tidied) ? null : tidied;
        }

        private static string FindKnownAnimal(string named, string recognizedAnimal, IReadOnlyList<string> knownAnimals)
        {
            var known = knownAnimals.FirstOrDefault(animal => Same(animal, named))
                ?? throw new RequestRefusedException(UnknownAnimal);

            return Same(known, recognizedAnimal) ? throw new RequestRefusedException(SameAnimal) : known;
        }

        private static string ValidateUnlistedAnimal(string named, IReadOnlyList<string> knownAnimals)
        {
            if (named.Length > RecognitionFeedback.MaxActualAnimalLength || !AnimalName().IsMatch(named))
            {
                throw new RequestRefusedException(string.Format(InvalidAnimalName, RecognitionFeedback.MaxActualAnimalLength));
            }

            if (knownAnimals.Any(animal => Same(animal, named)))
            {
                throw new RequestRefusedException(KnownAnimal);
            }

            return named.ToLowerInvariant();
        }

        private static string Required(string? named) =>
            named ?? throw new RequestRefusedException(MissingAnimal);

        // One space between the words and none around them, or null for none.
        private static string? TidyWords(string? text)
        {
            var words = text?.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);

            return words is null || words.Length == 0 ? null : string.Join(Space, words);
        }

        private static bool Same(string animal, string other) =>
            string.Equals(animal, other, StringComparison.OrdinalIgnoreCase);

        // Words of letters, joined by a space, a hyphen or an apostrophe, such
        // as "snow leopard", "aye-aye" or "Pallas's cat".
        [GeneratedRegex(@"^\p{L}+(?:[ '-]\p{L}+)*$")]
        private static partial Regex AnimalName();
    }
}
