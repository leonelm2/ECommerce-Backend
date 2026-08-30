namespace ECommerce.Domain.Exceptions
{
    public sealed class InsufficientStockException : DomainException
    {
        public InsufficientStockException(string message) : base(message)
        {
        }
    }
}
