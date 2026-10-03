namespace AnimalClassifier.ErrorHandling
{
    /// <summary>
    /// What the API tells the user when a request fails for a reason no
    /// service gave. Each is written to be shown as it stands.
    /// </summary>
    public static class ErrorMessages
    {
        public const string UnexpectedError = "Something went wrong on our side. Please try again in a moment.";
        public const string InvalidRequest = "The request could not be understood.";
        public const string RequestTooLarge = "The request is too large.";
        public const string TooManyRequests = "Too many attempts. Please wait a few minutes and try again.";
    }
}
