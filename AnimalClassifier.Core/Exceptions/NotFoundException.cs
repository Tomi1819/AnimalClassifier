namespace AnimalClassifier.Core.Exceptions
{
    /// <summary>
    /// What the request names does not exist, or is not the caller's to reach.
    /// The two are not told apart, so that an id cannot be tested for by
    /// asking.
    /// </summary>
    public class NotFoundException : DomainException
    {
        public NotFoundException(string message)
            : base(message)
        {
        }
    }
}
