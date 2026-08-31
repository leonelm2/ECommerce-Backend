# ECommerce API & PaymentService - Backend

API REST de un ecommerce hecha con .NET 8, Clean Architecture, CQRS (MediatR) y JWT.
El proyecto consiste en un **monorepositorio con dos carpetas** que contienen dos microservicios independientes comunicados a través de HTTP. Para el trabajo final se eligió implementar la **Opción 1 (PaymentService)**.

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
- **Persistencia en memoria**: Dado que el servicio es simple y su principal objetivo es procesar una regla de negocio sobre un pago, no utiliza base de datos ni EF Core (todo el estado y la validación son transitorios y en memoria).
- **Comunicación sin JWT protegido**: El endpoint `/api/payments/process` no tiene el atributo `[Authorize]` ya que se asume que este microservicio será alcanzable de manera interna sólo por el ECommerce (que actúa de Gateway y sí delega el JWT). Esto simplifica la validación de tokens en la comunicación interna.

## Reglas de Negocio del PaymentService
- Aprueba pagos menores a **$100.000**.
- Rechaza pagos iguales o mayores a **$100.000**.

### Contrato de Comunicación (ECommerce -> PaymentService)

Ejemplo concreto de la comunicación por HTTP cuando se procesa el pago:

**Request**
`POST /api/payments/process`
```json
{
  "orderId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "userId": "user-123",
  "amount": 45000,
  "currency": "ARS"
}
```

**Response - Aprobado (monto < 100.000)**
```json
{
  "paymentId": "481a5fc5-f621-4d32-9c95-4673fb3b7d1e",
  "orderId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "userId": "user-123",
  "status": "Approved",
  "transactionCode": "TXN-638234123456789012",
  "amount": 45000,
  "currency": "ARS",
  "processedAt": "2026-08-31T15:23:46Z",
  "message": "Pago procesado exitosamente"
}
```

**Response - Rechazado (monto >= 100.000)**
```json
{
  "paymentId": "73a4b6c1-a2c3-4d45-9e67-890abcdef123",
  "orderId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "userId": "user-123",
  "status": "Rejected",
  "transactionCode": "TXN-638234123456789013",
  "amount": 150000,
  "currency": "ARS",
  "processedAt": "2026-08-31T15:23:47Z",
  "message": "Pago rechazado. El monto supera el límite permitido."
}
```

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
