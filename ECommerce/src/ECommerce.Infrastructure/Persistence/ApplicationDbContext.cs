using ECommerce.Domain.Entities; // Importa las Entidades de Dominio (Product, Order, OrderItem, User)
using Microsoft.EntityFrameworkCore; // Importa DbContext, DbSet, ModelBuilder
using System.Reflection; // Importa Assembly.GetExecutingAssembly() para escanear las configuraciones de tablas

namespace ECommerce.Infrastructure.Persistence // Capa Infrastructure: configuración de persistencia con EF Core
{
    // ApplicationDbContext hereda de DbContext (de Entity Framework Core) y representa el puente entre nuestras clases C# y las tablas de la base de datos SQLite
    public class ApplicationDbContext : DbContext
    {
        // Constructor: recibe las opciones de conexión (como la cadena "Data Source=ecommerce.db") configuradas en InfrastructureServiceExtensions.cs
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        // Cada DbSet<T> representa una TABLA dentro de la base de datos SQLite:
        public DbSet<Product> Products { get; set; } // Tabla "Products"
        public DbSet<Order> Orders { get; set; } // Tabla "Orders"
        public DbSet<OrderItem> OrderItems { get; set; } // Tabla "OrderItems" (detalle de cada orden)
        public DbSet<User> Users { get; set; } // Tabla "Users"

        // Método que EF Core ejecuta cuando está armando el modelo de tablas en memoria
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Busca y aplica automáticamente todas las clases de la carpeta Persistence/Configurations (OrderConfiguration, ProductConfiguration, etc.)
            // donde están definidas las claves primarias, claves foráneas, longitudes máximas de texto y tipos decimales (Fluent API).
            modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());

            base.OnModelCreating(modelBuilder);
        }
    }
}
