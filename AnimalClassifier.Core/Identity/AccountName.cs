namespace AnimalClassifier.Core.Identity
{
    using AnimalClassifier.Core.Common.Exceptions;
    using static AnimalClassifier.Core.Identity.IdentityMessages;
    using static Constants.ValidationConstants;

    /// <summary>
    /// The name an account goes by, in the form it is kept in. Registering
    /// and changing the name both go through here, so that an account can
    /// always save the name it already has.
    /// </summary>
    public static class AccountName
    {
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

            if (tidied.Length > FullNameMaxLength)
            {
                throw new RequestRefusedException(string.Format(FullNameTooLong, FullNameMaxLength));
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
                            .Select(word => char.ToUpper(word[0]) + word[1..].ToLower());

            return string.Join(Space, words);
        }
    }
}
