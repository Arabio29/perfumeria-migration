# ADR-004 — Autorización por ámbitos: plataforma, tenant y sucursal

- **Estado:** Aceptada
- **Fecha:** 2026-10-21
- **Decisor:** Eliasib (desarrollo), con asistencia de arquitectura
- **Relacionadas:** ADR-002 (multi-tenancy), ADR-003 (persistencia por módulo)

## Contexto

Danelix tiene dos tipos distintos de administración que no deben confundirse:

1. La administración de la plataforma SaaS: tenants, planes y soporte.
2. La administración de una perfumería cliente: usuarios, sucursales y operación del negocio.

Dentro de una perfumería hay además dos alcances: toda la organización y una o varias sucursales concretas. Un rol global llamado `SuperAdmin` o un `Staff` asociado únicamente al tenant no expresan estos límites y pueden conceder acceso excesivo.

## Terminología

- **Tenant:** cuenta/organización cliente que representa una perfumería o empresa y su límite de datos, configuración y facturación. No representa una persona ni una sucursal.
- **Branch:** sucursal física perteneciente a un único tenant.
- **User:** identidad global. Una persona conserva una identidad aunque pertenezca a varios tenants o sucursales.
- **Membership:** relación que concede un rol a un usuario dentro de un tenant o sucursal. La pertenencia no se deduce de un ID enviado por el cliente.

Si una empresa tiene varias tiendas, se modela como un tenant con varias sucursales. Si Mario administra empresas/cuentas independientes, tiene memberships independientes en cada tenant. Los reportes agrupados entre tenants requerirán una capacidad explícita futura; no se conceden por ser propietario de uno de ellos.

## Decisión

### 1. Identidad y contextos separados

- `User` es global y único por email normalizado.
- Los roles de plataforma (`PlatformAdmin`, `SupportOperator`) se almacenan y autorizan separadamente de las memberships de negocio.
- Una identidad puede tener un rol de plataforma y memberships de negocio, pero entrar a `/platform` no activa un tenant ni concede acceso implícito a sus datos.
- Los contextos y políticas de plataforma y tenant no se combinan en una jerarquía de roles única.

### 2. Catálogo inicial de roles

| Contexto | Rol | Alcance y responsabilidades |
| --- | --- | --- |
| Plataforma | `PlatformAdmin` | Gestiona cuentas globales de usuario (invitación, bloqueo y recuperación), creación/suspensión de tenants, planes y configuración de Danelix. No tiene acceso implícito a roles de negocio ni a ventas, inventario o datos operativos de tenants. |
| Plataforma | `SupportOperator` | Atiende soporte con permisos limitados. El acceso a datos del negocio requiere una concesión de soporte explícita según la sección 4. |
| Tenant | `TenantOwner` | Alcance de toda la organización: administra usuarios y sucursales, ve reportes globales y gestiona configuración/facturación. Puede nombrar administradores. |
| Tenant | `TenantAdmin` | Administra la operación y los usuarios en todas las sucursales. No transfiere la propiedad ni gestiona la facturación de la cuenta. |
| Tenant | `TenantMember` | Membresía base; por sí sola no concede acceso operativo a todas las sucursales. Requiere una o más memberships de sucursal. |
| Sucursal | `BranchManager` | Opera y ve reportes solo de las sucursales asignadas; puede administrar el personal de esas sucursales, sin conceder roles de tenant ni nombrar otros gerentes. |
| Sucursal | `BranchStaff` | Ejecuta las operaciones permitidas en las sucursales asignadas, según las policies del módulo. No administra usuarios. |

El nombre `SuperBoss` no se usará en el dominio: el caso descrito corresponde a `TenantOwner` o `TenantAdmin`, dependiendo de si la persona es propietaria de la cuenta o solo administra su operación.

### 3. Permisos y resolución de alcance

- Los roles son **conjuntos de permisos** definidos en el servidor. Al inicio el catálogo es fijo; no se implementan permisos arbitrarios en JSON ni roles personalizados por tenant.
- Cada operación debe verificar tanto el permiso de acción como el alcance del recurso: plataforma, tenant o sucursal. Por defecto se deniega.
- `TenantOwner` y `TenantAdmin` tienen alcance tenant-wide, incluyendo sucursales creadas posteriormente.
- `TenantMember` solo obtiene acceso operativo mediante memberships explícitas de sucursal.
- Un usuario asignado a varias sucursales obtiene la unión de permisos de esas sucursales, no acceso al resto del tenant.
- `TenantOwner` puede asignar/revocar `TenantAdmin` y transferir la propiedad. `TenantOwner` y `TenantAdmin` pueden administrar roles de sucursal en todo el tenant.
- `BranchManager` puede administrar `BranchStaff` solo dentro de sus sucursales asignadas y no puede elevarlos a `TenantOwner`, `TenantAdmin` o `BranchManager`.
- Ningún actor puede asignar un rol de mayor alcance que el permitido por sus propias policies.
- La selección de tenant/sucursal en una ruta o petición identifica el recurso; nunca prueba autorización. El servidor verifica membership, estado y permisos. Ningún `tenant_id`, `branch_id` o rol enviado por el cliente concede autoridad.

### 4. Operaciones de plataforma y soporte

- `PlatformAdmin` puede administrar el ciclo de vida global de las cuentas de usuario (sin ver ni establecer contraseñas), crear/suspender tenants y configurar el SaaS sin asumir un workspace de negocio. La creación del tenant debe invitar o asignar explícitamente su primer `TenantOwner`.
- La administración global de una identidad no permite modificar libremente las memberships ni los datos del negocio. Las memberships tenant las administran `TenantOwner`/`TenantAdmin`; una excepción de soporte usa la concesión temporal descrita abajo.
- `SupportOperator` no consulta datos de negocio solo por tener un ticket o un rol de soporte.
- El acceso excepcional a un tenant se implementará mediante una concesión temporal y explícita, limitada por tenant, alcance (tenant/sucursal), permisos y expiración, asociada a un motivo/ticket y auditada. Será de solo lectura por defecto; cualquier permiso de escritura requerirá autorización adicional. La política de aprobación/consentimiento se definirá antes de implementar el flujo.

### 5. Integridad y administración de memberships

- Un usuario puede pertenecer a varios tenants y a varias sucursales del mismo tenant usando el mismo `User` global.
- `TenantMembership` contiene el rol general del usuario; `BranchMembership` contiene el rol y estado en la sucursal.
- La base de datos impone unicidad de membership y claves foráneas compuestas que impiden asociar una membership a una sucursal o usuario de otro tenant.
- Cada tenant debe conservar al menos un `TenantOwner` activo. La transferencia o revocación del último propietario se realiza de forma transaccional y auditable.
- Administrar una membership no equivale a borrar la identidad global del usuario. La baja de un empleado se limita al tenant o sucursal correspondiente.

### 6. Evolución

No se crea ahora una jerarquía `TenantGroup`/holding ni permisos personalizables. Si se requiere un panel consolidado de empresas independientes, se diseñará una relación explícita y permisos de lectura definidos, sin relajar el aislamiento tenant.

## Alternativas consideradas

1. **Un único rol global `SuperAdmin` para todas las operaciones.** Rechazado: mezcla la plataforma con los negocios y hace fácil conceder acceso operativo implícito.
2. **Un tenant por sucursal.** Rechazado para sucursales de la misma empresa: duplica usuarios/configuración y vuelve difícil consolidar estadísticas. Sí se usan tenants separados para cuentas/empresas independientes.
3. **Un único `TenantRole` con `Owner`, `Admin`, `Staff` para todo.** Rechazado: no expresa si el permiso cubre toda la organización o una sucursal.
4. **RBAC personalizable desde el primer día.** Rechazado: agrega UI, persistencia y validación de combinaciones antes de que exista una necesidad demostrada. Se inicia con roles fijos mapeados a policies.
5. **Dar acceso de datos a soporte por pertenecer a la plataforma.** Rechazado: el acceso debe ser limitado, temporal, justificado y auditable.

## Consecuencias

**Positivas**

- El alcance de propietario, administrador general, gerente de sucursal y empleado queda explícito.
- Un gerente puede estar asignado a varias sucursales sin duplicar la identidad.
- Se limita la escalada de privilegios y se separa el soporte de la operación del cliente.
- El modelo compartido por tenant puede crecer sin crear una base o esquema por sucursal.

**Negativas / riesgos**

- Cada endpoint y consulta debe resolver el alcance correcto; un filtro solo por tenant no basta para roles de sucursal.
- Un usuario con muchas memberships requiere un selector de workspace/sucursal y una experiencia de administración clara.
- El catálogo fijo de roles deberá revisarse si aparecen permisos por módulo o excepciones comerciales reales.
- La invariante de al menos un propietario necesita concurrencia/transacción y pruebas, no solo validación de UI.

## Plan de implementación

- **Paso 03:** alinear el dominio y el esquema de memberships con esta decisión antes de generar migraciones.
- **Paso 04:** separar el contexto de plataforma y `PlatformRole` del workspace tenant.
- **Paso 06:** implementar policies, alcance tenant/sucursal y tests de aislamiento y escalada de privilegios.
- **Paso 18:** construir concesiones temporales de soporte, auditoría y aprobación según la política definida.
- **Pasos 19–20:** selector de workspace/sucursal y recorridos E2E para roles y límites.

## Relación con decisiones anteriores

ADR-004 refina y reemplaza únicamente la taxonomía ambigua de roles tenant/sucursal descrita en ADR-002. No cambia la decisión de base compartida, tenant derivado en servidor, separación del contexto de plataforma ni las defensas de aislamiento de ADR-002.

## Disparadores de revisión

- Antes de exponer endpoints de administración de usuarios o sucursales.
- Cuando se soliciten roles personalizados o reportes consolidados entre tenants.
- Antes de permitir acceso de soporte con escritura o sin aprobación del cliente.
- Cuando un cambio de rol deba revocarse inmediatamente en sesiones activas.
