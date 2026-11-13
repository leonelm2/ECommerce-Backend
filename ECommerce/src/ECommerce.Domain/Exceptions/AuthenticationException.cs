namespace ECommerce.Domain.Exceptions
{
    public sealed class AuthenticationException : DomainException
    {
        public AuthenticationException(string message) : base(message)
        {
        }
    }
}
