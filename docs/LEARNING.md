# Bitácora de aprendizaje

Registro de conceptos, decisiones y ejercicios de cada incremento. No sustituye al roadmap ni a los ADR: sirve para consolidar lo aprendido.

## 2026-10-21 — Paso 03.1: dominio de Identity y Tenancy

### Qué se construyó

- Proyecto `Danelix.SharedKernel` con `DomainException`.
- Módulo `Identity` con `User`, `Email` y `UserStatus`.
- Módulo `Tenancy` con `Tenant`, `Branch`, `TenantMembership`, `BranchMembership`, `TenantSlug` y enums de estado/rol.
- Proyecto `tests/Unit/Danelix.Domain.Tests` con pruebas de invariantes.
- ADR-003: persistencia con un `DbContext` por módulo.

### Conceptos practicados

- **Value object:** `Email` y `TenantSlug` encapsulan normalización y validación; no son strings sueltos.
- **Encapsulación del dominio:** constructores privados y factories `Create`; el estado solo cambia por métodos con intención (`Suspend`, `Revoke`, etc.).
- **`internal` por defecto:** el dominio no es API pública del módulo. Los tests acceden mediante `InternalsVisibleTo`.
- **Identificadores:** `Guid.CreateVersion7()` para reducir fragmentación en índices PostgreSQL.
- **Tiempo:** `DateTimeOffset` normalizado a UTC y validado contra `default`.
- **Invariantes locales vs. globales:** el dominio valida formato y estado; la unicidad de email, slug, código de sucursal y membresía se impone en la base de datos.
- **Excepciones de dominio:** `DomainException` comunica una violación de regla de negocio, no un error técnico.
- **EF Core inicial:** `Email` se convierte a una columna normalizada; enums se guardan como strings con constraints. Npgsql y EF Core deben usar versiones compatibles; fijar `Relational` evita que NuGet seleccione una dependencia transitiva más antigua.

### Decisiones y deudas

- ADR-004 fija roles distintos por alcance: `TenantOwner`, `TenantAdmin`, `TenantMember`, `BranchManager` y `BranchStaff`; `PlatformAdmin` y `SupportOperator` son roles globales separados.
- Los roles iniciales son fijos y se mapean a permisos/policies en servidor. Los permisos granulares por módulo se implementan en el Paso 06; los roles personalizables no se construirán sin necesidad validada.
- El dominio tenant/sucursal ya está alineado con ADR-004: `TenantRole` es `Owner`/`Admin`/`Member`, y `BranchMembership` tiene `BranchRole` (`Manager`/`Staff`) y estado. `PlatformRole` y `/platform` corresponden al Paso 04; policies ejecutables al Paso 06.
- `TenantOwner`/`TenantAdmin` tienen alcance de todas las sucursales; los usuarios `TenantMember` requieren memberships explícitas de sucursal.
- No hay FK entre módulos. `TenantMembership.UserId` es un identificador opaco; la existencia del usuario se validará en aplicación.
- La transacción cross-módulo sigue pendiente. ADR-003 la marca como decisión obligatoria antes del Paso 11.
- Identity y Tenancy tienen `DbContext` y migraciones separadas; seeds solo en `Development`. Testcontainers verifica idempotencia, constraints tenant/sucursal y la protección concurrente del último `TenantOwner` mediante bloqueo de fila.

### Ejercicio

Implementa `TenantMembership.ChangeRole(TenantRole newRole)`:

1. Valida que el rol sea un valor definido del enum.
2. Agrega pruebas para un cambio válido y para un valor inválido.
3. Responde: si el rol cambia, ¿la sesión activa del usuario debería reflejarlo inmediatamente? ¿Cómo lo garantizarías sin consultar la base de datos en cada request?

### Preguntas abiertas

- La membership revocada conserva la fila y la reinvitación la reactiva; la auditoría debe registrar ambas transiciones cuando se implemente el módulo de auditoría.
- ¿Qué tabla o mecanismo usará el coordinador de transacciones cross-módulo?
- ¿Cómo se validará la existencia del usuario al crear una membresía?
- ¿Cuándo se introduce RLS y con qué rol de base de datos?
