using ECommerce.Application.Interfaces;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Exceptions;
using MediatR;

namespace ECommerce.Application.Commands.Products;

public sealed class UpdateProductCommandHandler : IRequestHandler<UpdateProductCommand, Product>
{
    private readonly IProductRepository _productRepository;

    public UpdateProductCommandHandler(IProductRepository productRepository)
    {
        _productRepository = productRepository;
    }

    public async Task<Product> Handle(UpdateProductCommand request, CancellationToken cancellationToken)
    {
        var existingProduct = await _productRepository.GetByIdAsync(request.Id);
        if (existingProduct is null)
        {
            throw new NotFoundException($"Producto con id {request.Id} no encontrado.");
        }

        existingProduct.Name = request.Name;
        existingProduct.Description = request.Description;
        existingProduct.UpdatePrice(request.Price);
        existingProduct.Stock = request.Stock;
        existingProduct.CategoryId = request.CategoryId;

        await _productRepository.UpdateAsync(existingProduct);
        return existingProduct;
    }
}
