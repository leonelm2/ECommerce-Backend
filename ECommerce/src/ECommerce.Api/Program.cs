using ECommerce.Api.Middleware; // Importa nuestro GlobalExceptionHandler
using ECommerce.Application.Behaviors; // Importa ValidationBehavior (el interceptor de validaciones de MediatR)
using ECommerce.Application.Commands.Orders; // Importa comandos de órdenes
using ECommerce.Application.Commands.Products; // Importa CreateProductCommand (para indicarle a MediatR qué ensamblado escanear)
using ECommerce.Application.Interfaces; // Importa interfaces como IPasswordHasher
using ECommerce.Application.Settings; // Importa las clases de configuración JwtSettings y AdminSettings
using ECommerce.Application.Validators; // Importa CreateProductCommandValidator (para registrar todos los validadores de FluentValidation)
using ECommerce.Domain.Entities; // Importa las entidades User y UserRole (para sembrar el usuario Admin al iniciar)
using ECommerce.Infrastructure; // Importa el método de extensión AddInfrastructure()
using ECommerce.Infrastructure.Persistence; // Importa ApplicationDbContext (para aplicar migraciones automáticas al arrancar)
using FluentValidation; // Librería para validaciones de entrada (sintácticas)
using MediatR; // Librería que implementa el patrón Mediator para CQRS
using Microsoft.AspNetCore.Authentication.JwtBearer; // Middleware de autenticación con tokens JWT
using Microsoft.AspNetCore.Server.Kestrel.Core; // Servidor web interno Kestrel de .NET
using Microsoft.EntityFrameworkCore; // Entity Framework Core (para llamar a context.Database.Migrate())
using Microsoft.IdentityModel.Tokens; // Clases para validar la firma criptográfica del JWT (TokenValidationParameters, SymmetricSecurityKey)
using Microsoft.OpenApi.Models; // Clases para configurar la documentación interactiva de Swagger con botón de candado JWT
using System.Text; // Para convertir la clave secreta de texto a bytes (Encoding.UTF8.GetBytes)
using Microsoft.Extensions.Configuration; // Para leer appsettings.json y User Secrets

// ================= 1. CREACIÓN DEL BUILDER (CONFIGURACIÓN DE SERVICIOS / DI) =================
// Inicializa el constructor de la aplicación web cargando configuraciones por defecto (appsettings.json, variables de entorno)
var builder = WebApplication.CreateBuilder(args);

// Agrega soporte opcional para User Secrets (permite guardar claves secretas fuera del repositorio en entorno local)
builder.Configuration.AddUserSecrets<Program>(optional: true);

// Configura el servidor web Kestrel para que escuche en puertos fijos
builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenLocalhost(5117); // Puerto HTTP principal: http://localhost:5117
    options.ListenLocalhost(5001, listenOptions => listenOptions.UseHttps()); // Puerto HTTPS secundario: https://localhost:5001
});

// Llama a nuestro método de extensión en InfrastructureServiceExtensions.cs que registra DbContext (SQLite), Repositorios, UnitOfWork, JWT y el HttpClient con Polly
builder.Services.AddInfrastructure(builder.Configuration);
// Registra nuestro manejador global de excepciones (GlobalExceptionHandler) y el formato estándar RFC 7807 (ProblemDetails)
builder.Services.AddExceptionHandler<GlobalExceptionHandler>().AddProblemDetails();
// Escanea el proyecto Application y registra automáticamente todos los validadores de FluentValidation (ej. CreateOrderCommandValidator)
builder.Services.AddValidatorsFromAssemblyContaining<CreateProductCommandValidator>();
// Registra el Pipeline Behavior en MediatR: intercepta cada Command/Query antes de llegar al Handler para correr FluentValidation
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
// Escanea el proyecto Application y registra automáticamente todos los Commands, Queries y sus respectivos Handlers en MediatR
builder.Services.AddMediatR(typeof(CreateProductCommand).Assembly);

// Aplica el patrón IOptions<T>: vincula las secciones del archivo appsettings.json con clases C# fuertemente tipadas
builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("JwtSettings"));
builder.Services.Configure<AdminSettings>(builder.Configuration.GetSection("AdminSettings"));

// Lee la configuración de JWT inmediatamente al arrancar para validar que esté bien configurada (Fail-Fast)
var jwtSettings = builder.Configuration.GetSection("JwtSettings").Get<JwtSettings>()
    ?? throw new InvalidOperationException("JwtSettings no configurado.");
// Verifica que la clave secreta para firmar tokens exista y tenga al menos 32 bytes (256 bits requeridos por HMAC-SHA256)
if (string.IsNullOrWhiteSpace(jwtSettings.Secret)
    || Encoding.UTF8.GetByteCount(jwtSettings.Secret) < 32)
{
    throw new InvalidOperationException("JwtSettings:Secret debe configurarse mediante variables de entorno o User Secrets y tener al menos 32 bytes de longitud.");
}

// Lee la configuración del usuario Administrador inicial desde appsettings.json
var adminSettings = builder.Configuration.GetSection("AdminSettings").Get<AdminSettings>()
    ?? throw new InvalidOperationException("AdminSettings no configurado.");
// Verifica que no falte usuario, email ni contraseña del admin
if (string.IsNullOrWhiteSpace(adminSettings.Username)
    || string.IsNullOrWhiteSpace(adminSettings.Email)
    || string.IsNullOrWhiteSpace(adminSettings.Password))
{
    throw new InvalidOperationException("AdminSettings debe configurarse.");
}

// ================= 2. CONFIGURACIÓN DE AUTENTICACIÓN JWT Y AUTORIZACIÓN =================
// Registra el esquema de autenticación basado en tokens Bearer JWT
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        // Define qué cosas debe verificar .NET cada vez que un cliente envía un token en el header Authorization
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true, // Verifica quién emitió el token (Issuer)
            ValidateAudience = true, // Verifica para quién fue emitido el token (Audience)
            ValidateLifetime = true, // Verifica que el token no esté vencido (fecha de expiración)
            ValidateIssuerSigningKey = true, // Verifica que la firma criptográfica sea auténtica y no haya sido adulterada
            ValidIssuer = jwtSettings.Issuer, // El Issuer esperado según appsettings.json ("CleanArchitectureApi")
            ValidAudience = jwtSettings.Audience, // La Audience esperada según appsettings.json ("CleanArchitectureApiClient")
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Secret)), // La clave secreta usada para verificar la firma
            ClockSkew = TimeSpan.Zero // Elimina los 5 minutos de tolerancia por defecto de .NET para que el token venza al segundo exacto
        };
    });

// Habilita el sistema de Autorización (el que evalúa los atributos [Authorize] y [Authorize(Roles = "Admin")])
builder.Services.AddAuthorization();

// Registra los Controladores (OrdersController, ProductsController, AuthController) en el contenedor de dependencias
builder.Services.AddControllers();

// ================= 3. CONFIGURACIÓN DE SWAGGER (DOCUMENTACIÓN INTERACTIVA) =================
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    // Define el título y versión que se ven arriba en la página de Swagger
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "E-Commerce API",
        Version = "v1",
        Description = "API REST para el Trabajo Final de E-Commerce"
    });

    // Agrega el botón "Authorize" (el candado) en Swagger para poder pegar el token JWT y probar endpoints protegidos
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization", // Nombre del header HTTP donde viaja el token
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header, // Va en los Headers de la petición HTTP
        Description = "Ingrese 'Bearer {token}' para autenticar."
    });

    // Indica a Swagger que incluya ese header Authorization en las peticiones que hagamos desde el navegador
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// Configura CORS (Cross-Origin Resource Sharing) para permitir que cualquier frontend o cliente consuma la API sin bloqueos de navegador
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin() // Permite cualquier origen (dominio/puerto)
              .AllowAnyMethod() // Permite cualquier verbo HTTP (GET, POST, PUT, DELETE)
              .AllowAnyHeader(); // Permite cualquier header (como Authorization o Content-Type)
    });
});

// Construye la aplicación web con todos los servicios ya registrados
var app = builder.Build();

// ================= 4. MIGRACIÓN AUTOMÁTICA Y SEEDING DEL USUARIO ADMIN =================
// Crea un ámbito (scope) temporal al arrancar la aplicación para preparar la base de datos SQLite
using (var scope = app.Services.CreateScope())
{
    // Pide una instancia de nuestro ApplicationDbContext
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    // Aplica automáticamente las migraciones pendientes (crea el archivo ecommerce.db y sus tablas si no existen)
    context.Database.Migrate();

    // Pide el servicio de hasheo de contraseñas
    var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
    // Busca en la tabla Users si ya existe algún usuario con rol Admin
    var adminUser = context.Users.FirstOrDefault(u => u.Role == UserRole.Admin);

    // Si no existe ningún Admin en la base de datos, instancia la Entidad de Dominio User y la agrega al DbContext
    if (adminUser is null)
    {
        adminUser = new User
        {
            Username = adminSettings.Username,
            Email = adminSettings.Email,
            Role = UserRole.Admin
        };
        context.Users.Add(adminUser);
    }

    // Asegura que los datos del Admin coincidan con appsettings.json y guarda la contraseña HASHEADA (nunca en texto plano)
    adminUser.Username = adminSettings.Username;
    adminUser.Email = adminSettings.Email;
    adminUser.Role = UserRole.Admin;
    adminUser.PasswordHash = passwordHasher.Hash(adminSettings.Password);

    // Guarda el usuario Admin en la base de datos SQLite
    context.SaveChanges();
}

// ================= 5. PIPELINE DE MIDDLEWARES HTTP (EL ORDEN ACÁ ES FUNDAMENTAL) =================
// 1°: Activa el middleware global de excepciones (envuelve todo lo que sigue para atrapar cualquier error y devolver ProblemDetails)
app.UseExceptionHandler();

// 2°: Si estamos en entorno de Desarrollo, habilita la interfaz visual de Swagger en la raíz (http://localhost:5117/)
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Clean Architecture API V1");
        c.RoutePrefix = string.Empty; // Hace que Swagger se abra directo en http://localhost:5117/ sin escribir /swagger
    });
}

// 3°: Activa la política CORS "AllowAll"
app.UseCors("AllowAll");
// 4°: AUTENTICACIÓN ("¿Quién sos?"): Lee y valida el token JWT del header Authorization (siempre debe ir ANTES de UseAuthorization)
app.UseAuthentication();
// 5°: AUTORIZACIÓN ("¿Qué permisos tenés?"): Verifica si el usuario autenticado cumple con el [Authorize] o rol requerido por el Controller
app.UseAuthorization();
// 6°: Enruta la petición al método correspondiente del Controller (OrdersController, ProductsController, AuthController)
app.MapControllers();

// Inicia el servidor y queda escuchando peticiones HTTP
app.Run();

// Clase parcial pública necesaria para que el proyecto de pruebas de integración (ECommerce.Api.Tests) pueda levantar la API en memoria
public partial class Program { }
