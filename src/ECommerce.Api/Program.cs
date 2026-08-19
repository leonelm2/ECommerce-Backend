using ECommerce.Api.Middleware;
using ECommerce.Application.Behaviors;
using ECommerce.Application.Commands.Orders;
using ECommerce.Application.Commands.Products;
using ECommerce.Application.Interfaces;
using ECommerce.Application.Settings;
using ECommerce.Application.Validators;
using ECommerce.Domain.Entities;
using ECommerce.Infrastructure;
using ECommerce.Infrastructure.Persistence;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using Microsoft.Extensions.Configuration;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddUserSecrets<Program>(optional: true);

builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenLocalhost(5117);
    options.ListenLocalhost(5001, listenOptions => listenOptions.UseHttps());
});

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddExceptionHandler<GlobalExceptionHandler>().AddProblemDetails();
builder.Services.AddValidatorsFromAssemblyContaining<CreateProductCommandValidator>();
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
builder.Services.AddMediatR(typeof(CreateProductCommand).Assembly);

builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("JwtSettings"));
builder.Services.Configure<AdminSettings>(builder.Configuration.GetSection("AdminSettings"));

var jwtSettings = builder.Configuration.GetSection("JwtSettings").Get<JwtSettings>()
    ?? throw new InvalidOperationException("JwtSettings no configurado.");
if (string.IsNullOrWhiteSpace(jwtSettings.Secret)
    || jwtSettings.Secret.Contains("ReplaceWithSecure")
    || Encoding.UTF8.GetByteCount(jwtSettings.Secret) < 32)
{
    throw new InvalidOperationException("JwtSettings:Secret debe configurarse mediante variables de entorno o User Secrets y tener al menos 32 bytes de longitud.");
}

var adminSettings = builder.Configuration.GetSection("AdminSettings").Get<AdminSettings>()
    ?? throw new InvalidOperationException("AdminSettings no configurado.");
if (string.IsNullOrWhiteSpace(adminSettings.Username)
    || string.IsNullOrWhiteSpace(adminSettings.Email)
    || string.IsNullOrWhiteSpace(adminSettings.Password)
    || adminSettings.Password.Contains("ReplaceWithSecure"))
{
    throw new InvalidOperationException("AdminSettings debe configurarse mediante variables de entorno o User Secrets.");
}

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings.Issuer,
            ValidAudience = jwtSettings.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Secret)),
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization();

// Agregar controladores
builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Clean Architecture API",
        Version = "v1",
        Description = "API REST profesional con Clean Architecture, Entity Framework Core y JWT"
    });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Ingrese 'Bearer {token}' para autenticar."
    });

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

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    context.Database.Migrate();

    var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
    var adminUser = context.Users.FirstOrDefault(u => u.Role == UserRole.Admin);

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

    adminUser.Username = adminSettings.Username;
    adminUser.Email = adminSettings.Email;
    adminUser.Role = UserRole.Admin;
    adminUser.PasswordHash = passwordHasher.Hash(adminSettings.Password);

    context.SaveChanges();
}

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Clean Architecture API V1");
        c.RoutePrefix = string.Empty; // Swagger en la raíz
    });
}

app.UseHttpsRedirection();
app.UseCors("AllowAll");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();

public partial class Program { }
