# Bolsa de Trabajo para Servidores Públicos — Desafío 3

Sistema web para publicar plazas de empleo público y gestionar el proceso de selección:
los **agentes de selección** publican y evalúan, los **candidatos** registran su hoja de vida y se postulan,
y el **administrador** gestiona usuarios y supervisa todos los procesos.

Tecnologías: ASP.NET Core 10 · arquitectura N-Capas · ASP.NET Core Identity + JWT · Entity Framework Core (migraciones) ·
Dapper · AutoMapper · Swagger · MVC con Bootstrap · xUnit · SQL Server.

---

## Estructura del proyecto

```
BolsaTrabajo.slnx
│
├── src/
│   ├── BolsaTrabajo.API/        1. Presentación — API REST (controladores, JWT, Swagger)
│   ├── BolsaTrabajo.Web/        1. Presentación — Interfaz MVC (consume la API)
│   ├── BolsaTrabajo.BLL/        2. Negocio      — Servicios con las reglas y validaciones, AutoMapper
│   ├── BolsaTrabajo.DAL/        3. Datos        — DbContext (EF Core), migraciones y repositorios con Dapper
│   ├── BolsaTrabajo.Entities/   4. Compartido   — Entidades, enumeraciones y modelos de consulta
│   └── BolsaTrabajo.DTOs/       4. Compartido   — Objetos de transferencia de datos (entrada y salida)
│
└── tests/
    └── BolsaTrabajo.Tests/      5. Pruebas      — 68 pruebas xUnit de reglas de negocio y base de datos
```

Recorrido de una petición: **MVC → API (Controller) → BLL (Service) → DAL (Repository con Dapper) → SQL Server**.

| Proyecto | Carpetas principales |
|---|---|
| **API** | `Controllers/` (Auth, Usuarios, Plazas, Postulaciones, HojasDeVida, Estado) · `Seguridad/` (JWT, errores, documentación de Swagger) |
| **Web** | `Controllers/` (por rol: Admin, Agente, Candidato, Cuenta, Plazas) · `Views/` · `Servicios/` (cliente de la API) · `Infraestructura/` (sesión, errores, textos) · `Models/` |
| **BLL** | `Servicios/` (Plaza, Postulación, HojaDeVida, Usuario, Estado) · `Mapping/` (AutoMapper) · `Excepciones/` · `Seguridad/` · `Archivos/` (almacenamiento de CV) |
| **DAL** | `Data/` (DbContext, datos iniciales, conexión) · `Repositorios/` (Dapper) · `Migrations/` · `Excepciones/` |
| **Entities** | Usuario, Plaza, Postulacion, HojaDeVida, Roles · `Enums/` · `Consultas/` (resultados de consultas Dapper) |
| **DTOs** | Una carpeta por recurso: `Auth/`, `Usuarios/`, `Plazas/`, `Postulaciones/`, `HojasDeVida/`, `Sistema/` |
| **Tests** | `Servicios/` · `Infraestructura/` · `Mapping/` · `Fakes/` (repositorios en memoria) |

**EF Core** se usa para Identity, migraciones y datos iniciales. **Dapper** se usa en todas las operaciones CRUD.

---

## Cómo ejecutarlo

**Requisitos:** .NET SDK 10, SQL Server y Visual Studio 2022/2026.

1. **Cadena de conexión:** en `src/BolsaTrabajo.API/appsettings.json`, cambie el servidor de
   `ConnectionStrings:DefaultConnection` por el de su equipo (por ejemplo `Server=localhost;` o `Server=localhost\\SQLEXPRESS;`).
2. **Arranque:** en Visual Studio, clic derecho en la solución → *Configurar proyectos de inicio* →
   *Varios proyectos de inicio* → **Iniciar** para `BolsaTrabajo.API` y `BolsaTrabajo.Web` → **F5**.
   Desde la terminal también puede usar:
   ```
   dotnet run --project src/BolsaTrabajo.API
   dotnet run --project src/BolsaTrabajo.Web
   ```
3. La API **crea la base `BolsaTrabajoDB` automáticamente** (aplica las migraciones) y carga datos de ejemplo.

| Aplicación | Dirección |
|---|---|
| Swagger (API) | https://localhost:7138/swagger |
| Sitio web (MVC) | https://localhost:7092 |

**Pruebas:** `dotnet test` o *Prueba → Explorador de pruebas → Ejecutar todas*.

---

## Usuarios de prueba

Se crean al iniciar la API. Sus contraseñas están en `src/BolsaTrabajo.API/appsettings.json` (secciones `AdminInicial` y `DatosDePrueba`).

| Rol | Correo |
|---|---|
| Administrador | admin@bolsatrabajo.gob |
| Agente de Selección | agente@bolsatrabajo.gob |
| Candidato | candidato@correo.com |

En Swagger: ejecute `POST /api/Auth/login`, copie el `token` y péguelo en **Authorize**.
