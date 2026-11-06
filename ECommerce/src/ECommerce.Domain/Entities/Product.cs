using ECommerce.Domain.Exceptions; // Importa DomainRuleException e InsufficientStockException

namespace ECommerce.Domain.Entities // Capa Domain: núcleo del negocio
{
    // ENTIDAD DE DOMINIO "Product": Representa un producto del catálogo y aplica DOMINIO RICO.
    // No es una clase anémica de solo propiedades: contiene métodos que protegen sus propias reglas de negocio (precio y stock).
    public class Product
    {
        public int Id { get; set; } // Clave primaria (Primary Key) del producto en la BD
        public string Name { get; set; } = null!; // Nombre del producto
        public string Description { get; set; } = string.Empty; // Descripción del producto
        public decimal Price { get; set; } // Precio unitario actual
        public int Stock { get; set; } // Cantidad de unidades disponibles en inventario
        public int CategoryId { get; set; } // ID de la categoría a la que pertenece

        // REGLA DE NEGOCIO 1: Método de dominio para actualizar el precio protegiendo que nunca sea negativo
        public void UpdatePrice(decimal newPrice)
        {
            // Si intentan asignar un precio menor a 0, la propia entidad se defiende lanzando DomainRuleException (422)
            if (newPrice < 0)
            {
                throw new DomainRuleException("El precio no puede ser negativo.");
            }

            // Si es válido, actualiza la propiedad Price
            Price = newPrice;
        }

        // REGLA DE NEGOCIO 2: Método de dominio para descontar stock cuando se crea una orden
        public void ReduceStock(int quantity)
        {
            // Valida que la cantidad a descontar sea positiva
            if (quantity <= 0)
            {
                throw new DomainRuleException("La cantidad debe ser mayor a 0.");
            }

            // Valida que alcance el stock actual; si piden más de lo que hay, lanza InsufficientStockException (422 Unprocessable Entity)
            if (quantity > Stock)
            {
                throw new InsufficientStockException($"No hay stock suficiente para el producto {Name}.");
            }

            // Si hay stock suficiente, descuenta la cantidad del inventario
            Stock -= quantity;
        }
    }
}
