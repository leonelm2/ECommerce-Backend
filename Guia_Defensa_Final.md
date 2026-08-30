# Guía de Estudio: Defensa Final - ECommerce & PaymentService

Esta guía está diseñada para que puedas preparar y ensayar tu defensa oral basándote en los criterios de evaluación y los puntos requeridos por la cátedra. Contiene las preguntas clave que te pueden hacer y la ruta exacta a los archivos donde debes mostrar la respuesta.

## 1. Clean Architecture y Regla de Dependencia
**Pregunta:** *Señalame cada capa en el proyecto y justificá la regla de dependencia (por qué Application no referencia a Infrastructure).*
*   **Dónde mostrarlo:** 
    *   Abre el archivo `CleanArchitectureApi.sln` (o los `.sln` individuales) y muestra la estructura de carpetas (`Domain`, `Application`, `Infrastructure`, `Api`).
    *   Abre el archivo `ECommerce.Application.csproj` y muestra que solo hace referencia a `ECommerce.Domain`.
*   **Respuesta a estudiar:** 
    > "La regla de dependencia de Clean Architecture dicta que las dependencias solo pueden apuntar hacia adentro (hacia el Dominio). `Application` contiene la lógica de negocio (casos de uso) y no debe depender de `Infrastructure` porque la lógica no debe acoplarse a tecnologías concretas como bases de datos (EF Core) o clientes HTTP. Si mañana cambiamos SQL Server por MongoDB, los casos de uso en `Application` no cambian ni una línea."

## 2. CQRS y MediatR
**Pregunta:** *Mostrame un Command y un Query concretos con sus Handlers. Explicá qué hace cada uno, por qué usaron MediatR y qué pasaría si pusieran esa lógica directamente en el Controller.*
*   **Dónde mostrarlo:** 
    *   Command: `ECommerce/src/ECommerce.Application/Commands/Orders/CreateOrderCommand.cs` y su handler.
    *   Query: `ECommerce/src/ECommerce.Application/Queries/Products/GetAllProductsQuery.cs` y su handler.
*   **Respuesta a estudiar:**
    > "Un **Command** es una intención de cambiar el estado del sistema (ej: crear una orden), mientras que un **Query** es solo una lectura de datos (ej: traer productos) sin modificar nada. Usamos **MediatR** para implementar este patrón (CQRS). Sirve para desacoplar el controlador de la lógica de negocio. Si pusiéramos la lógica en el Controller, este tendría muchísimas responsabilidades, inyectaría demasiados repositorios, sería difícil de testear unitariamente y romperíamos el principio de responsabilidad única (SRP)."

## 3. Reglas de Negocio en el Dominio
**Pregunta:** *Señalá dónde vive una regla de negocio y explicala.*
*   **Dónde mostrarlo:** 
    *   `PaymentService/src/PaymentService.Api/Core/Domain/Entities/Payment.cs` (Método `IsApproved()`).
*   **Respuesta a estudiar:**
    > "Las reglas de negocio no deben estar tiradas en los Handlers o Controllers, deben estar encapsuladas en las entidades (Dominio Rico). Acá en la entidad `Payment`, el método `IsApproved()` contiene la regla que dice que un pago se aprueba solo si el monto es menor a $100.000. La entidad controla y valida su propio estado."

## 4. Comunicación HTTP y Cliente Tipado
**Pregunta:** *Mostrá el registro de HttpClient y explicá por qué usan IHttpClientFactory y no `new HttpClient()`.*
*   **Dónde mostrarlo:** 
    *   `ECommerce/src/ECommerce.Infrastructure/InfrastructureServiceExtensions.cs` (Línea donde dice `services.AddHttpClient<IPaymentServiceClient, PaymentServiceClient>`).
*   **Respuesta a estudiar:**
    > "Registramos un 'Typed Client' mediante `IHttpClientFactory`. No usamos `new HttpClient()` manualmente porque cada instancia de HttpClient abre conexiones a nivel de sistema operativo (sockets). Si creamos muchos y no los liberamos bien, generamos un problema llamado *Socket Exhaustion*. Además, el factory maneja internamente el ciclo de vida de los handlers y responde correctamente a cambios de DNS."

## 5. Contratos y DTOs
**Pregunta:** *Señalá el DTO de contrato y explicá por qué no exponen la entidad de dominio por HTTP.*
*   **Dónde mostrarlo:** 
    *   `PaymentRequestDto.cs` y `PaymentResponseDto.cs`.
*   **Respuesta a estudiar:**
    > "Usamos DTOs (Data Transfer Objects) para definir el contrato de la API. No exponemos la entidad de dominio (`Order` o `Payment`) porque la entidad puede tener datos sensibles, referencias circulares o propiedades de navegación a la base de datos que no le importan al cliente. El DTO asegura que solo viaja por la red la información estrictamente necesaria."

## 6. Flujo End-to-End y Resiliencia
**Pregunta:** *Recorré una request completa en voz alta y explicá qué ocurre si el segundo servicio no responde.*
*   **Dónde mostrarlo:** 
    *   `ECommerce/src/ECommerce.Application/Commands/Orders/CreateOrderCommandHandler.cs`.
*   **Respuesta a estudiar:**
    > "1. Entra la request al `OrdersController`.\n2. Se despacha el `CreateOrderCommand` a través de MediatR.\n3. El Handler busca los productos, verifica stock, crea la `Order` y la guarda temporalmente.\n4. El Handler llama al `_paymentServiceClient.ProcessPaymentAsync`.\n5. Si responde 'Approved', se marca la orden como pagada. Si responde 'Rejected', se marca como rechazada y se devuelve el stock.\n**¿Qué pasa si el servicio de pagos está caído?** El bloque `catch (HttpRequestException)` o `catch (TaskCanceledException)` atrapa el error (ya que configuramos un timeout de 10 segundos). En lugar de que la app principal crashee, marcamos la orden como `PaymentRejected`, devolvemos el stock reservado y lanzamos una excepción de dominio clara indicando que el servicio no está disponible."

## 7. Configuración (Appsettings)
**Pregunta:** *Mostrá de dónde sale la URL del segundo servicio y qué pasa si cambia el puerto.*
*   **Dónde mostrarlo:** 
    *   `ECommerce/src/ECommerce.Api/appsettings.json` (Sección `PaymentSettings:BaseUrl`).
*   **Respuesta a estudiar:**
    > "La URL está centralizada en el `appsettings.json`. Si cambia el puerto o pasamos el servicio a un entorno en la nube, solo editamos este archivo de texto. No tenemos que modificar el código C# ni recompilar la aplicación."

## 8. Seguridad: JWT, Autenticación y Autorización
**Pregunta:** *Mostrá cómo se genera el JWT, explicá la diferencia entre autenticación y autorización, y mostrá un endpoint protegido.*
*   **Dónde mostrarlo:** 
    *   Generación: `ECommerce/src/ECommerce.Infrastructure/Services/JwtTokenService.cs`.
    *   Endpoint protegido: Cualquier Controller con `[Authorize(Roles="Admin")]`.
    *   Admin: `appsettings.json` (Sección `AdminSettings`).
*   **Respuesta a estudiar:**
    > "En el `JwtTokenService` generamos el token inyectando claims (información del usuario como su ID, Email y su Rol). \n**Diferencia:** La *Autenticación* responde a '¿Quién eres?' (es decir, el login que verifica email y password para darte el token). La *Autorización* responde a '¿Qué puedes hacer?' (es el atributo `[Authorize(Roles="Admin")]` que lee los claims del token y bloquea a un usuario común si no tiene el rol necesario).\nEl usuario Admin se crea dinámicamente al levantar la aplicación leyendo los datos del appsettings, asegurando que siempre exista un administrador inicial."

## 9. Repositorios e Inversión de Dependencias
**Pregunta:** *Señalá dónde se define la interfaz del repositorio y dónde se implementa, y por qué va así.*
*   **Dónde mostrarlo:** 
    *   Interfaz: `ECommerce/src/ECommerce.Application/Interfaces/IOrderRepository.cs`.
    *   Implementación: `ECommerce/src/ECommerce.Infrastructure/Repositories/OrderRepository.cs`.
*   **Respuesta a estudiar:**
    > "La interfaz se define en `Application` porque los casos de uso son los que dictan *qué* datos necesitan recuperar o guardar, sin importar la tecnología. La implementación real va en `Infrastructure` porque es ahí donde usamos Entity Framework Core para conectarnos a la base de datos SQL. Esto respeta el **Principio de Inversión de Dependencias** (D de SOLID): los módulos de alto nivel (casos de uso) no dependen de detalles de bajo nivel (base de datos), ambos dependen de abstracciones (la interfaz)."
