using ECommerce.Application.DTOs; // Importa ProductDto
using ECommerce.Application.Interfaces; // Importa IProductRepository
using MediatR; // Importa IRequestHandler

namespace ECommerce.Application.Queries.Products; // Capa Application: Query para listar todos los productos

public class GetAllProductsQueryHandler : IRequestHandler<GetAllProductsQuery, IEnumerable<ProductDto>>
{
    private readonly IProductRepository _productRepository;

    public GetAllProductsQueryHandler(IProductRepository productRepository)
    {
        _productRepository = productRepository;
    }

    public async Task<IEnumerable<ProductDto>> Handle(GetAllProductsQuery request, CancellationToken cancellationToken)
    {
        // 1. Pide al repositorio todas las Entidades de Dominio Product guardadas en la tabla Products
        var products = await _productRepository.GetAllAsync();
        // 2. Transforma (mapea) cada Entidad de Dominio Product en un DTO de salida (ProductDto) y devuelve la lista
        return products.Select(p => new ProductDto(p.Id, p.Name, p.Description, p.Price, p.Stock, p.CategoryId));
    }
}
