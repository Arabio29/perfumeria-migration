# Arquitectura objetivo

Este documento resume el diseño objetivo; no afirma que todas sus partes estén implementadas. Las decisiones normativas están en los [ADRs](DECISIONS.md), especialmente [ADR-002](adr/ADR-002-multi-tenant.md), [ADR-003](adr/ADR-003-dbcontext-por-modulo.md) y [ADR-004](adr/ADR-004-autorizacion-por-ambitos.md). El [roadmap](ROADMAP.md) indica cuándo construir cada parte.

## Organización y límites de datos

```text
Danelix (plataforma SaaS)
├── PlatformAdmin / SupportOperator
└── Tenant (empresa/cuenta de perfumería)
    ├── Branch (sucursal)
    ├── Branch
    └── Users y memberships
```

- `Tenant` representa una cuenta/organización cliente y su límite de datos, configuración y facturación; no representa una sucursal.
- `Branch` representa una ubicación perteneciente a un solo tenant.
- Empresas/cuentas independientes son tenants distintos, aunque una persona pertenezca a ambas. Un reporte agregado entre ellas requiere una capacidad explícita futura.
- `User` es una identidad global. Sus memberships expresan el acceso a tenants y sucursales; desactivar una membership no borra al usuario global.

## Contextos de autorización

| Contexto | Roles | Alcance |
| --- | --- | --- |
| Plataforma (`/platform`) | `PlatformAdmin`, `SupportOperator` | Configuración y operación de Danelix, incluido el ciclo de vida global de usuarios. Sin acceso implícito a memberships o datos operativos de un tenant. |
| Tenant (`/app/{tenantSlug}`) | `TenantOwner`, `TenantAdmin`, `TenantMember` | `TenantOwner` y `TenantAdmin` abarcan todas las sucursales; `TenantMember` necesita memberships explícitas de sucursal. |
| Sucursal | `BranchManager`, `BranchStaff` | Solo las sucursales asignadas al usuario. Un usuario puede tener varias memberships de sucursal. |

Los roles de plataforma no se mezclan con roles tenant. Una identidad que tenga ambos debe usar el contexto correspondiente; el rol de plataforma no le concede autoridad dentro de `/app` sin una membership de negocio válida.

## Evaluación de una petición

1. Autenticar la identidad global.
2. Resolver el contexto solicitado (plataforma o tenant); el slug/ID de la ruta identifica el recurso, no concede acceso.
3. En contexto tenant, comprobar tenant activo y membership activa; si el recurso es de sucursal, comprobar que la sucursal pertenece al tenant y está dentro del alcance del usuario.
4. Comprobar en el servidor el permiso para la acción sobre el recurso. Denegar por defecto.
5. Aplicar el mismo alcance en la consulta y en los comandos; no depender de guards de Angular ni de IDs/roles enviados por el cliente.

Los roles iniciales son fijos y se mapean a policies del backend. No se admiten permisos configurables por el cliente ni roles personalizados hasta que exista una necesidad validada.

## Responsabilidades generales

- `PlatformAdmin`: administra cuentas globales de usuario (invitación, bloqueo y recuperación), crea/suspende tenants y administra planes/configuración SaaS. No gestiona memberships de negocio ni lee ventas, inventario o finanzas por defecto.
- `SupportOperator`: atiende tickets. El acceso excepcional a datos requiere concesión explícita, temporal, acotada, justificada y auditada; solo lectura por defecto. El flujo detallado se implementa en el Paso 18.
- `TenantOwner`: administra la cuenta de negocio, todas las sucursales, usuarios y reportes; gestiona facturación y propiedad.
- `TenantAdmin`: administra la operación y usuarios en todas las sucursales, pero no transfiere propiedad ni modifica facturación.
- `TenantOwner` puede asignar/revocar `TenantAdmin`; Owner y Admin gestionan roles de sucursal. Nadie concede un rol de mayor alcance que el permitido por su policy.
- `BranchManager`: administra la operación y `BranchStaff` de sus sucursales asignadas; no concede roles de tenant, otros gerentes ni acceso a otras sucursales.
- `BranchStaff`: ejecuta únicamente las acciones de negocio autorizadas en sus sucursales.

El catálogo exacto de permisos por módulo (ventas, inventario, catálogo, reportes, etc.) se define al implementar el Paso 06. Tener alcance tenant-wide no significa que toda acción financiera o sensible sea irrestricta.

## Aislamiento y escalabilidad

- La primera versión usa PostgreSQL con esquema compartido y tablas de negocio tenant-scoped.
- Toda consulta de negocio se filtra por tenant; las consultas de sucursal se limitan además a las sucursales autorizadas.
- Las FKs compuestas impiden relaciones entre tenants o sucursales cruzadas.
- Los reportes tenant-wide pueden agregar todas las sucursales del tenant; los reportes de `BranchManager` solo agregan sus memberships explícitas.
- Se empieza con una base compartida e índices por tenant/sucursal. Partición, base dedicada o `TenantGroup` se consideran solo con requisitos o métricas que lo justifiquen.

## Estado actual

ADR-004 fija el modelo objetivo. El dominio creado en el Paso 03.1 aún debe alinearse: `TenantRole` actualmente contiene `Staff` y `BranchMembership` todavía no tiene rol/estado. Esa actualización está planificada antes de EF Core y las migraciones, para evitar versionar un esquema con alcances incorrectos.
