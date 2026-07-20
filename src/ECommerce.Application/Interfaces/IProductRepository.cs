using ECommerce.Domain.Entities;

namespace ECommerce.Application.Interfaces
{
    public interface IProductRepository : IRepository<Product>
    {
        /// <summary>
        /// Obtiene múltiples productos por sus IDs en una sola query.
        /// Elimina el problema N+1 al crear órdenes con múltiples ítems.
        /// </summary>
        Task<IEnumerable<Product>> GetByIdsAsync(IEnumerable<int> ids);
    }
}
