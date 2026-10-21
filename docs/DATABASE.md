# Modelo de datos objetivo

Este documento describe el esquema objetivo y **no implica que las tablas o constraints ya existan**. La decisión de autorización está en [ADR-004](adr/ADR-004-autorizacion-por-ambitos.md); la persistencia por módulo está en [ADR-003](adr/ADR-003-dbcontext-por-modulo.md). No usar ni aplicar este modelo a la base de datos de producción de Supabase.

## Tablas iniciales de Identity y Tenancy

| Tabla | Alcance | Datos/relaciones principales |
| --- | --- | --- |
| `users` | Global | Identidad, email normalizado único, nombre, estado y timestamps. No contiene roles de tenant. |
| `platform_roles` | Global/plataforma | Roles `PlatformAdmin`/`SupportOperator`, separados de memberships de negocio. |
| `tenants` | Cuenta de negocio | Nombre, slug único si se usa como ruta global, estado y timestamps. |
| `branches` | Tenant + sucursal | `tenant_id`, nombre, código, estado; una sucursal pertenece a un tenant. |
| `tenant_memberships` | Tenant + usuario | Una relación por usuario/tenant; rol `TenantOwner`, `TenantAdmin` o `TenantMember`; estado. `user_id` es referencia opaca a Identity conforme a ADR-003. |
| `branch_memberships` | Tenant + sucursal + usuario | Rol `BranchManager` o `BranchStaff`, estado y timestamps. Un usuario puede tener varias memberships. |

La identidad de una persona es única y puede tener memberships en varios tenants. Una empresa con varias sucursales usa un solo tenant; no se duplica el usuario ni se crea un tenant por sucursal.

## Constraints e índices requeridos

- `users`: `UNIQUE(normalized_email)`.
- `tenants`: `UNIQUE(slug)` cuando el slug identifica al tenant globalmente en la ruta.
- `branches`: `UNIQUE(tenant_id, code)` y `UNIQUE(tenant_id, id)` para permitir FKs compuestas.
- `tenant_memberships`: `UNIQUE(tenant_id, user_id)`; agregar índice de acceso por `user_id`.
- `branch_memberships`: `UNIQUE(tenant_id, branch_id, user_id)`.
- `branch_memberships(tenant_id, branch_id)` referencia `branches(tenant_id, id)`.
- `branch_memberships(tenant_id, user_id)` referencia `tenant_memberships(tenant_id, user_id)`, para que la persona pertenezca al mismo tenant.
- Las tablas de negocio tienen `tenant_id NOT NULL`; las tablas por sucursal incluyen `branch_id` y FK compuesta `(tenant_id, branch_id)`.
- No confiar en FKs simples por `id` cuando permitan combinar un `tenant_id` de una organización con una sucursal de otra.

Por ADR-003, `tenant_memberships.user_id` no tiene inicialmente una FK a `users` porque Identity y Tenancy tienen `DbContext` separados. La existencia/estado del usuario se verifica desde la aplicación; los usuarios no se eliminan físicamente.

## Invariantes de autorización que el esquema debe soportar

- `TenantOwner` y `TenantAdmin` tienen alcance de todas las sucursales, incluidas las creadas después de asignar el rol.
- `TenantMember` no tiene acceso operativo a todas las sucursales; requiere `branch_memberships` activas.
- `BranchManager` y `BranchStaff` quedan acotados a sus memberships de sucursal.
- Un usuario puede tener roles distintos en sucursales distintas; por ejemplo, gerente en una y empleado en otra.
- Cada tenant conserva al menos un `TenantOwner` activo. La baja/transferencia se valida dentro de una transacción y se registra en auditoría; un índice único por sí solo no garantiza esta regla.
- Desactivar una membership no elimina la identidad global ni memberships de otros tenants.
- Revocar una membership conserva la fila; una reinvitación reactiva esa misma fila. No usar índices únicos parciales para liberar la clave. La auditoría futura conservará eventos de revocación/reinvitación.

## Estado de persistencia

- **Implementado:** `IdentityDbContext` mapea `users`; `InitialIdentity` crea email normalizado único, estado con constraint y `timestamptz`. `TenancyDbContext` mapea tenants, sucursales y memberships.
- **Seeds:** API en `Development` aplica las migraciones y carga usuarios/tenants/sucursales/memberships de ejemplo. No crea contraseñas y no se ejecuta en `Testing` ni producción.
- **Versiones:** EF Core/Design/Relational `10.0.13`, proveedor Npgsql EF `10.0.3` y herramienta local `dotnet-ef` `10.0.13` fijada en `dotnet-tools.json`.
- **Historial:** Identity usa `__ef_migrations_history_identity`; Tenancy usa `__ef_migrations_history_tenancy`.
- **Verificación automatizada:** `DevelopmentDatabaseTests` usa Testcontainers para migrar ambos contextos dos veces, ejecutar el seed dos veces, verificar constraints únicas, rechazar FKs cruzadas y probar concurrentemente el último TenantOwner.
- **Enums:** se persisten como strings legibles con constraints para valores cerrados. Renombrar un valor persistido requiere una migración de datos/esquema.

## Invariante de propietario

EF Core serializa cambios de rol/estado por tenant con una transacción y `SELECT ... FOR UPDATE` sobre la fila del tenant. Luego cuenta otros propietarios activos antes de modificar la membership. La prueba Testcontainers intenta demover a dos propietarios simultáneamente y verifica que uno quede activo.

## Comandos EF Core (Identity)

Desde la raíz del repositorio, restaurar la herramienta local una vez:

```powershell
dotnet tool restore
```

La fábrica de diseño requiere `DANELIX_IDENTITY_CONNECTION_STRING`. Usar solo la conexión local de desarrollo o una base de test desechable; nunca una conexión de producción.

```powershell
$env:DANELIX_IDENTITY_CONNECTION_STRING = "Host=localhost;Port=5432;Database=danelix;Username=danelix;Password=danelix_dev"
dotnet ef migrations list --project src/Modules/Identity --startup-project src/Modules/Identity --context IdentityDbContext
dotnet ef database update --project src/Modules/Identity --startup-project src/Modules/Identity --context IdentityDbContext
Remove-Item Env:DANELIX_IDENTITY_CONNECTION_STRING

$env:DANELIX_TENANCY_CONNECTION_STRING = "Host=localhost;Port=5432;Database=danelix;Username=danelix;Password=danelix_dev"
dotnet ef migrations list --project src/Modules/Tenancy --startup-project src/Modules/Tenancy --context TenancyDbContext
dotnet ef database update --project src/Modules/Tenancy --startup-project src/Modules/Tenancy --context TenancyDbContext
Remove-Item Env:DANELIX_TENANCY_CONNECTION_STRING
```

Todos los timestamps se almacenan en UTC (`timestamptz`). El valor de un ID o una ruta enviado por el cliente nunca sustituye la autorización del servidor. RLS y roles de base de datos se especificarán por separado como defensa en profundidad.
