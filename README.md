# ECommerce API & PaymentService - Backend

API REST de un ecommerce hecha con .NET 8, Clean Architecture, CQRS (MediatR) y JWT.
El proyecto incluye **dos microservicios independientes** comunicados a través de HTTP.

1. **ECommerce.Api**: El sistema principal de gestión de órdenes y productos.
2. **PaymentService.Api**: Servicio dedicado de procesamiento de pagos.

Ambos proyectos aplican principios de Clean Architecture.

## Estructura del proyecto

### ECommerce
- **ECommerce.Domain**: entidades, enums y excepciones
- **ECommerce.Application**: commands, queries, DTOs, validators e interfaces
- **ECommerce.Infrastructure**: repositorios, DbContext, servicios (JWT, hashing, cliente HTTP de pagos)
- **ECommerce.Api**: controllers, middleware de excepciones, configuración

### PaymentService
- Estructurado internamente con carpetas `Core/Domain`, `Core/Application`, e `Infrastructure` para aislar reglas de negocio, y controladores ligeros.

## Reglas de Negocio del PaymentService
- Aprueba pagos menores a **$100.000**.
- Rechaza pagos iguales o mayores a **$100.000**.

## Cómo correr

Para probar el flujo completo (crear una orden y que se pague), **DEBEN LEVANTARSE AMBOS SERVICIOS**.

### Opción 1: Visual Studio
1. Haz clic derecho sobre la solución `CleanArchitectureApi`.
2. Selecciona **"Establecer proyectos de inicio..."** (Set Startup Projects...).
3. Selecciona **"Proyectos de inicio múltiples"** (Multiple startup projects).
4. Pon la acción **"Iniciar"** (Start) para `ECommerce.Api` y `PaymentService.Api`.
5. Presiona F5.

### Opción 2: CLI (Consola)
Abre dos terminales diferentes.

En la terminal 1 (Levantar PaymentService en puerto 5200):
```bash
dotnet run --project PaymentService/src/PaymentService.Api
```

En la terminal 2 (Levantar ECommerce en puerto 5117/5001):
```bash
dotnet run --project ECommerce/src/ECommerce.Api
```

### Base de Datos
La base de datos se crea sola al iniciar ECommerce (SQLite). Si querés aplicar migraciones manualmente:

```bash
dotnet ef database update -p ECommerce/src/ECommerce.Infrastructure -s ECommerce/src/ECommerce.Api
```

## Swagger y Puertos

Una vez corriendo, entrá a:
- ECommerce API: http://localhost:5117
- PaymentService API: http://localhost:5200 (Solo tiene el endpoint de pagos, es consumido por ECommerce).

## Usuario admin de prueba

Se crea automaticamente al iniciar (configurado en appsettings.json):
- **Usuario**: `admin`
- **Contraseña**: `AdminPassword123!`
- **Email**: `admin@ecommerce.com`
- **Rol**: `Admin`

## Endpoints principales y Flujo

1. `POST /api/auth/register` - registrar usuario
2. `POST /api/auth/login` - login (devuelve token JWT)
3. `POST /api/orders` - crear orden
    - Cuando se crea, el ECommerce contacta automáticamente al PaymentService vía HTTP usando IHttpClientFactory.
    - Si PaymentService **aprueba** el pago (<$100.000), la orden queda en estado `Paid`.
    - Si PaymentService **rechaza** el pago (>=$100.000), la orden queda en estado `PaymentRejected`.
    - Si PaymentService está **caído**, se controla la excepción (Timeout o HttpRequestException), no se cae la app, y la orden queda en estado `PaymentRejected` con mensaje de error.

Para los endpoints protegidos, en Swagger hay que poner el token con formato `Bearer <token>` en el botón Authorize.
