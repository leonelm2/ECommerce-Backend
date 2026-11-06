using ECommerce.Application.DTOs; // Importa ProductDto
using ECommerce.Application.Interfaces; // Importa IProductRepository
using ECommerce.Domain.Exceptions; // Importa NotFoundException
using MediatR; // Importa IRequestHandler

namespace ECommerce.Application.Commands.Products; // Capa Application: caso de uso "Actualizar Producto"

public sealed class UpdateProductCommandHandler : IRequestHandler<UpdateProductCommand, ProductDto>
{
    private readonly IProductRepository _productRepository;

    // Constructor: inyecta el repositorio de productos
    public UpdateProductCommandHandler(IProductRepository productRepository)
    {
        _productRepository = productRepository;
    }

    public async Task<ProductDto> Handle(UpdateProductCommand request, CancellationToken cancellationToken)
    {
        // 1. Busca la Entidad de Dominio Product en la base de datos por su ID
        var existingProduct = await _productRepository.GetByIdAsync(request.Id);
        // Si no existe en la BD, lanza NotFoundException (que GlobalExceptionHandler convierte en HTTP 404 Not Found)
        if (existingProduct is null)
            throw new NotFoundException($"Producto con id {request.Id} no encontrado.");

        // 2. Actualiza las propiedades de la Entidad de Dominio con los datos nuevos del Command
        existingProduct.Name = request.Name;
        existingProduct.Description = request.Description;
        // ¡DOMINIO RICO! Usa el método de negocio UpdatePrice() que valida que el precio no sea negativo
        existingProduct.UpdatePrice(request.Price);
        existingProduct.Stock = request.Stock;
        existingProduct.CategoryId = request.CategoryId;

        // 3. Guarda los cambios en la base de datos mediante el repositorio
        await _productRepository.UpdateAsync(existingProduct);

        // 4. Empaqueta la entidad actualizada en un ProductDto de respuesta y lo devuelve
        return new ProductDto(
            existingProduct.Id,
            existingProduct.Name,
            existingProduct.Description,
            existingProduct.Price,
            existingProduct.Stock,
            existingProduct.CategoryId);
    }
}
