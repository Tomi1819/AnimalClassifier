namespace AnimalClassifier.Core.Common.Exceptions
{
    /// <summary>
    /// The request was understood and turned down: a wrong password, a name
    /// that is too long, a link that has expired. The message says which, so
    /// that the user can put it right.
    /// </summary>
    public class RequestRefusedException : DomainException
    {
        public RequestRefusedException(string message)
            : base(message)
        {
        }
    }
}
