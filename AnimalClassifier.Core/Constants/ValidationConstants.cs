namespace AnimalClassifier.Core.Constants
{
    public static class ValidationConstants
    {
        /// <summary>
        /// The longest name an account can go by, whether it is given at
        /// registration or changed later. The frontend's fields match it.
        /// </summary>
        public const int FullNameMaxLength = 100;

        /// <summary>
        /// The shortest password an account can be given, whether at
        /// registration, by a change or by a reset. The frontend's fields
        /// match it. A password set before it was raised keeps working.
        /// </summary>
        public const int PasswordMinLength = 8;
    }
}
