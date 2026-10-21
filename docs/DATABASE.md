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

## Pendientes antes de crear las migraciones

1. Alinear el dominio del Paso 03.1 con `TenantMember`, `BranchRole` y el estado de `BranchMembership`.
2. Decidir el ciclo de vida de una membership revocada y si una reinvitación reactiva la fila o crea un nuevo periodo histórico. Evitar duplicados activos y conservar auditoría.
3. Decidir cómo serializar roles/estados en PostgreSQL (por ejemplo, valores string con validación o códigos estables) y cómo se manejan cambios de enum en migraciones.
4. Mapear FKs e índices mediante configuraciones EF Core y verificar con PostgreSQL real/Testcontainers.
5. Probar que una FK cruzada tenant/sucursal falla y que constraints únicos devuelven el error esperado.

Todos los timestamps se almacenan en UTC (`timestamptz`). El valor de un ID o una ruta enviado por el cliente nunca sustituye la autorización del servidor. RLS y roles de base de datos se especificarán por separado como defensa en profundidad.
