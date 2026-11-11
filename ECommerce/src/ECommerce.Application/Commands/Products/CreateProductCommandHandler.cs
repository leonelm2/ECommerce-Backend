using ECommerce.Application.DTOs;
using ECommerce.Application.Interfaces;
using ECommerce.Domain.Entities;
using MediatR;

namespace ECommerce.Application.Commands.Products;

// FLUJO DE REQUEST (PRODUCTS - PASO 3): MediatR toma el Command, busca quién es el encargado de procesarlo (este Handler) y se lo entrega.
public sealed class CreateProductCommandHandler : IRequestHandler<CreateProductCommand, ProductDto>
{
    private readonly IProductRepository _productRepository;

    public CreateProductCommandHandler(IProductRepository productRepository)
    {
        _productRepository = productRepository;
    }

    public async Task<ProductDto> Handle(CreateProductCommand request, CancellationToken cancellationToken)
    {
        // 1. El Handler recibe los datos del Command y crea nuestra ENTIDAD DE DOMINIO (Product).
        var product = new Product
        {
            Name = request.Name,
            Description = request.Description,
            Price = request.Price,
            Stock = request.Stock,
            CategoryId = request.CategoryId
        };

        // 2. Se la pasa al repositorio para que la guarde en la base de datos.
        await _productRepository.AddAsync(product);

        // 3. Una vez guardada en la base de datos, el Handler empaqueta el resultado en un DTO (ProductDto) y MediatR se lo devuelve al controlador.
        return new ProductDto(product.Id, product.Name, product.Description, product.Price, product.Stock, product.CategoryId);
    }
}
