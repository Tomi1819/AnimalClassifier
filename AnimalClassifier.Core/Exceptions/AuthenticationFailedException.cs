namespace AnimalClassifier.Core.Exceptions
{
    /// <summary>
    /// The caller could not be signed in: the credentials were wrong, or the
    /// account they belong to is locked.
    /// </summary>
    public class AuthenticationFailedException : DomainException
    {
        public AuthenticationFailedException(string message)
            : base(message)
        {
        }
    }
}
