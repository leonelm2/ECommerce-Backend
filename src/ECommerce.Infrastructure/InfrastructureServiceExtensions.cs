using ECommerce.Application.Interfaces;
using ECommerce.Application.Security;
using ECommerce.Application.Settings;
using ECommerce.Infrastructure.Persistence;
using ECommerce.Infrastructure.Repositories;
using ECommerce.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Net.Http.Headers;

namespace ECommerce.Infrastructure
{
    public static class InfrastructureServiceExtensions
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlite(configuration.GetConnectionString("DefaultConnection"),
                    b => b.MigrationsAssembly("ECommerce.Infrastructure")));

            // Repositorios
            services.AddScoped<IProductRepository, ProductRepository>();
            services.AddScoped<IOrderRepository, OrderRepository>();
            services.AddScoped<IUserRepository, UserRepository>();
            services.AddScoped<IUnitOfWork, UnitOfWork>();

            // Servicios de seguridad
            services.AddScoped<IJwtTokenService, JwtTokenService>();
            services.AddScoped<IPasswordHasher, PasswordHasherService>();

            // Integración con PaymentService
            services.AddHttpContextAccessor();
            services.AddHttpClient<IPaymentServiceClient, PaymentServiceClient>(client =>
            {
                var baseUrl = configuration["PaymentSettings:BaseUrl"] 
                    ?? throw new InvalidOperationException("La URL de PaymentSettings:BaseUrl no está configurada.");
                
                client.BaseAddress = new Uri(baseUrl);
                client.Timeout = TimeSpan.FromSeconds(10); // Timeout preventivo de 10 segundos
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            });

            return services;
        }
    }
}