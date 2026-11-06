using ECommerce.Application.Interfaces; // Importa IOrderRepository
using ECommerce.Domain.Entities; // Importa la Entidad de Dominio Order
using ECommerce.Infrastructure.Persistence; // Importa ApplicationDbContext
using Microsoft.EntityFrameworkCore; // Importa Include, ThenInclude, AsNoTracking, FirstOrDefaultAsync

namespace ECommerce.Infrastructure.Repositories
{
    // Hereda todos los métodos básicos de Repository<Order> e implementa la interfaz específica IOrderRepository
    public class OrderRepository : Repository<Order>, IOrderRepository
    {
        // Constructor: le pasa el ApplicationDbContext al constructor de la clase padre (base)
        public OrderRepository(ApplicationDbContext context) : base(context)
        {
        }

        // Sobrescribe (override) el método GetByIdAsync porque cuando pedimos una Orden también necesitamos traer sus ítems y el nombre de cada producto
        public override async Task<Order?> GetByIdAsync(int id)
        {
            return await _context.Orders
                .Include(o => o.OrderItems) // Eager Loading (JOIN): trae la lista de OrderItems asociados a la orden
                .ThenInclude(oi => oi.Product) // Segundo JOIN: por cada OrderItem, trae también los datos de la entidad Product (para saber el nombre)
                .AsNoTracking() // Lectura rápida sin tracking de cambios
                .FirstOrDefaultAsync(o => o.Id == id); // Devuelve la orden con ese ID o null si no existe
        }
    }
}
