using ECommerce.Application.DTOs; // Importa ProductDto (el DTO de respuesta)
using ECommerce.Application.Interfaces; // Importa la interfaz IProductRepository
using ECommerce.Domain.Entities; // Importa la Entidad de Dominio Product
using MediatR; // Importa IRequestHandler para procesar el Command

namespace ECommerce.Application.Commands.Products; // Capa Application: caso de uso "Crear Producto"

public sealed class CreateProductCommandHandler : IRequestHandler<CreateProductCommand, ProductDto>
{
    // Interfaz del repositorio de productos (Inversión de Dependencias: no conoce a EF Core)
    private readonly IProductRepository _productRepository;

    // Constructor: recibe IProductRepository por Inyección de Dependencias
    public CreateProductCommandHandler(IProductRepository productRepository)
    {
        _productRepository = productRepository;
    }

    // PASO 3: EL HANDLER (LA COCINA)
    // MediatR busca automáticamente esta clase porque implementa IRequestHandler<CreateProductCommand, ProductDto>.
    // Aquí es donde vive la lógica real: se recibe el paquete (request), se mapea a una entidad de base de datos (Product), 
    // se guarda en el repositorio y se devuelve el DTO de respuesta.
    public async Task<ProductDto> Handle(CreateProductCommand request, CancellationToken cancellationToken)
    {
        // 1. ¡PUNTO CLAVE DE EXAMEN! Instanciamos la ENTIDAD DE DOMINIO "Product" copiando los datos que vinieron en el Command (request)
        var product = new Product
        {
            Name = request.Name, // Nombre del producto
            Description = request.Description, // Descripción
            Price = request.Price, // Precio unitario
            Stock = request.Stock, // Stock inicial disponible
            CategoryId = request.CategoryId // ID de la categoría
        };

        // 2. Le pasamos la Entidad de Dominio al repositorio para que EF Core haga el INSERT en la tabla Products de SQLite.
        // Al terminar esta línea, "product.Id" ya tiene cargado el número de ID autogenerado por la base de datos.
        await _productRepository.AddAsync(product);

        // 3. ¡PUNTO CLAVE DE EXAMEN! Empaquetamos los datos de la Entidad de Dominio ya guardada dentro de un DTO DE SALIDA (ProductDto)
        // y lo retornamos a MediatR (que se lo devuelve al ProductsController para responder 201 Created).
        return new ProductDto(product.Id, product.Name, product.Description, product.Price, product.Stock, product.CategoryId);
    }
}
