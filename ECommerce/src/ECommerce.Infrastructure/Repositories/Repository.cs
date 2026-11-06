using ECommerce.Application.Interfaces; // Importa la interfaz genérica IRepository<T>
using ECommerce.Infrastructure.Persistence; // Importa ApplicationDbContext
using Microsoft.EntityFrameworkCore; // Importa DbContext, EntityState, AsNoTracking, ToListAsync
using System.Collections.Generic; // Importa IEnumerable<T>
using System.Threading.Tasks; // Importa Task

namespace ECommerce.Infrastructure.Repositories // Capa Infrastructure: acceso real a la base de datos con EF Core
{
    // Repositorio Genérico que implementa las operaciones CRUD básicas (GetAll, GetById, Add, Update, Delete)
    // para cualquier Entidad de Dominio "T". Así evitamos repetir este mismo código en ProductRepository, OrderRepository y UserRepository.
    public class Repository<T> : IRepository<T> where T : class
    {
        // Instancia protegida del DbContext de Entity Framework Core (protected para que las clases hijas puedan usarla)
        protected readonly ApplicationDbContext _context;

        // Constructor: recibe el ApplicationDbContext por Inyección de Dependencias
        public Repository(ApplicationDbContext context)
        {
            _context = context;
        }

        // Obtiene todos los registros de la tabla correspondiente a la entidad T
        public async Task<IEnumerable<T>> GetAllAsync()
        {
            // Set<T>() selecciona la tabla; AsNoTracking() optimiza la lectura diciéndole a EF Core que no guarde seguimiento en memoria; ToListAsync() ejecuta el SELECT en la BD
            return await _context.Set<T>().AsNoTracking().ToListAsync();
        }

        // Busca un registro por su clave primaria (ID). Es "virtual" para que OrderRepository pueda sobrescribirlo (override) y agregar .Include()
        public virtual async Task<T?> GetByIdAsync(int id)
        {
            // FindAsync busca primero en caché de EF Core y si no está hace el SELECT WHERE Id = @id
            return await _context.Set<T>().FindAsync(id);
        }

        // Agrega una nueva Entidad de Dominio a la base de datos.
        // ¡DETALLE CLAVE DE EXAMEN! El parámetro "saveChanges = true" permite elegir si guardamos inmediatamente
        // o si pasamos "saveChanges: false" (como en CreateOrderCommandHandler) para guardar todo junto al final con UnitOfWork.
        public async Task AddAsync(T entity, bool saveChanges = true)
        {
            // Agrega la entidad en memoria al Change Tracker de EF Core en estado "Added"
            await _context.Set<T>().AddAsync(entity);
            // Solo si saveChanges es true, ejecuta el INSERT inmediatamente en la base de datos
            if (saveChanges)
            {
                await _context.SaveChangesAsync();
            }
        }

        // Marca una Entidad de Dominio existente como modificada
        public async Task UpdateAsync(T entity, bool saveChanges = true)
        {
            // Le avisa al Change Tracker de EF Core que esta entidad fue modificada (estado "Modified") para que arme el UPDATE
            _context.Entry(entity).State = EntityState.Modified;
            // Si saveChanges es true, ejecuta el UPDATE inmediatamente; si es false, espera al UnitOfWork.SaveChangesAsync()
            if (saveChanges)
            {
                await _context.SaveChangesAsync();
            }
        }

        // Elimina un registro por su ID
        public async Task DeleteAsync(int id, bool saveChanges = true)
        {
            // Primero busca la entidad por ID en la base de datos
            var entity = await _context.Set<T>().FindAsync(id);
            // Si existe, la marca en estado "Deleted" y (si saveChanges es true) ejecuta el DELETE en la BD
            if (entity != null)
            {
                _context.Set<T>().Remove(entity);
                if (saveChanges)
                {
                    await _context.SaveChangesAsync();
                }
            }
        }

        // Método auxiliar para confirmar cambios pendientes en la base de datos
        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}
