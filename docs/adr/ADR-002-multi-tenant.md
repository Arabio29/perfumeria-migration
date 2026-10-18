# ADR-002 — Multi-tenancy: base de datos única con aislamiento derivado del servidor

- **Estado:** Aceptada
- **Fecha:** 2026-10-17
- **Decisor:** Eliasib (desarrollo), con asistencia de arquitectura

## Contexto

Cada perfumería cliente es un **tenant** con sus sucursales, usuarios, catálogo, inventario y cifras. La aplicación actual dejó problemas estructurales conocidos:

- `super_admin` convive con las membresías de negocio (`user_tenants`) → confusión entre administrador de plataforma y usuario de un negocio.
- La auditoría del esquema de producción encontró políticas RLS permisivas (`USING (true) WITH CHECK (true)`) para `anon`/`authenticated` en varias tablas y `GRANT ALL` sobre tablas sensibles → el aislamiento efectivo no está garantizado en el sistema viejo.
- Varias operaciones confían en identificadores (`tenant_id`, `sucursal_id`) que llegan desde el cliente.

El mercado objetivo son pymes; el volumen de datos por tenant es moderado y decenas o cientos de tenants deben convivir con coste operativo mínimo.

## Decisión

1. **Una sola base PostgreSQL, esquema compartido.** Cada tabla de negocio incluye `tenant_id NOT NULL`; las tablas por sucursal incluyen además `branch_id`.
2. **El tenant de cada petición se deriva en el servidor** de la sesión autenticada y la membresía verificada (`tenant_memberships`). Un `tenant_id` que llegue en body/query/header es dato **no confiable** y no concede autoridad.
3. **Autorización en tres contextos separados:**
   - Plataforma (`PlatformAdmin`, `SupportOperator`): sin tenant activo, endpoints bajo `/platform`, sin acceso implícito a datos de negocio.
   - Tenant (`TenantOwner`, `Admin`, `Staff`): membresía y permisos verificados en servidor.
   - Sucursal: `branch_memberships` / permisos por sucursal cuando el rol lo exija; validar que la sucursal pertenece al tenant derivado.
4. **Integridad scoped por tenant en la base de datos:** `UNIQUE (tenant_id, ...)` (SKU, membresías, claves de idempotencia) y `UNIQUE (tenant_id, id)` para FK compuestas, de modo que una referencia cruzada entre tenants **falle a nivel de base de datos**.
5. **RLS como defensa en profundidad, no como única defensa.** Se evaluará e implementará por fases, estableciendo contexto siempre desde el servidor; nunca se confiará en RLS para compensar consultas sin filtrar.
6. **Suite cross-tenant obligatoria** (tenant A intenta leer/escribir datos de tenant B y debe fallar) como gate de merge desde el Paso 06 del roadmap.

## Alternativas consideradas

1. **Base de datos por tenant.** Aislamiento máximo, pero: N migraciones, N backups/restores, N cadenas de conexión y coste operativo por cliente; útil solo para clientes enterprise. Descartada para el mercado actual.
2. **Esquema por tenant** (`tenant_1.sales`, …). Coste operativo similar; EF Core, migraciones y consultas de plataforma se complican; el soporte transversal se vuelve difícil.
3. **Esquema compartido con RLS como única barrera.** Arriesgada: un `SET` olvidado o una conexión pooled contaminada abre una fuga silenciosa; la aplicación debe filtrar siempre.
4. **Esquema compartido con autorización en aplicación + RLS planificada** — elegida.

## Consecuencias

**Positivas**

- Un solo esquema, una migración, un backup; EF Core y las consultas de plataforma funcionan de forma natural.
- Coste marginal por tenant casi nulo: apto para muchos tenants pequeños.

**Negativas / riesgos**

- El aislamiento depende de la disciplina de la aplicación: un `WHERE tenant_id = ...` olvidado es una fuga. Mitigaciones: consultas base filtradas por contexto, tests cross-tenant obligatorios, revisión de seguridad y RLS posterior.
- Índices compuestos que comienzan por `tenant_id` (mayor coste de índices); aceptable por el tamaño de los tenants.
- Un tenant gigante convive en la misma base; si algún día lo exige, el diseño por módulos permite extraerlo a su propia base.

## Reglas derivadas

- Nunca aceptar `tenant_id`/`branch_id` del cliente como autorización; derivarlos del servidor.
- El PlatformAdmin no obtiene acceso implícito a ventas, stock ni cuentas; las operaciones de soporte requieren autorización explícita, justificación y auditoría.
- Toda tabla nueva de negocio nace con `tenant_id` (y `branch_id` si aplica) e índices que empiezan por esas columnas.
