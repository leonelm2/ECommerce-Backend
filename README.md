# ECommerce API - Backend

API REST de un ecommerce hecha con .NET 8, Clean Architecture y JWT.

Usa SQLite como base de datos, Entity Framework Core para la persistencia, MediatR para separar los casos de uso (CQRS), y FluentValidation para validar los requests.

## Estructura del proyecto

- **ECommerce.Domain**: entidades, enums y excepciones
- **ECommerce.Application**: commands, queries, DTOs, validators e interfaces
- **ECommerce.Infrastructure**: repositorios, DbContext, servicios (JWT, hashing, cliente de pagos)
- **ECommerce.Api**: controllers, middleware de excepciones, configuración

## Cómo correr

Necesitás tener .NET 8 SDK instalado.

```bash
dotnet restore
dotnet build
dotnet run --project src/ECommerce.Api
```

La base de datos se crea sola al iniciar (SQLite). Si querés aplicar migraciones manualmente:

```bash
dotnet ef database update -p src/ECommerce.Infrastructure -s src/ECommerce.Api
```

## Tests

```bash
dotnet test
```

## Swagger

Una vez corriendo, entrá a:
- http://localhost:5117
- https://localhost:5001

## Usuario admin

Se crea automaticamente al iniciar:
- **Usuario**: `admin`
- **Contraseña**: `Admin123!`
- **Rol**: `Admin`

## Endpoints principales

- `POST /api/auth/register` - registrar usuario
- `POST /api/auth/login` - login (devuelve token JWT)
- `GET /api/products` - listar productos (público)
- `GET /api/products/{id}` - obtener producto por id
- `POST /api/products` - crear producto (solo admin)
- `PUT /api/products/{id}` - editar producto (solo admin)
- `DELETE /api/products/{id}` - borrar producto (solo admin)
- `POST /api/orders` - crear orden
- `GET /api/orders/{id}` - ver orden por id

Para los endpoints protegidos, en Swagger hay que poner el token con formato `Bearer <token>` en el botón Authorize.
