# Danelix

[![CI](https://github.com/Arabio29/perfumeria-migration/actions/workflows/ci.yml/badge.svg)](https://github.com/Arabio29/perfumeria-migration/actions/workflows/ci.yml)

SaaS multi-tenant para perfumerías. Este repositorio reconstruye la plataforma actual (React/Next.js + Supabase) hacia un monolito modular en ASP.NET Core, siguiendo el [roadmap del proyecto](docs/ROADMAP.md).

- **Backend:** ASP.NET Core 10 + EF Core (monolito modular)
- **Frontend:** Angular (Paso 19 del roadmap)
- **Base de datos:** PostgreSQL (Docker Compose en desarrollo)
- **Infraestructura:** Docker → VPS Linux → K3s (laboratorio, fases posteriores)

> Regla de oro: la operación actual (Supabase en producción) no se toca. Este repositorio no se conecta a la base de datos de producción.

## Estructura del repositorio

| Ruta | Contenido |
| --- | --- |
| `src/Danelix.Api` | Host de la API (middleware, composición, `Program.cs`) |
| `src/SharedKernel` | Primitivas de dominio compartidas (`DomainException`) |
| `src/Modules/*` | Módulos del monolito (Identity, Tenancy, Catalog, Inventory…; se agregan por incremento) |
| `tests/Unit` | Pruebas unitarias de dominio (xUnit) |
| `tests/Integration` | Pruebas de integración (xUnit + `WebApplicationFactory`) |
| `docs/` | [Roadmap](docs/ROADMAP.md), [arquitectura](docs/ARCHITECTURE.md), [modelo de datos](docs/DATABASE.md) y [ADRs](docs/DECISIONS.md) |
| `infra/compose` | Servicios locales de desarrollo (PostgreSQL) |

`tests/Architecture` y `tests/E2E` se crearán cuando exista contenido para ellos.

## Documentación

- [docs/ROADMAP.md](docs/ROADMAP.md) — plan maestro por pasos; fuente de verdad del avance.
- [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md) — límites de plataforma, tenant y sucursal.
- [docs/DATABASE.md](docs/DATABASE.md) — modelo de datos objetivo; no implementado completo.
- [docs/DECISIONS.md](docs/DECISIONS.md) — índice de ADRs (decisiones de arquitectura).
- [docs/LEARNING.md](docs/LEARNING.md) — bitácora de aprendizaje y ejercicios.
- [AGENTS.md](AGENTS.md) — reglas de trabajo para asistentes de código.

## Requisitos

- .NET SDK 10
- Docker Desktop (PostgreSQL local)
- Herramientas locales del repositorio: `dotnet tool restore` (EF Core CLI)

## Base de datos (desarrollo)

PostgreSQL 17 con pgvector, definido en `infra/compose/docker-compose.yml`. Las credenciales son triviales y **solo** para desarrollo local.

```powershell
docker compose -f infra/compose/docker-compose.yml up -d
docker compose -f infra/compose/docker-compose.yml ps   # debe mostrar (healthy)
```

- Conexión desde el host: `Host=localhost;Port=5432;Database=danelix;Username=danelix;Password=danelix_dev`
- Consola SQL: `docker exec -it danelix-postgres psql -U danelix -d danelix`
- Detener sin perder datos: `docker compose -f infra/compose/docker-compose.yml stop`
- ⚠️ `down -v` elimina el volumen y con él los datos locales.

## Ejecutar la API

En `Development`, el arranque aplica las migraciones pendientes de Identity/Tenancy y carga datos de ejemplo idempotentes. Requiere PostgreSQL local saludable. Esto no se ejecuta en `Testing` ni en producción.

```powershell
dotnet run --project src/Danelix.Api
```

- Salud: `https://localhost:7002/health`. El perfil HTTP (`http://localhost:5289`) redirige a HTTPS.

## Pruebas

Las pruebas de integración usan Testcontainers para levantar un PostgreSQL desechable; Docker Desktop debe estar ejecutándose.

```powershell
dotnet test Danelix.sln
```

## Seguridad

- El volcado del esquema de producción (`danelix-production-public-schema.sql`) **no se versiona** (ignorado por git), igual que los metadatos locales del CLI de Supabase.
- No ejecutar `supabase db push`, `db reset` ni migraciones contra producción desde este repositorio.
