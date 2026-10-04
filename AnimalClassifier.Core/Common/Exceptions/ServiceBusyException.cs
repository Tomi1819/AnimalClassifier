namespace AnimalClassifier.Core.Common.Exceptions
{
    /// <summary>
    /// The request was fine, but the server has too much of its kind of work
    /// in hand to take it on now. Trying again shortly may well succeed.
    /// </summary>
    public class ServiceBusyException : DomainException
    {
        public ServiceBusyException(string message)
            : base(message)
        {
        }
    }
}
