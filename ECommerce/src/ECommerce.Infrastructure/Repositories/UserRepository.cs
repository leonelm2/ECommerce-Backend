using ECommerce.Application.Interfaces; // Importa IUserRepository
using ECommerce.Domain.Entities; // Importa la Entidad de Dominio User
using ECommerce.Infrastructure.Persistence; // Importa ApplicationDbContext
using Microsoft.EntityFrameworkCore; // Importa AsNoTracking y FirstOrDefaultAsync

namespace ECommerce.Infrastructure.Repositories // Capa Infrastructure
{
    // Hereda el CRUD básico de Repository<User> e implementa las búsquedas específicas de IUserRepository
    public class UserRepository : Repository<User>, IUserRepository
    {
        public UserRepository(ApplicationDbContext context) : base(context)
        {
        }

        // Busca un usuario por su nombre de usuario (usado en Login y en Registro para evitar duplicados)
        public async Task<User?> GetByUsernameAsync(string username)
        {
            // Se traduce a SQL: SELECT * FROM Users WHERE Username = @username LIMIT 1
            return await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Username == username);
        }

        // Busca un usuario por su correo electrónico (usado en Registro para validar que el email no esté repetido)
        public async Task<User?> GetByEmailAsync(string email)
        {
            // Se traduce a SQL: SELECT * FROM Users WHERE Email = @email LIMIT 1
            return await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Email == email);
        }
    }
}
