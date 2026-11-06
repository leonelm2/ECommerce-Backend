# 🎯 Plan Maestro de 10 Días: Defensa Final `ECommerce-Backend` & `PaymentService`

> **Objetivo del Plan:** Que en 10 días no solo sepas *qué* decir de memoria, sino que tengas **memoria muscular en el editor**: cuando el profesor haga una pregunta o señale una línea, abras el archivo exacto en menos de 3 segundos y respondas usando el **vocabulario técnico exacto** (Command, Entidad de Dominio, DTO, Pipeline Behavior, Unit of Work, Typed HttpClient).

---

## 🧠 Parte 1: Modos de Estudio y Reglas de Juego

Para no estudiar "leyendo pasivamente" (que hace que en el examen te tildes si te cambian una palabra), cada día de los 10 vas a aplicar estos **4 Modos de Estudio** en bloques de 45-60 minutos:

1. **Modo 1 — "Traducción a Idioma Profesor" (10 min):**
   Nunca digas *"acá creo la orden"* o *"acá mando los datos"*. Siempre nombrá **Qué artefacto técnico es + En qué capa vive**. (Ver tabla de traducción más abajo).
2. **Modo 2 — "Navegación a Ciegas con Atajos" (15 min):**
   Cerrá todos los archivos de VS Code. Leé una pregunta en voz alta y tratá de llegar a la línea exacta usando solo atajos de teclado en menos de 5 segundos.
3. **Modo 3 — Técnica Feynman en Voz Alta (15 min):**
   Con el archivo abierto en pantalla, explicá en voz alta qué hace ese bloque señalando con el cursor como si el profesor estuviera sentado al lado tuyo. Si te trabás en una palabra, no sigas: repetí esa frase hasta que salga fluida.
4. **Modo 4 — "Pregunta Trampa / ¿Y qué pasa si...?" (10 min):**
   Al final de cada sesión, respondé la pregunta de "¿Qué pasa si borro o cambio esta línea?". Eso es lo que separa un 6 de un 10.

---

## ⌨️ Parte 2: Atajos de Teclado Salvavidas para el Examen en Vivo

Cuando estás nervioso en un final, buscar archivos con el mouse en el árbol de carpetas te hace perder tiempo y te pone más nervioso. Dominá estos **5 atajos en VS Code**:

| Atajo (Windows) | Qué hace | Cuándo usarlo frente al profesor |
| :--- | :--- | :--- |
| **`Ctrl + P`** | Buscar archivo por nombre | Si el profe dice *"Mostrame dónde generan el token"*, tocás `Ctrl + P`, escribís `jwt` y apretás `Enter` → abre `JwtTokenService.cs` al instante. |
| **`F12`** | Ir a la definición | Estás en `OrdersController.cs` sobre `CreateOrderCommand`, tocás `F12` y saltás directo al Command. |
| **`Ctrl + F12`** | Ir a la implementación real | ¡CLAVE! Si estás en el Handler sobre `_paymentServiceClient.ProcessPaymentAsync` (que es una interfaz en Application) y tocás `F12`, te lleva a la interfaz. Si tocás **`Ctrl + F12`**, te lleva directo a la clase real `PaymentServiceClient.cs` en Infraestructura. |
| **`Alt + Flecha Izquierda`** | Volver atrás donde estabas | Después de saltar con `F12` a mostrar una entidad o interfaz, tocás `Alt + ←` y volvés exactamente a la línea del Handler donde estabas explicando. |
| **`Ctrl + G`** | Ir a una línea exacta | Si sabés que el bloque `try/catch` del pago está en la línea 72, tocás `Ctrl + G`, escribís `72` y te lleva ahí. |

**💡 Truco antes de empezar a hablar en el examen:** Dejá abiertas en pestañas fijas estos 5 archivos en este orden (de izquierda a derecha), porque siguen el orden exacto de tu machete:
1. `OrdersController.cs` (o `ProductsController.cs`)
2. `CreateOrderCommand.cs`
3. `ValidationBehavior.cs`
4. `CreateOrderCommandHandler.cs`
5. `GlobalExceptionHandler.cs`

---

## 🗣️ Parte 3: Diccionario "No te Tildes" (Qué decir cuando señala algo)

Esta tabla resuelve exactamente lo que te pasó en el examen anterior:

| Si el profe señala... | ❌ NO digas... | ✅ DECÍ SIEMPRE ESTO: |
| :--- | :--- | :--- |
| `[FromBody] CreateOrderCommand request` en `OrdersController.cs` (L36) | *"Acá entran los datos de la orden"* | *"Acá el Model Binder de ASP.NET deserializa el JSON y crea el **Command** (`CreateOrderCommand`), que es el objeto de entrada del patrón CQRS."* |
| `var order = new Order { ... }` en `CreateOrderCommandHandler.cs` (L44) | *"Acá creo una orden"* | *"Acá instanciamos la **Entidad de Dominio** `Order` (de la capa `Domain`), porque el repositorio y la base de datos solo trabajan con entidades de dominio."* |
| `var product = new Product { ... }` en `CreateProductCommandHandler.cs` (L23) | *"Acá creo el producto"* | *"Acá mapeamos los datos del Command a una nueva instancia de la **Entidad de Dominio** `Product`."* |
| `return new OrderDto(...)` en `CreateOrderCommandHandler.cs` (L98) | *"Acá devuelvo la orden"* | *"Acá mapeamos la Entidad de Dominio ya persistida a un **DTO de respuesta** (`OrderDto`) para no exponer nuestras entidades de dominio hacia afuera."* |
| `product.ReduceStock(...)` en `CreateOrderCommandHandler.cs` (L57) | *"Acá le resto el stock"* | *"Acá ejecutamos un **método de dominio** dentro de la entidad `Product` (Dominio Rico), que valida y encapsula la regla de negocio del stock."* |

---

## 📅 Parte 4: El Cronograma de 10 Días (Día por Día)

### 🗓️ DÍA 1: Clean Architecture y la Regla de Dependencia
**Concepto Completo:**
El sistema está dividido en **4 proyectos (.csproj)** que representan 4 capas concéntricas. La regla sagrada de Clean Architecture es la **Regla de Dependencia**: *las dependencias del código fuente solo pueden apuntar hacia adentro (hacia el Dominio)*.
* **`ECommerce.Domain`**: El núcleo. No tiene referencias a ningún otro proyecto ni paquetes pesados. Contiene Entidades (`Order`, `Product`, `User`) y Excepciones de Dominio (`DomainException`, `InsufficientStockException`).
* **`ECommerce.Application`**: Orquesta los casos de uso. Solo referencia a `Domain`. Define **Interfaces** (contratos como `IOrderRepository`, `IPaymentServiceClient`), Commands, Queries, Handlers, Validadores y DTOs. No sabe que existe SQLite ni HTTP.
* **`ECommerce.Infrastructure`**: Implementa los detalles técnicos externos. Referencia a `Application` y `Domain`. Contiene Entity Framework Core (`ApplicationDbContext`), los Repositorios concretos (`OrderRepository`), `JwtTokenService` y `PaymentServiceClient`.
* **`ECommerce.Api`**: Capa de presentación/entrada HTTP. Contiene Controladores, `GlobalExceptionHandler` y `Program.cs` (el *Composition Root* donde se unen las interfaces con sus implementaciones mediante Inyección de Dependencias).

**🎯 Práctica del Día 1:**
1. Abrí `ECommerce.Domain.csproj`, `ECommerce.Application.csproj` y `ECommerce.Infrastructure.csproj`.
2. Señalá las etiquetas `<ProjectReference>` en cada uno.
3. **Pregunta de examen:** *"¿Cómo hace `CreateOrderCommandHandler` (en Application) para guardar en la base de datos o llamar por HTTP si Application no tiene referencia a Infrastructure?"*
   * **Respuesta:** *"Por el **Principio de Inversión de Dependencias (la D de SOLID)**. Application define la interfaz (`IOrderRepository`, `IPaymentServiceClient`) y el Handler pide esa interfaz por constructor. En tiempo de ejecución, el contenedor de Inyección de Dependencias en `InfrastructureServiceExtensions.cs` (L24-33) le inyecta la clase concreta de Infrastructure."*

---

### 🗓️ DÍA 2: El Flujo de una Request Simple (Productos) + Tu Machete al 100%
**Concepto Completo:**
Antes de meterte con pagos y compensaciones, dominá el flujo limpio de **Crear Producto (`POST /api/products`)** y **Obtener Producto (`GET /api/products/{id}`)**.
* **CQRS (Command Query Responsibility Segregation):** Separamos las operaciones que **modifican estado (Commands)** de las que **solo consultan datos (Queries)**.
* **MediatR (Patrón Mediator):** Evita que el Controlador conozca e inyecte múltiples servicios o repositorios. El controlador solo conoce a `IMediator`.

**🎯 Práctica del Día 2 (Recorré estos 4 archivos con `F12` y `Alt + ←` mientras decís tu machete):**
1. **Controlador:** `ProductsController.Create` (L53-57) → Recibe el `CreateProductCommand` y hace `await _mediator.Send(request)`.
2. **Handler de Escritura:** `CreateProductCommandHandler.Handle` (L21-35) → Instancia la **Entidad de Dominio** `Product`, llama a `_productRepository.AddAsync(product)` y retorna el **DTO** `ProductDto`.
3. **Comparalo con una Query (Lectura):** Abrí `GetAllProductsQuery.cs` y `GetAllProductsQueryHandler.cs`. Fijate que una Query no modifica nada; en `Repository.GetAllAsync` (L18-21) usa `.AsNoTracking()` para que Entity Framework lea más rápido sin trackear cambios en memoria.

---

### 🗓️ DÍA 3: Dominio Rico (Rich Domain Model) vs. Dominio Anémico
**Concepto Completo:**
Un **Modelo de Dominio Anémico** es cuando las entidades son simples bolsas de `get; set;` públicos sin lógica, y toda la lógica está desparramada en los Handlers.
En este proyecto aplicamos **Dominio Rico**: las entidades protegen sus propias invariantes (reglas de negocio) mediante métodos y propiedades con `private set;`.

**🎯 Los 3 lugares exactos para mostrar Dominio Rico en el examen:**
1. **En `Product.cs` (L14-37):**
   * Método `UpdatePrice(decimal newPrice)`: Valida que el precio no sea negativo; si lo es, lanza `DomainRuleException`.
   * Método `ReduceStock(int quantity)`: Valida que `quantity > 0` y que `quantity <= Stock`. Si no alcanza el stock, lanza `InsufficientStockException`.
2. **En `Order.cs` (L15-34):**
   * `TransactionId` y `PaymentRejectReason` tienen **`private set;`** (L15-16). Nadie desde afuera puede hacer `order.TransactionId = "123"`.
   * Solo se pueden modificar llamando a los métodos de negocio: `order.MarkAsPaid(transactionId)`, `order.MarkPaymentAsRejected(reason)` o `order.Cancel()`.
3. **En el microservicio de pagos `Payment.cs` (L5-16):**
   * `Amount` tiene `private set;` y se inicializa por constructor.
   * El método `IsApproved()` encapsula la regla de negocio: aprueba si `Amount < 100000m` y rechaza si es `>= 100000m`.

---

### 🗓️ DÍA 4: Validaciones (FluentValidation + Pipeline Behavior) vs. Reglas de Dominio
**Concepto Completo:**
A los profesores les encanta preguntar: *"¿Por qué tenés validaciones con FluentValidation y también validaciones en el Dominio? ¿No es repetir código?"*
La respuesta técnica es distinguir **Validación Sintáctica/De Entrada (400 Bad Request)** de **Validación Semántica/De Negocio (422 Unprocessable Entity o 404 Not Found)**:

1. **Validación de Entrada (FluentValidation en el Pipeline de MediatR):**
   * Revisa que el comando tenga "forma válida" *antes* de tocar la base de datos (ej.: que `Quantity > 0` y `<= 100`, que la lista de `Items` no venga vacía, que el `Email` tenga formato de email).
   * **Cómo funciona automáticamente:** En `Program.cs` (L33-34) se registran todos los validadores y el `ValidationBehavior`.
   * Cuando hacés `_mediator.Send(request)`, MediatR ejecuta primero `ValidationBehavior.Handle` (L16-41). Este ejecuta `CreateOrderCommandValidator`. Si hay errores, lanza `ValidationException` (L37) y **corta el flujo antes de entrar al Handler**, devolviendo **`400 Bad Request`**.
2. **Validación de Negocio / Dominio (Dentro del Handler y Entidades):**
   * Requiere consultar el estado actual del sistema/base de datos: *"¿Existe el producto 5 en la BD?"* (`404 Not Found`), *"¿Alcanza el stock actual para comprar 3 unidades?"* (`422 Unprocessable Entity`), *"¿El servicio de pagos aprobó el monto?"* (`422 Unprocessable Entity`).

---

### 🗓️ DÍA 5: Persistencia con Entity Framework Core, Repositorio Genérico y Unit of Work
**Concepto Completo:**
* **`ApplicationDbContext.cs`:** Representa la sesión con la base de datos SQLite y expone los `DbSet<Product>`, `DbSet<Order>`, `DbSet<User>`, `DbSet<OrderItem>`. En `OnModelCreating` aplica las configuraciones Fluent API de la carpeta `Configurations`.
* **Patrón Repositorio (`Repository.cs`):** Abstrae las consultas de EF Core detrás de una interfaz.
* **Patrón Unit of Work (`UnitOfWork.cs`):** Asegura la **atomicidad transaccional**.

**🎯 Pregunta clave del Día 5:**
*"¿Por qué en `CreateOrderCommandHandler.cs` (L58 y L69) pasan `saveChanges: false` al actualizar los productos y agregar la orden, y después llaman a `_unitOfWork.SaveChangesAsync()`?"*
* **Respuesta:** *"Porque si compramos 5 productos distintos, no queremos hacer 5 `SaveChanges` separados en la base de datos (si fallara el 4to producto, los primeros 3 ya habrían descontado stock en la BD). Con `saveChanges: false`, solo marcamos los cambios en el Change Tracker en memoria de EF Core, y con `_unitOfWork.SaveChangesAsync()` (L70) impactamos todos los descuentos de stock y la creación de la orden en **una única transacción atómica**."*

---

### 🗓️ DÍA 6: El Caso de Uso Maestro (`CreateOrderCommandHandler`) y Compensación (Saga)
**Concepto Completo:**
Hoy estudiás a fondo `CreateOrderCommandHandler.cs`. Dividilo mentalmente en **3 bloques**:

* **Bloque 1 (Líneas 33-70) — Reserva local:** Verifica usuario (L33), trae todos los productos de una sola vez con `GetByIdsAsync` (L41) para evitar hacer un `SELECT` por cada ítem dentro del `foreach` (*problema N+1*), descuenta stock con `product.ReduceStock(item.Quantity)` (L57), crea la entidad `Order` en estado `Pending` (L44-50) y guarda en BD (L70).
* **Bloque 2 (Líneas 74-99) — Llamada a Pagos y Camino Feliz:** Crea el `PaymentRequestDto` (L74), llama a `_paymentServiceClient.ProcessPaymentAsync` (L82). Si vuelve `"Approved"`, llama a `order.MarkAsPaid(...)` (L86), guarda en BD (L88) y devuelve el `OrderDto` (L98).
* **Bloque 3 (Líneas 100-172) — Compensación / Rollback de Negocio:** Como en la línea 70 ya habíamos guardado el descuento de stock en la BD, si el pago es rechazado (L100), o el servicio de pagos está caído (`catch HttpRequestException` en L119), o da timeout (`catch TaskCanceledException` en L137), ejecutamos una **acción compensatoria**:
  1. Marcamos la orden como `PaymentRejected` con el motivo.
  2. Recorremos los ítems y **restituimos el stock** (`product.Stock += item.Quantity`).
  3. Guardamos con `_unitOfWork.SaveChangesAsync()`.
  4. Lanzamos `DomainRuleException` para que el cliente reciba un `422 Unprocessable Entity` explicando por qué falló el pago.

---

### 🗓️ DÍA 7: Comunicación entre Microservicios, `IHttpClientFactory` y Resiliencia con Polly
**Concepto Completo:**
El backend principal (`ECommerce`, puerto `5117`) se comunica de forma síncrona por HTTP REST con el microservicio `PaymentService` (puerto `5200`).

**🎯 Los 3 puntos que tenés que saber mostrar:**
1. **Registro del Typed HttpClient y Polly (`InfrastructureServiceExtensions.cs` L33-44):**
   * Se registra con `services.AddHttpClient<IPaymentServiceClient, PaymentServiceClient>(...)`.
   * **¿Por qué no hacer `new HttpClient()`?** Porque instanciar `HttpClient` manualmente por cada request no libera inmediatamente los sockets TCP del sistema operativo (quedan en estado `TIME_WAIT`), provocando **Socket Exhaustion** bajo carga. `IHttpClientFactory` reutiliza un pool de `HttpMessageHandler`.
   * **Polly (L42-43):** Configura `WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)))`. Es un **Retry con Exponential Backoff**: si hay un error transitorio de red o HTTP 5xx, reintenta 3 veces esperando 2s, 4s y 8s antes de rendirse y lanzar la excepción al Handler.
2. **Propagación del Token JWT (`PaymentServiceClient.cs` L33-40):**
   * Usa `IHttpContextAccessor` para leer el header `Authorization: Bearer ...` que mandó el usuario a `ECommerce` y lo copia en la petición saliente hacia `PaymentService`.
3. **El lado receptor en `PaymentService`:**
   * Recibe en `PaymentsController.ProcessPayment` (L20-36), envía el `ProcessPaymentCommand` por MediatR a `ProcessPaymentCommandHandler` (L12-49), que evalúa `payment.IsApproved()` (`Amount < 100000m`).

---

### 🗓️ DÍA 8: Seguridad de Punta a Punta (Hashing BCrypt, Login, JWT y Roles)
**Concepto Completo:**
* **Nunca guardamos contraseñas en texto plano:** Al registrar un usuario en `RegisterUserCommandHandler.cs` o al crear el Admin por defecto en `Program.cs` (L148), se usa `PasswordHasherService.cs` (que usa **BCrypt** con salt automático).
* **Flujo de Login (`POST /api/auth/login`):**
  1. Entra a `AuthController.Login` (L36-46).
  2. Envía `AuthenticateUserQuery` a `AuthenticateUserQueryHandler.cs` (L19-26).
  3. El Handler busca al usuario por username y verifica el hash con `_passwordHasher.Verify(...)`. Si no coincide, lanza `AuthenticationException` (que devuelve `401 Unauthorized`).
  4. Si es válido, devuelve el `UserDto` al `AuthController` (L44), que llama a `_jwtTokenService.GenerateToken(userDto)` (`JwtTokenService.cs` L21-43).
* **Diferencia entre Autenticación y Autorización:**
  * **Autenticación (`401 Unauthorized`):** *"¿Quién sos?"* Se comprueba con la firma HMAC-SHA256 y fecha de expiración del token JWT en `app.UseAuthentication()` (`Program.cs` L57-71).
  * **Autorización (`403 Forbidden`):** *"Sé quién sos, pero ¿tenés permiso para hacer esto?"* Se comprueba leyendo el claim `ClaimTypes.Role` del token en endpoints con `[Authorize(Roles = UserRoles.Admin)]`, como `ProductsController.Create` (L47).

---

### 🗓️ DÍA 9: Manejo Global de Errores (`GlobalExceptionHandler` y `ProblemDetails`)
**Concepto Completo:**
En ningún controlador vas a ver bloques `try / catch`. ¿Por qué? Porque usamos el middleware de .NET 8 `IExceptionHandler` implementado en `GlobalExceptionHandler.cs` (L14-101).
* Centraliza el manejo de errores y devuelve siempre un JSON estandarizado bajo la norma **RFC 7807 (`application/problem+json` / `ProblemDetails`)**.
* **Tabla de Mapeo de Excepciones (Líneas 31-91 de `GlobalExceptionHandler.cs`):**
  * `NotFoundException` → **`404 Not Found`** (ej. producto o usuario inexistente).
  * `ValidationException` (de FluentValidation) → **`400 Bad Request`** (con el diccionario de errores por campo).
  * `AuthenticationException` → **`401 Unauthorized`** (login incorrecto o token inválido en pago).
  * `DomainException` (incluye `DomainRuleException` e `InsufficientStockException`) → **`422 Unprocessable Entity`** (la sintaxis del JSON estaba bien, pero violó una regla de negocio como falta de stock o pago rechazado).
  * `default` (cualquier otro bug no controlado) → **`500 Internal Server Error`** (oculta los detalles técnicos al cliente por seguridad y guarda el stack trace en `error.log`).

---

### 🗓️ DÍA 10: Simulacro General en Vivo (La Demo + Defensa Oral de 15 Minutos)
El último día no leas teoría: hacé **2 pasadas completas del examen en vivo** cronometrando 15 minutos.

#### 🚀 Checklist del Simulacro en Vivo:
1. **Levantar ambos servicios:**
   * Consola 1: `dotnet run --project ECommerce/src/ECommerce.Api` (levanta en `http://localhost:5117`)
   * Consola 2: `dotnet run --project PaymentService/src/PaymentService.Api` (levanta en `http://localhost:5200`)
2. **Probar Login y Autorización en Swagger (`http://localhost:5117`):**
   * Ir a `POST /api/auth/login` con `username: "admin"`, `password: "AdminPassword123!"` (configurados en `appsettings.json` L14-18 y sembrados al iniciar en `Program.cs` L126-151).
   * Copiar el token, abrir el botón **Authorize** arriba a la derecha y poner `Bearer <TOKEN>`.
3. **Recorrer tu Machete en vivo con un Producto (`POST /api/products`):**
   * Crear un producto de `$20.000` con stock `10`.
   * Mostrar en VS Code el recorrido: `ProductsController.cs` → `CreateProductCommand.cs` → `CreateProductCommandHandler.cs` (nombrando **Command**, **Entidad de Dominio** y **DTO**).
4. **Probar los 3 escenarios de una Orden (`POST /api/orders`):**
   * **Escenario 1 (Aprobado < $100.000):** Comprar 2 unidades (`$40.000`). Devuelve `201 Created` con `status: "Paid"`. Mostrar en `GET /api/products/{id}` que el stock bajó de 10 a 8.
   * **Escenario 2 (Rechazado >= $100.000):** Comprar 6 unidades (`$120.000`). Devuelve `422 Unprocessable Entity` con `ProblemDetails`. Mostrar en `GET /api/products/{id}` que el stock **sigue en 8** gracias a la compensación en `CreateOrderCommandHandler.cs` (L105-114).
   * **Escenario 3 (Microservicio caído):** Frenar con `Ctrl + C` la consola de `PaymentService`. Intentar comprar 1 unidad. Mostrar cómo actúa Polly y cómo cae en el `catch (HttpRequestException)` (`CreateOrderCommandHandler.cs` L119-135), devolviendo el stock a 8 sin que `ECommerce.Api` se caiga.

