using ECommerce.Application.Interfaces;
using ECommerce.Domain.Exceptions;
using MediatR;

namespace ECommerce.Application.Commands.Products;

public sealed class DeleteProductCommandHandler : IRequestHandler<DeleteProductCommand>
{
    private readonly IProductRepository _productRepository;

    public DeleteProductCommandHandler(IProductRepository productRepository)
    {
        _productRepository = productRepository;
    }

    public async Task<Unit> Handle(DeleteProductCommand request, CancellationToken cancellationToken)
    {
        var existingProduct = await _productRepository.GetByIdAsync(request.Id);
        if (existingProduct is null)
        {
            throw new NotFoundException($"Producto con id {request.Id} no encontrado.");
        }

        if (await _productRepository.HasOrderItemsAsync(request.Id))
        {
            throw new DomainRuleException($"No se puede eliminar el producto con id {request.Id} porque tiene órdenes asociadas.");
        }

        await _productRepository.DeleteAsync(request.Id);
        return Unit.Value;
    }
}
