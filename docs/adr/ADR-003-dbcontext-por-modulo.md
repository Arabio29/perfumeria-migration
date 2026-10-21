# ADR-003 — Persistencia con un DbContext por módulo

- **Estado:** Aceptada (con pendientes)
- **Fecha:** 2026-10-21
- **Decisor:** Eliasib (desarrollo), con asistencia de arquitectura

## Contexto

ADR-001 define un monolito modular con módulos separados y comunicación por `Contracts`. El Paso 03 empieza a modelar Identity y Tenancy y obliga a decidir dónde vive la persistencia:

- un único `DbContext` compartido por todos los módulos;
- un `DbContext` por módulo;
- un híbrido.

ADR-001 también afirma que operaciones como una venta y su movimiento de stock deben ejecutarse en una sola transacción. Esa garantía **no es automática** si cada módulo tiene su propio `DbContext`: cada contexto tiene su change tracker y su `SaveChanges`, y compartir una transacción exige coordinar conexión y `DbTransaction`.

## Decisión

1. **Un `DbContext` por módulo.** Los primeros serán `IdentityDbContext` y `TenancyDbContext`.
2. **Cada contexto es dueño de sus tablas y de sus migraciones.** Cada uno usa una tabla de historial propia (por ejemplo, `__ef_migrations_history_identity` y `__ef_migrations_history_tenancy`).
3. **Sin navegaciones ni FKs entre módulos en el modelo inicial.** Las referencias cross-módulo son identificadores opacos (`Guid`). `TenantMembership.UserId` referencia al usuario de Identity por ID; la existencia se valida en la capa de aplicación. Los usuarios se desactivan, no se borran físicamente.
4. **Dentro de un módulo, la integridad tenant se impone en la base de datos** con FKs compuestas e índices únicos. Por ejemplo, `BranchMembership` no puede apuntar a una sucursal de otro tenant.
5. **IDs `Guid` v7** generados por el dominio (`Guid.CreateVersion7`), para reducir fragmentación en índices PostgreSQL.
6. **Tests de integración con PostgreSQL real** (Testcontainers) a partir de la tarea 03.4. No se usará el proveedor InMemory para validar constraints.
7. **Transacciones cross-módulo: pendiente.** Antes del Paso 11 (venta + inventario) hay que definir un `TransactionCoordinator` que comparta conexión y transacción, o aceptar un `DbContext` compartido para el núcleo transaccional. Mientras no exista esa decisión, no se implementan casos de uso que escriban en dos módulos.

## Alternativas consideradas

1. **Un `DbContext` compartido.** Más simple para FKs y transacciones, pero concentra todo el esquema, diluye los límites y convierte el contexto en un “god object”. Se descarta por ahora.
2. **Híbrido: contexto compartido para Sales + Inventory y contextos separados para el resto.** Es una salida viable si el coordinador de transacciones resulta demasiado costoso; se evaluará antes del Paso 11.
3. **Contextos por módulo con FKs cross-módulo creadas por SQL explícito.** Da integridad referencial, pero acopla migraciones y orden de despliegue. Se descarta salvo que aparezca una necesidad fuerte.
4. **Un solo proyecto con namespaces en lugar de módulos separados.** Más simple, pero contradice ADR-001 y la decisión de aprender límites modulares.

## Consecuencias

**Positivas**

- Cada módulo evoluciona su esquema y sus migraciones sin tocar a los demás.
- Los límites de persistencia son explícitos y verificables.
- La separación facilita una futura extracción de módulo.

**Negativas / riesgos**

- Más infraestructura de migraciones y orden de ejecución.
- La integridad cross-módulo depende de la aplicación hasta que se decida lo contrario.
- Las operaciones que cruzan módulos necesitan un coordinador de transacción; sin él, la promesa de ACID de ADR-001 queda incompleta.
- Reporting necesitará read models o vistas propias, no consultas ad hoc a tablas de otros módulos.

## Decisiones pendientes

- Diseño y prueba del coordinador de transacciones cross-módulo.
- Política definitiva de FKs cross-módulo.
- RLS, roles de base de datos y least privilege (ADR posterior).
- Soft delete e índices únicos parciales.
- Orden de migraciones en despliegue y estrategia de rollback.
- Testcontainers vs servicio PostgreSQL en CI.

## Disparadores de revisión

- Antes del Paso 11 (venta + inventario).
- Cuando un caso de uso necesite escribir en dos módulos dentro de una transacción.
- Cuando se evalúe extraer un módulo a otro servicio o base de datos.
- Cuando se implemente RLS o un cambio de rol de base de datos.
