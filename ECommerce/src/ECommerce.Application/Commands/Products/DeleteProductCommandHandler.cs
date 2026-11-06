using ECommerce.Application.Interfaces; // Importa IProductRepository
using ECommerce.Domain.Exceptions; // Importa NotFoundException y DomainRuleException
using MediatR; // Importa IRequestHandler y Unit (el "void" de MediatR)

namespace ECommerce.Application.Commands.Products; // Capa Application: caso de uso "Eliminar Producto"

public sealed class DeleteProductCommandHandler : IRequestHandler<DeleteProductCommand>
{
    private readonly IProductRepository _productRepository;

    public DeleteProductCommandHandler(IProductRepository productRepository)
    {
        _productRepository = productRepository;
    }

    // Devuelve Task<Unit> porque eliminar un producto no devuelve ningún DTO (el Controller responde 204 No Content)
    public async Task<Unit> Handle(DeleteProductCommand request, CancellationToken cancellationToken)
    {
        // 1. Verifica que el producto exista en la base de datos; si no, lanza 404 Not Found
        var existingProduct = await _productRepository.GetByIdAsync(request.Id);
        if (existingProduct is null)
        {
            throw new NotFoundException($"Producto con id {request.Id} no encontrado.");
        }

        // 2. REGLA DE NEGOCIO DE INTEGRIDAD: Verifica si el producto ya fue comprado en alguna orden histórica.
        // Si tiene órdenes asociadas, impide borrarlo lanzando DomainRuleException (422 Unprocessable Entity).
        if (await _productRepository.HasOrderItemsAsync(request.Id))
        {
            throw new DomainRuleException($"No se puede eliminar el producto con id {request.Id} porque tiene órdenes asociadas.");
        }

        // 3. Si no tiene órdenes asociadas, lo elimina físicamente de la base de datos
        await _productRepository.DeleteAsync(request.Id);
        // Retorna Unit.Value (indica a MediatR que terminó con éxito sin valor de retorno)
        return Unit.Value;
    }
}
