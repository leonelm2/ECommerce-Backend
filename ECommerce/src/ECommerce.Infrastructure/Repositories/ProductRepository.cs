using ECommerce.Application.Interfaces; // Importa IProductRepository
using ECommerce.Domain.Entities; // Importa la Entidad de Dominio Product
using ECommerce.Infrastructure.Persistence; // Importa ApplicationDbContext
using Microsoft.EntityFrameworkCore; // Importa ToListAsync y AnyAsync

namespace ECommerce.Infrastructure.Repositories
{
    // Hereda el CRUD genérico de Repository<Product> y agrega las consultas específicas de IProductRepository
    public class ProductRepository : Repository<Product>, IProductRepository
    {
        // Constructor: pasa el DbContext a la clase base Repository<Product>
        public ProductRepository(ApplicationDbContext context) : base(context)
        {
        }

        // Trae varios productos de una sola vez recibiendo una lista de IDs (usado en CreateOrderCommandHandler para evitar N+1 queries)
        public async Task<IEnumerable<Product>> GetByIdsAsync(IEnumerable<int> ids)
        {
            // Se traduce en SQL a: SELECT * FROM Products WHERE Id IN (1, 2, 5...)
            return await _context.Products
                .Where(p => ids.Contains(p.Id))
                .ToListAsync();
        }

        // Verifica si un producto ya fue vendido en alguna orden (usado en DeleteProductCommandHandler para no borrar productos con historial de ventas)
        public async Task<bool> HasOrderItemsAsync(int productId)
        {
            // Devuelve true si existe al menos una fila en OrderItems con ese ProductId
            return await _context.OrderItems
                .AnyAsync(oi => oi.ProductId == productId);
        }
    }
}
