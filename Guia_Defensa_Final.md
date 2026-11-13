# 🎓 Guía Definitiva de Estudio: Defensa Final - ECommerce & PaymentService

Esta guía contiene absolutamente todo lo que necesitas saber y entender de tu proyecto para defenderlo con éxito. Está pensada para que la leas de principio a fin, entiendas el "por qué" de cada decisión técnica, y sepas exactamente qué responder a las preguntas del profesor.

---

## 🏗️ 1. Visión General del Proyecto

**¿Qué es este proyecto?**
Es una API REST para un sistema de ECommerce, acompañada de un microservicio secundario (PaymentService) para simular el procesamiento de pagos.
Están desarrollados en **.NET 8**, utilizando **C#**.

**Tecnologías y Patrones Clave (Palabras que debes mencionar):**
*   **Clean Architecture (Arquitectura Limpia):** Para separar las responsabilidades y que el negocio no dependa de la tecnología.
*   **CQRS (Command Query Responsibility Segregation):** Separación de operaciones de escritura (Commands) y lectura (Queries) usando la librería **MediatR**.
*   **JWT (JSON Web Tokens):** Para seguridad, autenticación y autorización.
*   **Entity Framework Core:** Como ORM (Object-Relational Mapper) usando Code-First y base de datos SQLite.
*   **Comunicación HTTP síncrona:** El ECommerce se comunica con el PaymentService a través de un cliente HTTP tipado (`IHttpClientFactory`).

---

## 📐 2. Arquitectura: Clean Architecture

El proyecto se divide en 4 capas concéntricas. La **Regla de Dependencia** es vital: *Las capas externas dependen de las internas, pero las internas NUNCA dependen de las externas.*

1.  **Domain (Capa de Dominio):** Es el corazón. No depende de NADA. Contiene las Entidades (`Order`, `Product`), los Enums, Excepciones personalizadas y las Reglas de Negocio (ej: validar si hay stock o si un monto es válido).
2.  **Application (Capa de Aplicación):** Depende de Domain. Contiene la lógica de los Casos de Uso (ej: "Crear Orden"). Aquí viven los Commands, Queries, Handlers, DTOs (Data Transfer Objects) e interfaces (ej: `IOrderRepository`). **NO SABE** que usas SQL, ni Entity Framework, ni HTTP.
3.  **Infrastructure (Capa de Infraestructura):** Depende de Application (y por ende, de Domain). Aquí es donde nos conectamos con el mundo exterior. Implementa el `DbContext` de Entity Framework, los repositorios reales, la generación de JWT y el cliente que llama a la API de pagos.
4.  **Api (Capa de Presentación):** Depende de Application e Infrastructure. Contiene los Controladores (Controllers) que exponen los endpoints, middlewares (para atrapar errores globalmente) y la configuración inicial (`Program.cs`, `appsettings.json`).

---
 
## ⚙️ 3. Flujo de Ejecución (End-to-End)

Si el profesor te pide: *"Explicame qué pasa cuando un usuario crea una orden"*, debes relatar este flujo:

1.  **Controller:** La petición HTTP POST llega a `OrdersController` en la capa Api.
2.  **MediatR:** El controlador no tiene lógica, simplemente crea un `CreateOrderCommand` (un objeto con los datos) y se lo pasa a MediatR (`_mediator.Send()`).
3.  **Handler:** MediatR busca en la capa Application quién maneja ese comando, y ejecuta el `CreateOrderCommandHandler`.
4.  **Lógica (Application):** El Handler usa los repositorios (`IProductRepository`, `IOrderRepository`) para validar que los productos existan y tengan stock.
5.  **Dominio:** Se crea la entidad `Order`.
6.  **Microservicio Externo (Infraestructura):** El Handler llama al servicio de pagos a través de una interfaz (`IPaymentServiceClient`), cuya implementación real en la capa de Infraestructura hace un HTTP POST a la URL de `PaymentService`.
7.  **Decisión:** 
    *   Si el PaymentService devuelve Ok (el pago es < $100.000), la orden se guarda como `Paid`.
    *   Si rechaza el pago (>= $100.000) o si el servicio está caído (Timeout), se atrapa la excepción, se devuelve el stock a los productos y la orden queda `PaymentRejected`.
8.  **Persistencia:** Se guarda todo en la base de datos a través de EF Core.

---

## ❓ 4. Preguntas Frecuentes del Profesor y Dónde Mostrarlo

### Pregunta 1: Regla de Dependencia en Clean Architecture
**Profesor:** *Señalame cada capa en el proyecto y justificá la regla de dependencia (por qué Application no referencia a Infrastructure).*
*   **Dónde mostrarlo:** Abre `ECommerce.Application.csproj` y muestra que solo hace referencia a `ECommerce.Domain`.
*   **Respuesta:** "La regla de dependencia dicta que dependemos hacia adentro. Application tiene los casos de uso y no debe acoplarse a tecnologías concretas (Infrastructure). Si mañana cambiamos SQL por MongoDB, los casos de uso no cambian ni una línea."

### Pregunta 2: CQRS y MediatR
**Profesor:** *Mostrame un Command y un Query concretos. Explicá por qué usaron MediatR y qué pasaría si pusieran esa lógica directamente en el Controller.*
*   **Dónde mostrarlo:** Un Command en `Application/Commands` y su Handler.
*   **Respuesta:** "Un Command cambia el estado (crea/actualiza), un Query solo lee. MediatR desacopla el Controller de la lógica. Si la lógica estuviera en el Controller, inyectaríamos decenas de repositorios, sería difícil de testear   y romperíamos el Principio de Responsabilidad Única (SRP)."

### Pregunta 3: Reglas de Negocio en el Dominio (Dominio Rico)
**Profesor:** *Señalá dónde vive una regla de negocio y explicala.*
*   **Dónde mostrarlo:** Entidad `Payment.cs` en PaymentService (`IsApproved()`).
*   **Respuesta:** "Las reglas de negocio no deben estar tiradas en los Handlers, sino encapsuladas en las entidades. En `Payment`, el método `IsApproved()` aprueba solo pagos menores a $100.000. La entidad valida su propio estado."

### Pregunta 4: Cliente HTTP (Microservicios)
**Profesor:** *Mostrá el registro de HttpClient y explicá por qué usan IHttpClientFactory y no `new HttpClient()`.*
*   **Dónde mostrarlo:** `InfrastructureServiceExtensions.cs` (donde dice `services.AddHttpClient`).
*   **Respuesta:** "Usamos un 'Typed Client' con `IHttpClientFactory`. No usamos `new HttpClient()` porque instanciarlo a mano agota los sockets del sistema (Socket Exhaustion). El factory maneja eficientemente el ciclo de vida y la conexión."

### Pregunta 5: Contratos y DTOs
**Profesor:** *Señalá el DTO de contrato y explicá por qué no exponen la entidad de dominio por HTTP.*
*   **Dónde mostrarlo:** Carpeta de DTOs en `Application`.
*   **Respuesta:** "Usamos DTOs (Data Transfer Objects) para definir el contrato. No exponemos las Entidades (ej: User u Order) porque pueden tener datos sensibles (passwords) o referencias circulares. El DTO asegura que solo viaja por la red lo estrictamente necesario."

### Pregunta 6: Resiliencia (Fallo en Microservicio)
**Profesor:** *¿Qué pasa si el servicio de pagos está caído? ¿Se cae todo tu sistema?*
*   **Dónde mostrarlo:** `CreateOrderCommandHandler.cs` (el bloque `try/catch` que atrapa `HttpRequestException`).
*   **Respuesta:** "No se cae el sistema. Configuramos un timeout. Si el servicio de pagos falla, el bloque catch lo atrapa, marcamos la orden como `PaymentRejected`, devolvemos el stock reservado y devolvemos un mensaje de error claro al usuario."

### Pregunta 7: Configuración (Appsettings)
**Profesor:** *Mostrá de dónde sale la URL del segundo servicio y qué pasa si cambia el puerto.*
*   **Dónde mostrarlo:** `appsettings.json` (Sección `PaymentSettings:BaseUrl`).
*   **Respuesta:** "Está centralizada en `appsettings.json`. Si cambia de puerto o vamos a la nube, solo editamos este archivo de texto sin tener que recompilar el código C# (Options Pattern)."

### Pregunta 8: JWT, Autenticación vs Autorización
**Profesor:** *Mostrá cómo se genera el JWT, explicá la diferencia entre autenticación y autorización.*
*   **Dónde mostrarlo:** `JwtTokenService.cs` (Generación) y un Controller con `[Authorize(Roles="Admin")]`.
*   **Respuesta:** "En `JwtTokenService` generamos el token inyectando 'claims' (ID, Rol). **Autenticación** es '¿Quién eres?' (login exitoso da el token). **Autorización** es '¿Qué puedes hacer?' (el atributo Authorize bloquea a usuarios sin rol Admin)."

### Pregunta 9: Inversión de Dependencias (SOLID)
**Profesor:** *¿Por qué la interfaz del repositorio está en una capa y la implementación en otra?*
*   **Dónde mostrarlo:** Interfaz en `Application/Interfaces` e Implementación en `Infrastructure/Repositories`.
*   **Respuesta:** "Es el Principio de Inversión de Dependencias (D de SOLID). Los casos de uso (alto nivel) dictan qué datos necesitan (la Interfaz). La Infraestructura (bajo nivel) obedece e implementa el cómo (Entity Framework). Ambos dependen de la abstracción."

---

## 🚀 5. Cómo Demostrar el Proyecto en Vivo

1. **Levantar ambos proyectos:** Ejecuta ambos proyectos (ECommerce en 5117 y PaymentService en 5200).
2. **Generar Token (Login):** Usa Swagger en ECommerce `POST /api/auth/login` con:
   *   Email: `admin@ecommerce.com`
   *   Password: `AdminPassword123!`
3. **Autorizar Swagger:** Copia el token devuelto, haz clic en el candado "Authorize" de Swagger y escribe `Bearer pegatu_token_aqui`.
4. **Probar Regla de Negocio:**
   *   Crea una orden de poco valor (< $100.000) y muestra que queda `Paid`.
   *   Crea una orden muy cara (>= $100.000) y muestra que queda `PaymentRejected`.
5. **Probar Resiliencia (Sistema Caído):**
   *   Apaga la consola de `PaymentService.Api`.
   *   Intenta crear otra orden.
   *   Demuestra que el sistema principal sigue vivo, que devuelve un error controlado por timeout, y que no se cae la base de datos principal.

¡Con esto estás listo para sacar un 10! Éxitos.
