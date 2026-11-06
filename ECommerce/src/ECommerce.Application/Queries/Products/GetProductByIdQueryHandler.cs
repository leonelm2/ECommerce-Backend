using ECommerce.Application.DTOs; // Importa ProductDto
using ECommerce.Application.Interfaces; // Importa IProductRepository
using ECommerce.Domain.Exceptions; // Importa NotFoundException
using MediatR; // Importa IRequestHandler

namespace ECommerce.Application.Queries.Products; // Capa Application: Query para buscar un producto por su ID

public class GetProductByIdQueryHandler : IRequestHandler<GetProductByIdQuery, ProductDto>
{
    private readonly IProductRepository _productRepository;

    public GetProductByIdQueryHandler(IProductRepository productRepository)
    {
        _productRepository = productRepository;
    }

    public async Task<ProductDto> Handle(GetProductByIdQuery request, CancellationToken cancellationToken)
    {
        // 1. Busca la Entidad de Dominio Product por ID en la base de datos
        var product = await _productRepository.GetByIdAsync(request.Id);
        // 2. Si no existe, lanza NotFoundException (que GlobalExceptionHandler convierte en un HTTP 404 Not Found)
        if (product is null)
            throw new NotFoundException($"Product with id {request.Id} not found.");

        // 3. Empaqueta la Entidad de Dominio en el DTO de salida (ProductDto) y lo devuelve
        return new ProductDto(product.Id, product.Name, product.Description, product.Price, product.Stock, product.CategoryId);
    }
}
