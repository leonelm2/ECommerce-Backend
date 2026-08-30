using ECommerce.Domain.Exceptions;

namespace ECommerce.Domain.Entities
{
    public class Product
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public string Description { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int Stock { get; set; }
        public int CategoryId { get; set; }

        public void UpdatePrice(decimal newPrice)
        {
            if (newPrice < 0)
            {
                throw new DomainRuleException("El precio no puede ser negativo.");
            }

            Price = newPrice;
        }

        public void ReduceStock(int quantity)
        {
            if (quantity <= 0)
            {
                throw new DomainRuleException("La cantidad debe ser mayor a 0.");
            }

            if (quantity > Stock)
            {
                throw new InsufficientStockException($"No hay stock suficiente para el producto {Name}.");
            }

            Stock -= quantity;
        }
    }
}
