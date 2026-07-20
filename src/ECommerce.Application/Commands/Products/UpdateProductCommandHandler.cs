using ECommerce.Application.DTOs;
using ECommerce.Application.Interfaces;
using ECommerce.Domain.Exceptions;
using MediatR;

namespace ECommerce.Application.Commands.Products;

public sealed class UpdateProductCommandHandler : IRequestHandler<UpdateProductCommand, ProductDto>
{
    private readonly IProductRepository _productRepository;

    public UpdateProductCommandHandler(IProductRepository productRepository)
    {
        _productRepository = productRepository;
    }

    public async Task<ProductDto> Handle(UpdateProductCommand request, CancellationToken cancellationToken)
    {
        var existingProduct = await _productRepository.GetByIdAsync(request.Id);
        if (existingProduct is null)
            throw new NotFoundException($"Producto con id {request.Id} no encontrado.");

        existingProduct.Name = request.Name;
        existingProduct.Description = request.Description;
        existingProduct.UpdatePrice(request.Price);  // Usa el método de dominio con validación
        existingProduct.Stock = request.Stock;
        existingProduct.CategoryId = request.CategoryId;

        await _productRepository.UpdateAsync(existingProduct);

        // Mapear la entidad a DTO antes de retornar — nunca exponer la entidad de dominio
        return new ProductDto(
            existingProduct.Id,
            existingProduct.Name,
            existingProduct.Description,
            existingProduct.Price,
            existingProduct.Stock,
            existingProduct.CategoryId);
    }
}
