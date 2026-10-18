# Danelix

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
| `src/Modules/*` | Módulos del monolito (Identity, Tenancy, Catalog, Inventory…; se agregan por incremento) |
| `tests/Integration` | Pruebas de integración (xUnit + `WebApplicationFactory`) |
| `docs/` | [Roadmap](docs/ROADMAP.md), [ADRs](docs/DECISIONS.md) y documentación de arquitectura |
| `infra/compose` | Servicios locales de desarrollo (PostgreSQL) |

`tests/Unit`, `tests/Architecture` y `tests/E2E` se crearán cuando exista contenido para ellos.

## Documentación

- [docs/ROADMAP.md](docs/ROADMAP.md) — plan maestro por pasos; fuente de verdad del avance.
- [docs/DECISIONS.md](docs/DECISIONS.md) — índice de ADRs (decisiones de arquitectura).
- [AGENTS.md](AGENTS.md) — reglas de trabajo para asistentes de código.

## Requisitos

- .NET SDK 10
- Docker Desktop (PostgreSQL local)

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

```powershell
dotnet run --project src/Danelix.Api
```

- Salud: `https://localhost:7002/health`. El perfil HTTP (`http://localhost:5289`) redirige a HTTPS.

## Pruebas

```powershell
dotnet test Danelix.sln
```

## Seguridad

- El volcado del esquema de producción (`danelix-production-public-schema.sql`) **no se versiona** (ignorado por git), igual que los metadatos locales del CLI de Supabase.
- No ejecutar `supabase db push`, `db reset` ni migraciones contra producción desde este repositorio.
