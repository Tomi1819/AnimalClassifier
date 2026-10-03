namespace AnimalClassifier.Core.Common.Exceptions
{
    /// <summary>
    /// A failure the caller is told about. Its message is written for the
    /// user, which is what sets it apart from every other exception: those
    /// describe what went wrong inside the app, and must not reach a response.
    /// </summary>
    public abstract class DomainException : Exception
    {
        protected DomainException(string message)
            : base(message)
        {
        }
    }
}
