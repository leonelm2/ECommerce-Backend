using ECommerce.Domain.Entities;

namespace ECommerce.Application.Interfaces
{
    public interface IProductRepository : IRepository<Product>
    {
        Task<IEnumerable<Product>> GetByIdsAsync(IEnumerable<int> ids);

        Task<bool> HasOrderItemsAsync(int productId);
    }
}
