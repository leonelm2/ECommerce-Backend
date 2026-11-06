using ECommerce.Application.Interfaces; // Importa la interfaz IUnitOfWork
using ECommerce.Infrastructure.Persistence; // Importa ApplicationDbContext

namespace ECommerce.Infrastructure.Repositories; // Capa Infrastructure

// Implementa el patrón Unit of Work (Unidad de Trabajo).
// Comparte la misma instancia "Scoped" de ApplicationDbContext que los repositorios durante una petición HTTP,
// permitiendo confirmar todos los cambios pendientes (de productos, órdenes e ítems) en una sola transacción de base de datos.
public class UnitOfWork : IUnitOfWork
{
    private readonly ApplicationDbContext _context; // El contexto de Entity Framework Core

    // Constructor: recibe el mismo ApplicationDbContext que se inyectó en los repositorios de esta request
    public UnitOfWork(ApplicationDbContext context)
    {
        _context = context;
    }

    // Ejecuta SaveChangesAsync() en el DbContext: abre una transacción en SQLite, ejecuta todos los INSERT/UPDATE/DELETE acumulados, y hace COMMIT (o ROLLBACK si algo falla)
    public Task SaveChangesAsync()
    {
        return _context.SaveChangesAsync();
    }
}
