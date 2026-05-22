using ECommerce.Application.Interfaces;
using ECommerce.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ECommerce.Infrastructure.Repositories
{
    public class Repository<T> : IRepository<T> where T : class
    {
        protected readonly ApplicationDbContext _context;

        public Repository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<T>> GetAllAsync()
        {
            return await _context.Set<T>().AsNoTracking().ToListAsync();
        }

        public virtual async Task<T?> GetByIdAsync(int id)
        {
            return await _context.Set<T>().FindAsync(id);
        }

        public async Task AddAsync(T entity, bool saveChanges = true)
        {
            await _context.Set<T>().AddAsync(entity);
            if (saveChanges)
            {
                await _context.SaveChangesAsync();
            }
        }

        public async Task UpdateAsync(T entity, bool saveChanges = true)
        {
            _context.Entry(entity).State = EntityState.Modified;
            if (saveChanges)
            {
                await _context.SaveChangesAsync();
            }
        }

        public async Task DeleteAsync(int id, bool saveChanges = true)
        {
            var entity = await _context.Set<T>().FindAsync(id);
            if (entity != null)
            {
                _context.Set<T>().Remove(entity);
                if (saveChanges)
                {
                    await _context.SaveChangesAsync();
                }
            }
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}
