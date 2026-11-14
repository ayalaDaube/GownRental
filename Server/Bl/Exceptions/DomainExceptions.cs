namespace Bl.Exceptions
{
    /// <summary>
    /// A problem that the caller can understand and fix (as opposed to a bug or an outage).
    /// The message is safe to show to the user; the API turns each kind into the matching HTTP status.
    /// </summary>
    public abstract class DomainException : Exception
    {
        protected DomainException(string message) : base(message)
        {
        }
    }

    /// <summary>The request is malformed or breaks a rule (HTTP 400).</summary>
    public class InvalidRequestException : DomainException
    {
        public InvalidRequestException(string message) : base(message)
        {
        }
    }

    /// <summary>The requested record does not exist (HTTP 404).</summary>
    public class NotFoundException : DomainException
    {
        public NotFoundException(string message) : base(message)
        {
        }
    }

    /// <summary>The request is valid but clashes with the current state, for example a gown already rented (HTTP 409).</summary>
    public class ConflictException : DomainException
    {
        public ConflictException(string message) : base(message)
        {
        }
    }
}
