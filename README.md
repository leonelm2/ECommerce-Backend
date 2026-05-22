# 🛍️ ECommerce API - Clean Architecture con .NET 8 & JWT

Hola profe,

Este es mi proyecto de **ECommerce API** desarrollado para la materia, utilizando **.NET 8** y aplicando **Clean Architecture**, **Domain-Driven Design (DDD) Lite**, **CQRS (con MediatR)**, **FluentValidation** y manejo centralizado de errores.

La persistencia de datos se resolvió mediante **SQLite** y **Entity Framework Core**, y la seguridad se implementó mediante tokens **JWT** estructurados por roles (`Admin` y `User`).

---

## 🏗️ Diseño y Decisiones de Arquitectura

El sistema se diseñó dividiendo la solución en 4 capas de responsabilidad única:

1. **ECommerce.Domain**:
   * Es el núcleo de la aplicación.
   * Contiene las entidades puras (`Product`, `Order`, `OrderItem`, `User`) y las excepciones de dominio (`NotFoundException`, `DomainRuleException`, etc.).
   * Se aplicó **DDD Lite**: la lógica de negocio no está dispersa en servicios anémicos, sino encapsulada en las propias entidades. Por ejemplo, el producto es responsable de reducir su stock (`ReduceStock`) y validar que el precio no sea negativo (`UpdatePrice`).
2. **ECommerce.Application**:
   * Define los casos de uso a través de comandos (`Commands`) y consultas (`Queries`).
   * Se utilizó **MediatR** para desacoplar los controladores de los casos de uso.
   * Las reglas de validación sintáctica se definieron con **FluentValidation** y se inyectaron automáticamente al pipeline de MediatR usando un `ValidationBehavior`, deteniendo peticiones incorrectas antes de que lleguen a los manejadores de negocio.
3. **ECommerce.Infrastructure**:
   * Implementa la persistencia física usando **Entity Framework Core**.
   * Define la configuración relacional de datos a través de **Fluent API** en lugar de data annotations, manteniendo las entidades del dominio limpias.
   * Implementa los patrones **Repository** y **Unit of Work** para abstraer el acceso a datos.
4. **ECommerce.Api (Composition Root)**:
   * Expone los endpoints RESTful.
   * Configura la autenticación y autorización basada en claims (roles) de JWT.
   * Registra el middleware global de captura de excepciones para formatear los errores.

---

## 🛠️ Problemas Encontrados y Soluciones Aplicadas

Durante el desarrollo y refinamiento del proyecto surgieron varios desafíos técnicos:

1. **Fuga de Dependencias en la Capa de Aplicación**:
   * *Problema*: El proyecto `ECommerce.Application.csproj` contenía referencias a paquetes de Entity Framework Core y SQLite. Esto rompía el principio fundamental de Clean Architecture, ya que la lógica de negocio se acoplaba a la tecnología de persistencia.
   * *Solución*: Removí completamente las dependencias de EF Core y SQLite del archivo `.csproj` de aplicación. El proyecto ahora compila de forma pura y desacoplada, dependiendo únicamente de abstracciones.
2. **Mal Acoplamiento del Middleware de Excepciones**:
   * *Problema*: El manejador global de excepciones estaba ubicado en la capa de `Infrastructure`. La infraestructura de persistencia no debe tener conocimiento de tecnologías de presentación o transporte (como `HttpContext` de ASP.NET Core).
   * *Solución*: Trasladé el middleware a la capa de `Api` (`Middleware/GlobalExceptionHandler.cs`), donde corresponde conceptualmente en el flujo de peticiones web.
3. **Advertencias de Ocultación de Campos (Warning CS0108)**:
   * *Problema*: En la clase `OrderRepository` se declaraba un campo privado `_context` que causaba advertencias del compilador al ocultar el mismo miembro heredado del repositorio genérico base.
   * *Solución*: Eliminé el campo duplicado y modifiqué el constructor para alimentar correctamente la clase base, garantizando una compilación limpia sin advertencias.
4. **Rutas e Interfaces Incompletas**:
   * *Problema*: El endpoint de creación de órdenes retornaba un código `201 Created` con una ruta para obtener la orden por ID, pero dicha consulta (`GET`) y su correspondiente manejador de MediatR no existían en el backend.
   * *Solución*: Diseñé e implementé la query `GetOrderByIdQuery`, su handler y el endpoint respectivo en `OrdersController`, cerrando de forma limpia el flujo REST.
5. **Payload Incompleto en Tests de Integración**:
   * *Problema*: La prueba de integración `AdminPermissionsTests` fallaba con errores `400 Bad Request` debido a que el JSON enviado para crear productos omitía la propiedad `categoryId`, violando la regla del validador de comandos.
   * *Solución*: Modifiqué el payload del test para incluir un ID de categoría válido (`categoryId = 1`), permitiendo verificar correctamente que el flujo de autorización para administradores funciona.

---

## 🚀 Cómo ejecutar el proyecto en su PC

Siga estos pasos para compilar, migrar y ejecutar el proyecto localmente:

### 1. Requisitos Previos
* Tener instalado el **.NET 8.0 SDK**.
* Tener instalada la herramienta de EF Core. Si no la tiene, ejecute en su consola:
  ```bash
  dotnet tool install --global dotnet-ef
  ```

### 2. Restaurar y Compilar la Solución
Desde la terminal en la raíz del proyecto, ejecute:
```bash
dotnet restore
dotnet build
```

### 3. Migraciones y Base de Datos (SQLite)
La base de datos SQLite se crea y migra **automáticamente** al iniciar el proyecto. No obstante, si prefiere aplicar o recrear el esquema de base de datos manualmente, ejecute:
```bash
dotnet ef database update -p src/ECommerce.Infrastructure -s src/ECommerce.Api
```

### 4. Ejecutar las Pruebas de Integración
Para verificar que el sistema funciona correctamente, ejecute:
```bash
dotnet test
```

### 5. Iniciar la API
Inicie el servidor web de desarrollo ejecutando:
```bash
dotnet run --project src/ECommerce.Api
```

### 6. Acceder a Swagger
Una vez corriendo, abra su navegador e ingrese a:
* **Swagger UI**: [https://localhost:5001/](https://localhost:5001/) o [http://localhost:5117/](http://localhost:5117/)

---

## 🔑 Credenciales de Prueba (Seeding Automático)
Al iniciar la aplicación por primera vez, el sistema siembra de forma automática un usuario con rol de **Administrador** si la tabla está vacía:

* **Usuario**: `admin`
* **Contraseña**: `Admin123!`
* **Rol**: `Admin`

---

## 🛡️ Flujo de Pruebas en Swagger

### Paso 1: Autenticación
1. Vaya al endpoint **`POST /api/auth/login`**.
2. Presione *Try it out* e inicie sesión con las credenciales de administrador:
   ```json
   {
     "username": "admin",
     "password": "Admin123!"
   }
   ```
3. Copie el token JWT generado en la respuesta (`Token`).
4. Presione el botón **Authorize** en la parte superior derecha de Swagger, pegue el token en formato:
   ```text
   Bearer <SU_TOKEN_AQUÍ>
   ```
5. Presione *Authorize*. Ya está autenticado como administrador.

### Paso 2: Crear un Producto (Rol Admin)
1. Vaya a **`POST /api/products`**.
2. Envíe el payload para registrar un nuevo producto (ej. `categoryId: 1`).
3. El servidor le responderá con un código **`201 Created`**.

### Paso 3: Probar las Validaciones y Errores Centralizados
* **Validación de campos (400 Bad Request)**: Intente crear un producto con precio negativo o nombre vacío. El sistema retornará un `ValidationProblemDetails` formateado con los errores del modelo.
* **Acceso Denegado (403 Forbidden)**: Regístrese como un nuevo usuario en `POST /api/auth/register` (obtendrá el rol `User` por defecto). Inicie sesión con este usuario, autorice el token en Swagger e intente crear un producto en `POST /api/products`. Retornará un **403 Forbidden** ya que solo el rol `Admin` tiene permisos de escritura.
* **Control de Reglas de Negocio (422 Unprocessable Entity)**: Intente realizar una compra en `POST /api/orders` solicitando más stock del disponible. El dominio lanzará una excepción controlada (`InsufficientStockException`) y el middleware la transformará en una respuesta **422 Unprocessable Entity**.
* **Recurso No Encontrado (404 Not Found)**: Busque una orden o un producto con un ID inexistente en `GET /api/orders/{id}`. Retornará un **404 Not Found** controlado.
