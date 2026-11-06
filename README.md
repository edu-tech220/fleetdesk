# FleetDesk

Plataforma de gestion de mantenimiento preventivo y correctivo para flotas vehiculares
(camionetas, vans, unidades de reparto). Resuelve un problema real de cualquier empresa con
vehiculos propios: saber **que unidades tienen el servicio vencido**, **controlar las ordenes
de trabajo del taller** y **no quedarse sin refacciones a la mitad de una reparacion** cuando
varios mecanicos trabajan al mismo tiempo.

## Que problema resuelve

- Cada vehiculo tiene un plan de mantenimiento (cada *N* kilometros o *M* meses). FleetDesk
  calcula automaticamente que vehiculos estan vencidos combinando ambas reglas.
- Un vehiculo solo puede tener **una orden de trabajo abierta a la vez** y pasa a estado
  "En taller" mientras se repara.
- Cada mecanico tiene un **limite de ordenes abiertas simultaneas** (evita que se le asignen
  mas reparaciones de las que puede atender).
- Al agregar una refaccion a una orden, el inventario se descuenta con **concurrencia
  optimista**: si dos mecanicos intentan usar la ultima pieza casi al mismo tiempo, el segundo
  recibe un conflicto claro (409) en vez de dejar el stock en negativo. Si no hay piezas
  suficientes, la orden pasa automaticamente a **"Esperando refacciones"**.
- Al completar una orden se actualiza el odometro del vehiculo y se recalcula su proximo
  mantenimiento.

## Arquitectura

Microservicios con dos aplicaciones ASP.NET Core independientes que **no comparten ensamblados**
(cada una define sus propios DTOs; solo el proyecto de pruebas referencia el backend para poder
usar `WebApplicationFactory`):

```
                    JWT (Bearer)
┌──────────────────┐  HTTP   ┌──────────────────┐        ┌───────────────┐
│ FleetDesk.Web     │ ------> │ FleetDesk.Api     │ ----> │ SQL Server     │
│ Razor Pages (MVC) │         │ ASP.NET Core      │       │ (SQLite en     │
│ Sesion por cookie │         │ Web API           │       │  pruebas)      │
└──────────────────┘         └──────────────────┘        └───────────────┘
```

- **FleetDesk.Api**: microservicio backend con toda la logica de negocio (vehiculos,
  refacciones, ordenes de trabajo, reportes). Expone una API REST documentada con Swagger.
- **FleetDesk.Web**: frontend en Razor Pages (HTML + CSS, sin framework de JS). Llama al
  backend a traves de `IHttpClientFactory`. El usuario inicia sesion contra el backend y el
  JWT recibido se guarda como claim dentro de la cookie de autenticacion del sitio; un
  `DelegatingHandler` (`JwtForwardingHandler`) lo recupera en cada peticion y lo reenvia como
  `Authorization: Bearer` al microservicio.
- **FleetDesk.Api.Tests**: pruebas xUnit de extremo a extremo con `WebApplicationFactory`,
  reemplazando SQL Server por SQLite en memoria. No requieren Docker ni servicios externos.

## Decisiones tecnicas

- **Concurrencia optimista sin `rowversion` nativo**: en vez de depender de un tipo
  `rowversion`/`timestamp` generado por SQL Server (que SQLite no soporta igual), cada entidad
  sensible a condiciones de carrera (`Part`, usado al descontar inventario) tiene un `Guid
  RowVersion` que la aplicacion renueva manualmente. El cliente debe enviar el valor que vio al
  leer el recurso; el backend fija ese valor como `OriginalValue` del token de concurrencia de
  EF Core antes de guardar, por lo que un cambio concurrente produce un
  `DbUpdateConcurrencyException` real (traducido a `409 Conflict`). Esto funciona identico en
  SQL Server y SQLite, lo que permite probarlo con pruebas automatizadas sin levantar SQL Server.
- **Maquina de estados explicita para las ordenes de trabajo**: `Pending -> InProgress ->
  (WaitingParts <-> InProgress) -> Completed | Cancelled`. Las transiciones invalidas devuelven
  `409 Conflict` con un mensaje claro en vez de fallar silenciosamente.
- **Migraciones vs. `EnsureCreated`**: la API usa migraciones de EF Core reales contra SQL
  Server (aplicadas automaticamente al arrancar). Las pruebas usan `EnsureCreated()` sobre
  SQLite en lugar de reproducir las mismas migraciones, porque algunos tipos de columna
  generados para SQL Server (`nvarchar(max)`) no son validos en SQLite; `EnsureCreated` reconstruye
  el esquema a partir del modelo usando el proveedor activo en cada caso.
- **Autorizacion por rol + propiedad del recurso**: `Admin`, `Mechanic` y `FleetManager` tienen
  permisos distintos via `[Authorize(Roles = ...)]`, y ademas un mecanico solo puede modificar
  sus propias ordenes de trabajo (un administrador puede modificar cualquiera).
- **Contrasenas con PBKDF2** (`Rfc2898DeriveBytes`, 100,000 iteraciones, sal aleatoria por
  usuario) en vez de un hash simple.
- **Rate limiting** con el limitador nativo de ASP.NET Core (ventana fija por IP) para proteger
  la API de abuso.
- **ProblemDetails consistente**: un middleware central traduce las excepciones de dominio
  (`NotFoundException`, `BusinessRuleException`, `ConcurrencyConflictException`,
  `ForbiddenException`) a respuestas `application/problem+json` con el status HTTP correcto.

## Estructura del repositorio

```
FleetDesk.sln
src/
  FleetDesk.Api/      Microservicio backend (ASP.NET Core Web API + EF Core)
  FleetDesk.Web/       Frontend Razor Pages
tests/
  FleetDesk.Api.Tests/ Pruebas xUnit (SQLite en memoria)
docker-compose.yml
.github/workflows/ci.yml
```

## Como correrlo

### Opcion 1: Docker Compose (API + Web + SQL Server)

```bash
docker compose up --build
```

- Frontend: http://localhost:8081
- API + Swagger: http://localhost:8080/swagger

La API aplica las migraciones y siembra datos de ejemplo (vehiculos, refacciones y usuarios)
automaticamente al arrancar.

Usuarios de prueba (ya sembrados):

| Usuario     | Contrasena      | Rol           |
|-------------|-----------------|---------------|
| `admin`     | `Admin123!`     | Admin         |
| `mecanico1` | `Mecanico123!`  | Mechanic      |
| `mecanico2` | `Mecanico123!`  | Mechanic      |
| `gerente`   | `Gerente123!`   | FleetManager  |

### Opcion 2: local con `dotnet`

Requiere SQL Server accesible (puede ser el de `docker compose up sqlserver`) y el SDK de
.NET 8.

```bash
# Backend
cd src/FleetDesk.Api
dotnet run

# Frontend (en otra terminal)
cd src/FleetDesk.Web
dotnet run
```

Ajusta `ConnectionStrings:DefaultConnection` en `src/FleetDesk.Api/appsettings.json` y
`Api:BaseUrl` en `src/FleetDesk.Web/appsettings.json` si usas otros puertos.

## Como probarlo

```bash
dotnet test
```

Las pruebas corren contra SQLite en memoria via `WebApplicationFactory`, sin necesidad de
Docker ni de SQL Server. Cubren, entre otras cosas:

- Autenticacion JWT (login valido/invalido, acceso sin token).
- Reglas de negocio de vehiculos (placas duplicadas, calculo de vencidos por km y por fecha).
- Concurrencia optimista al reabastecer/consumir refacciones (RowVersion desactualizado -> 409).
- Maquina de estados de ordenes de trabajo (transiciones invalidas, limite de ordenes abiertas
  por mecanico, paso automatico a "Esperando refacciones", cierre que actualiza el odometro).
- Autorizacion por rol y por propiedad del recurso (un mecanico no puede modificar la orden de
  otro).

## Stack

C# / .NET 8 - ASP.NET Core Web API - ASP.NET Core Razor Pages - Entity Framework Core -
SQL Server (SQLite en pruebas) - JWT Bearer - xUnit - Docker / Docker Compose - GitHub Actions.
