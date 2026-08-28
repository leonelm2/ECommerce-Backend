# REVISIÓN FINAL DEL EXAMEN

## ESTADO GENERAL DEL PROYECTO
**LISTO PARA APROBAR.** 
El proyecto cumple actualmente con todos los requisitos solicitados para el examen final. La arquitectura fue refactorizada donde era necesario (como en PaymentService) y los flujos manejan escenarios de éxito, rechazo y caída del servicio correctamente.

## PROBLEMAS ENCONTRADOS Y CORREGIDOS

### Críticos
1. **Falta de Clean Architecture en PaymentService**: El servicio secundario consistía únicamente de un controlador sin capas. 
2. **Manejo Incorrecto de Caídas (Timeouts/Excepciones)**: El manejador de crear órdenes capturaba los errores HTTP y marcaba la orden como `Cancelled` genéricamente. Además, `PaymentServiceClient` enmascaraba las excepciones de red como excepciones de dominio.

### Importantes
3. **Encapsulamiento del Dominio `Order`**: No existían métodos explícitos para los cambios de estado (todo se hacía modificando las propiedades públicas).
4. **Estado de Rechazo Inexistente**: El enumerador `OrderStatus` no contaba con el estado `PaymentRejected`.
5. **Swagger en PaymentService**: Si bien existía Swagger, no estaba usando MediatR ni CQRS.
6. **Contraseña Admin no permitida**: El Seed lanzaba excepción porque la contraseña contenía `"ReplaceWithSecure"`.

### Menores
7. **Falta de Documentación**: El `README.md` no mencionaba la existencia de `PaymentService.Api` ni cómo ejecutarlo.

## CORRECCIONES REALIZADAS
1. **Refactorización de PaymentService**:
   - Se crearon las carpetas lógicas `Core/Domain`, `Core/Application` (Commands/DTOs) en el proyecto `PaymentService.Api`.
   - Se instaló e implementó MediatR.
   - La regla de negocio se movió a una entidad de Dominio (`Payment.cs`) y se invocó desde `ProcessPaymentCommandHandler`.
2. **Encapsulamiento de ECommerce.Domain**:
   - Se agregó `PaymentRejected` a `OrderStatus`.
   - Se agregaron los métodos `MarkAsPaid` y `MarkPaymentAsRejected` en `Order`.
   - Se crearon las propiedades para almacenar el ID de Transacción y motivo de rechazo en la BD (aplicando migración `AddPaymentFieldsToOrder`).
3. **Manejo Robusto de Comunicación HTTP**:
   - Se modificó `CreateOrderCommandHandler` para manejar explícitamente `HttpRequestException` y `TaskCanceledException` (timeout), marcando la orden como `PaymentRejected` y revirtiendo el stock, devolviendo un error claro al cliente sin que la app explote.
   - `PaymentServiceClient` ahora permite que las excepciones HTTP nativas "burbujeen" hacia arriba.
4. **Auth y Admin**:
   - Se cambió la clave JWT y las credenciales Admin en `appsettings.json` a valores válidos para que el Seed funcione sin errores en entorno de desarrollo y un profesor pueda evaluar fácilmente el acceso Admin.
5. **Documentación**:
   - Se actualizó el `README.md` detallando la arquitectura y cómo probar el proyecto en conjunto.

## CHECKLIST DEL FINAL

| Requisito | Estado | Observación |
| :--- | :---: | :--- |
| Dos servicios independientes | ✅ Cumple | Ambos en proyectos separados en la Solución. |
| Comunicación E-Commerce -> PaymentService vía HTTP | ✅ Cumple | Realizada con IHttpClientFactory y Tipado. |
| Actualización correcta de estados (Paid / PaymentRejected) | ✅ Cumple | Implementado en `CreateOrderCommandHandler`. |
| Clean Architecture en ambos proyectos | ✅ Cumple | PaymentService ahora usa carpetas por capas y MediatR. |
| CQRS + MediatR | ✅ Cumple | Todo el flujo pasa por Comandos y Handlers. |
| PaymentService independiente y puerto propio | ✅ Cumple | Puerto 5200. |
| Regla de negocio en Dominio de PaymentService | ✅ Cumple | Entidad `Payment.cs`. |
| Manejo de caída del servicio de pagos | ✅ Cumple | `HttpRequestException` manejada. |
| Encapsulamiento en Entidades (Ej: `MarkAsPaid`) | ✅ Cumple | Se agregaron métodos a `Order`. |
| EF Core, Migraciones y Persistencia coherente | ✅ Cumple | Migración `AddPaymentFieldsToOrder` aplicada. |
| JWT, Registro, Login y creación de Admin en runtime | ✅ Cumple | Admin creado por Seed (`Program.cs`). Endpoint Roles Admin existe (`ProductsController`). |
| Swagger configurado | ✅ Cumple | Ambos proyectos cuentan con Swagger. |
| README completo | ✅ Cumple | Actualizado. |

---

## POSIBLES PREGUNTAS DEL PROFESOR (Defensa Oral)

**1. ¿Qué es Clean Architecture y por qué la regla de dependencia es hacia adentro?**
*Respuesta:* Es una arquitectura en capas (Domain, Application, Infrastructure, API) donde la regla de dependencia exige que las capas externas dependan de las internas. El Dominio (el centro) no depende de nadie, garantizando que la lógica de negocio esté aislada de detalles técnicos (como bases de datos o frameworks web).

**2. ¿Para qué usamos CQRS y MediatR?**
*Respuesta:* CQRS separa las operaciones de lectura (Queries) de las de escritura (Commands) permitiendo optimizarlas de forma independiente. MediatR facilita esto implementando el patrón mediador: el controlador solo envía un comando, y MediatR se encarga de rutearlo al Handler correspondiente (desacoplando el controlador de la lógica).

**3. ¿Cómo te comunicás entre los microservicios? ¿Por qué no hiciste `new HttpClient()`?**
*Respuesta:* Me comunico mediante HTTP POST. Utilizo `IHttpClientFactory` y un Cliente Tipado (`PaymentServiceClient`) inyectado por dependencias porque evita el agotamiento de sockets (socket exhaustion) y maneja el ciclo de vida y reciclaje de conexiones de manera óptima por detrás, además de permitir configurar BaseAddress o Polly desde un solo lugar.

**4. ¿Qué pasa con la orden si PaymentService está apagado?**
*Respuesta:* El cliente de HTTP arroja una `HttpRequestException` (o `TaskCanceledException` por timeout). Mi handler captura explícitamente estas excepciones de red, llama a `order.MarkPaymentAsRejected("Error de conexión...")`, actualiza la orden, devuelve el stock reservado y lanza una excepción de dominio controlada. La app NO se cae y la BD queda en un estado coherente y auditable.

**5. ¿Por qué agregaste métodos como `MarkAsPaid()` en la clase Order en vez de asignar el Status en el Handler?**
*Respuesta:* Por el principio de **Encapsulamiento** y el **Modelo de Dominio Rico** (Rich Domain Model). Es el Dominio quien debe conocer y gestionar cómo cambia su estado, y no el Application layer. Esto previene que una orden se asigne a estados inválidos desde fuera.

**6. ¿Cómo aplicaste Clean Architecture en el PaymentService siendo tan chico?**
*Respuesta:* Lo estructure utilizando carpetas (`Core/Domain`, `Core/Application`, `Infrastructure`) dentro del mismo proyecto. Así mantengo la Regla de Dependencia (el controlador llama a MediatR, MediatR ejecuta el Handler en Application, y este instancia una Entidad de Domain con la regla de negocio), evitando la sobreingeniería de crear 4 sub-proyectos para un microservicio pequeño, pero demostrando que entiendo la arquitectura subyacente.
