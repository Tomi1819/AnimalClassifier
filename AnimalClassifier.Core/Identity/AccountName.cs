namespace AnimalClassifier.Core.Identity
{
    using AnimalClassifier.Core.Common.Exceptions;
    using System.Globalization;
    using static AnimalClassifier.Core.Identity.IdentityMessages;

    /// <summary>
    /// The name an account goes by, in the form it is kept in. Registering
    /// and changing the name both go through here, so that an account can
    /// always save the name it already has.
    /// </summary>
    public static class AccountName
    {
        /// <summary>
        /// The longest name an account can go by, whether it is given at
        /// registration or changed later. The frontend's fields match it.
        /// </summary>
        public const int MaxLength = 100;

        /// <summary>
        /// The name of an account registered without one.
        /// </summary>
        public const string Unknown = "Unknown user";

        private const char Space = ' ';

        /// <summary>
        /// Leaves one space between the words and none around them. The
        /// letters stay as typed.
        /// </summary>
        /// <returns>
        /// The tidied name, which is empty when there was nothing in it.
        /// </returns>
        /// <exception cref="RequestRefusedException">
        /// When the name is too long.
        /// </exception>
        public static string Tidy(string name)
        {
            var words = name.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
            var tidied = string.Join(Space, words);

            if (tidied.Length > MaxLength)
            {
                throw new RequestRefusedException(string.Format(CultureInfo.InvariantCulture, FullNameTooLong, MaxLength));
            }

            return tidied;
        }

        /// <summary>
        /// Starts each word with a capital and puts the rest of it in lower
        /// case, which leaves the length as it was.
        /// </summary>
        public static string Capitalise(string name)
        {
            var words = name.Split(Space, StringSplitOptions.RemoveEmptyEntries)
                            .Select(word => char.ToUpperInvariant(word[0]) + word[1..].ToLowerInvariant());

            return string.Join(Space, words);
        }
    }
}
