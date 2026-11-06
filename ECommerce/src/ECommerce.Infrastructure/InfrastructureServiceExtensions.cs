using ECommerce.Application.Interfaces; // Importa las interfaces de la capa Application (IProductRepository, IOrderRepository, etc.)
using ECommerce.Application.Settings; // Importa configuraciones
using ECommerce.Infrastructure.Persistence; // Importa ApplicationDbContext (nuestro contexto de Entity Framework Core)
using ECommerce.Infrastructure.Repositories; // Importa las implementaciones concretas de los repositorios y UnitOfWork
using ECommerce.Infrastructure.Services; // Importa JwtTokenService, PasswordHasherService y PaymentServiceClient
using Microsoft.EntityFrameworkCore; // Importa UseSqlite para conectar EF Core con SQLite
using Microsoft.Extensions.Configuration; // Importa IConfiguration para leer appsettings.json
using Microsoft.Extensions.DependencyInjection; // Importa IServiceCollection para registrar las dependencias (IoC Container)
using System; // Importa Uri, TimeSpan, Math, InvalidOperationException
using System.Net.Http.Headers; // Importa MediaTypeWithQualityHeaderValue
using Polly; // Librería de resiliencia para aplicar reintentos automáticos (WaitAndRetryAsync)
using Polly.Extensions.Http; // Extensiones de Polly para detectar errores HTTP transitorios (5xx o caída de red)

namespace ECommerce.Infrastructure
{
    // Clase estática que agrupa el registro de todos los servicios de la capa de Infraestructura.
    // Se llama desde Program.cs en la línea: builder.Services.AddInfrastructure(builder.Configuration);
    public static class InfrastructureServiceExtensions
    {
        // Método de extensión sobre IServiceCollection
        public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            // 1. BASE DE DATOS (EF Core + SQLite):
            // Registra ApplicationDbContext indicando que use SQLite con la cadena "DefaultConnection" de appsettings.json ("Data Source=ecommerce.db")
            // y que las migraciones viven en el ensamblado "ECommerce.Infrastructure".
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlite(configuration.GetConnectionString("DefaultConnection"),
                    b => b.MigrationsAssembly("ECommerce.Infrastructure")));

            // 2. INVERSIÓN DE DEPENDENCIAS (REPOSITORIOS Y UNIT OF WORK):
            // Se registran como "Scoped" (se crea una instancia nueva por cada petición HTTP y se destruye al terminar la petición).
            // Acá le decimos a .NET: "Cuando un Handler pida la interfaz IProductRepository, inyectale la clase concreta ProductRepository".
            services.AddScoped<IProductRepository, ProductRepository>();
            services.AddScoped<IOrderRepository, OrderRepository>();
            services.AddScoped<IUserRepository, UserRepository>();
            services.AddScoped<IUnitOfWork, UnitOfWork>();

            // 3. SERVICIOS DE SEGURIDAD:
            // Cuando alguien pida IJwtTokenService, inyecta JwtTokenService.
            services.AddScoped<IJwtTokenService, JwtTokenService>();
            // Cuando alguien pida IPasswordHasher, inyecta PasswordHasherService.
            services.AddScoped<IPasswordHasher, PasswordHasherService>();

            // 4. COMUNICACIÓN CON MICROSERVICIO DE PAGOS (Typed HttpClient + Polly):
            // Registra IHttpContextAccessor para poder leer el header Authorization de la request actual dentro de PaymentServiceClient
            services.AddHttpContextAccessor();
            // Registra PaymentServiceClient usando IHttpClientFactory (evita hacer "new HttpClient()" que agota los sockets del sistema)
            services.AddHttpClient<IPaymentServiceClient, PaymentServiceClient>(client =>
            {
                // Lee la URL del microservicio de pagos desde appsettings.json ("http://localhost:5200")
                var baseUrl = configuration["PaymentSettings:BaseUrl"] 
                    ?? throw new InvalidOperationException("La URL de PaymentSettings:BaseUrl no está configurada.");
                
                // Configura la URL base del cliente HTTP
                client.BaseAddress = new Uri(baseUrl);
                // Configura un Timeout máximo de 10 segundos por intento (si tarda más, lanza TaskCanceledException)
                client.Timeout = TimeSpan.FromSeconds(10);
                // Indica que esperamos respuestas en formato JSON
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            })
            // 5. POLÍTICA DE RESILIENCIA (POLLY):
            // Ante fallos transitorios de red (HttpRequestException) o códigos HTTP 5xx (servidor caído) o 408 (Request Timeout),
            // reintenta automáticamente hasta 3 veces con espera exponencial (Exponential Backoff): 2^1 = 2 seg, 2^2 = 4 seg, 2^3 = 8 seg.
            .AddTransientHttpErrorPolicy(policyBuilder =>
                policyBuilder.WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt))));

            // Devuelve la colección de servicios configurada
            return services;
        }
    }
}
