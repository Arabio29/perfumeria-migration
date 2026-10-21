## DANELIX

Plan maestro de reconstrucción del SaaS de perfumerías Arquitectura, datos, API, Angular, calidad, pagos, IA y despliegue

Edición: octubre 2026 | Enfoque: aprender construyendo | Origen: esquema SQL y descripción funcional proporcionados por el propietario

**Estado de avance (oct 2026):** Paso 02 cerrado — solución .NET 10 con health checks y tests de integración, monorepo con calidad básica (editorconfig/analizadores), PostgreSQL 17 + pgvector en Docker Compose, ADR-001 a ADR-004 y CI en GitHub Actions. Paso 03 en curso: el dominio inicial Identity/Tenancy y tests unitarios existen; falta alinear roles y alcance según ADR-004 antes de mapear EF Core o crear migraciones. Siguiente: ajustar dominio/tests y luego persistencia.

REGLA CENTRAL: conservar la operación actual mientras se construye y verifica una plataforma nueva en paralelo. No ejecutar el esquema recibido directamente: se facilitó solo como contexto.

## 0. Cómo usar este manual

- Ejecutar los pasos en orden. Cada uno tiene un entregable observable y una prueba para demostrar comprensión.

- Antes de pedir código a un agente: escribir reglas de negocio, un pequeño diagrama, criterios de aceptación y ejemplos de errores.

- Después de recibir ayuda de IA: explicar el cambio, reescribir una parte sin asistencia, ejecutar tests y documentar el motivo en un ADR.

- Mantener issues pequeñas: una historia por PR, tests obligatorios, revisión de seguridad multi-tenant y demo local.

## 1. Diagnóstico del esquema y decisiones de arquitectura

La base existente cubre identidad, empresas, sucursales, catálogo, stock, ventas, gastos, pedidos, fiados, proveedores, seguridad, planes, soporte y auditoría. No conviene hacer una migración tabla-a-tabla: hay modelos que deben evolucionar.

| Situación observada | Rediseño propuesto |
| --- | --- |
| super_admin en user_tenants / sesión | Separar el rol global PlatformAdmin de las memberships de negocio; |
| atado a perfumería | panel /platform sin tenant activo. |
| ventas mezcla línea, cobro y | Sale + SaleItem + SalePayment + CashMovement; ingreso manual no es |
| entrada_dinero | una venta. |
| esencias tiene bala/bodega; envases | StockBalance + InventoryMovement por ubicación/producto; toda |
| stock mutable | mutación vía caso de uso transaccional. |
| cuentas_cobro usa pagado boolean | Receivable + ReceivablePayment; saldo derivado; distinguir cuentas |
|   | independientes de ventas a crédito. |
| cuentas_proveedor/pagos_proveedor | FK (tenant_id, proveedor_id) hacia UNIQUE (tenant_id,id) y equivalente |
| tienen FK compuesta mal expresada | para cuenta; validar con migraciones. |


| Situación observada | Rediseño propuesto |
| --- | --- |
| permisos_usuario JSONB y roles dispares | Policies por rol/permisos explícitos: plataforma vs tenant vs sucursal; |
|   | back-end autoritativo. |
| estadísticas leen 1000 filas y calculan en | Queries agregadas en backend con índices, zona horaria y definición |
| cliente | contable clara. |
| planes / sucursales duplican status de | BillingAccount, Subscription, Invoice/Payment, entitlements por tenant; |
| suscripción | decidir política comercial de sucursales. |
| backup ZIP CSV en interfaz | Exportación funcional por tenant vs backup operacional cifrado, externo |
|   | y restaurable. |
| sessions JWT 8h pero cookie 7 días | Separar access-token corto / sesión refresh con rotación y revocación, o |
|   | cookie BFF validada en servidor. |

Riesgos a verificar del SQL aportado: campos cantidad_disponible/inventario_cantidad con DEFAULT no son columnas generadas y pueden desincronizarse; claves foráneas compuestas de proveedores/pagos parecen partidas en dos constraints con el mismo nombre; sucursal_id puede corresponder a otro tenant si solo hay FK por id; faltan algunas unicidades e índices de negocio (membresía, SKU activo, pendientes, idempotencia); credenciales TOTP requieren cifrado en reposo. Verificar el esquema físico real antes de afirmar cuáles ya están resueltos por migraciones, RLS o triggers.

## 2. Arquitectura objetivo

Angular SPA --> HTTPS / Caddy --> ASP.NET Core API / BFF

- |--> Identity + Authorization

- |--> Catalog / Inventory / Sales / Credit

- |--> Purchasing / Finance / Reports

- |--> Billing / Support / Notifications / AI

- |--> PostgreSQL + pgvector

- |--> Background Worker + Outbox

GitHub Actions --> Docker Compose (VPS) --> K3s (laboratorio posterior)

Patrón: monolito modular con límites claros por módulo y separación Domain / Application / Infrastructure / API. No inventar microservicios ni repositorios genéricos para cada tabla. EF Core para cambios de estado; Dapper/SQL para consultas especiales si existe una ventaja demostrable. Integración entre módulos por contratos internos y eventos de dominio; outbox para publicar efectos posteriores al commit.

Repositorio: /apps/web-angular, /src/Danelix.Api, /src/Modules/{Identity,Tenancy,Catalog,Inventory,Sales,Rec eivables,Procurement,Finance,Reporting,Billing,Platform,Support,Notifications,AI}, /tests/{Unit,Integration,Architecture,E2E}, /infra/{compose,k3s,monitoring}, /docs/{adr,api,runbooks,migration}.

Identity/Tenancy (ver [ADR-004](adr/ADR-004-autorizacion-por-ambitos.md)): usuario global único; PlatformRole (`PlatformAdmin`, `SupportOperator`) separado de memberships de negocio; TenantMembership (`TenantOwner`, `TenantAdmin`, `TenantMember`) y BranchMembership (`BranchManager`, `BranchStaff`) con alcance explícito. Tenant representa una cuenta/empresa; Branch representa una sucursal. `/platform` no activa tenant; `/app/{tenantSlug}` identifica el workspace pero el servidor debe validar estado, membership, alcance y permiso. Los IDs/roles recibidos del cliente no conceden autoridad.


## 3. Modelo de datos recomendado (no copia literal)

| Módulo | Entidades/tablas objetivo y reglas |
| --- | --- |
|   | users, auth_sessions, mfa_factors, platform_roles, tenants, tenant_memberships, |
| Identity & Tenancy | branches, branch_memberships; roles de plataforma independientes de los roles |
|   | TenantOwner/TenantAdmin/TenantMember y BranchManager/BranchStaff. |
| Catalog | essences, bottles, product_recipes, suppliers, product_images; SKU único por tenant para |
|   | activos; costos snapshot en transacciones. |
|   | stock_balances(tenant,branch,product,location,quantity,version), stock_movements, |
| Inventory | stock_counts, stock_count_lines, stock_reservations opcional; jamás cambiar stock sin |
|   | movimiento. |
| Sales | sales, sale_items, sale_payments, sale_adjustments/refunds, cash_movements; |
|   | idempotency; líneas y pagos independientes. |
| Credit & Procurement | customers opcional, receivables, receivable_payments, purchase_orders, |
|   | purchase_order_lines, goods_receipts, supplier_payables, supplier_payments. |
| Billing SaaS | plans, entitlements, billing_accounts, subscriptions, invoices, payment_attempts, |
|   | webhook_events; proveedor de pagos desacoplado. |
| Reporting & Ops | audit_events, outbox_messages, notifications, expenses, cash_sessions, report_snapshots |
|   | opcional; support_tickets, ticket_messages. |
| AI | knowledge_documents, document_chunks(tenant_id,embedding), ai_query_logs sin datos |
|   | sensibles; búsqueda aislada por tenant. |

Regla de organización: una cuenta/empresa es un tenant y sus tiendas son sucursales. Una persona puede tener memberships en varios tenants y sucursales usando un único `User`. Los roles iniciales son fijos y se traducen a permissions/policies en el servidor; no se crean roles personalizados ni un `TenantGroup` para reportes cross-tenant hasta que exista una necesidad validada.

## Invariantes de base de datos

- Todas las entidades de negocio incluyen tenant_id NOT NULL; sucursal debe pertenecer al mismo tenant mediante FK compuesta o validación transaccional y restricciones.

- Crear UNIQUE(tenant_id,id) donde existan FK compuestas; `UNIQUE(tenant_id,user_id)` en tenant memberships y `UNIQUE(tenant_id,branch_id,user_id)` en branch memberships; `UNIQUE(tenant_id,idempotency_key)` por operación; índices alineados con consultas.
- `BranchMembership(tenant_id,branch_id)` referencia `Branch(tenant_id,id)` y `(tenant_id,user_id)` referencia `TenantMembership(tenant_id,user_id)`; ambas evitan membresías cruzadas.
- `TenantOwner`/`TenantAdmin` tienen alcance de todas las sucursales; `TenantMember` necesita asignación de sucursal. Un gerente solo accede a sus sucursales explícitas. Todo tenant conserva al menos un TenantOwner activo.

- Usar numeric(p,s) o decimal en .NET para dinero y cantidades; definir gramos/ml/unidades explícitamente. Usar timestamptz en UTC y la zona America/Bogota en reportes/presentación.

- Costos históricos, precios y nombres de venta son snapshots inmutables; nunca recalcular venta pasada con precio actual.

- RLS como defensa adicional en tablas compartidas, usando contexto establecido solo por servidor dentro de transacciones; probar fugas con conexión pooled y fallos de contexto.

- Guardar secretos TOTP cifrados; tokens de recuperación hasheados; audit log mínimo y sin contraseñas ni tokens.


## 4. Contratos REST iniciales

| Método / ruta | Propósito y controles |
| --- | --- |
| POST /api/v1/auth/login | Autenticación, anti-enumeración, rate limiting, MFA condicional. |
| GET /api/v1/me/context | Identidad y memberships disponibles; roles/contexto plataforma separados del contexto tenant. |
| GET /api/v1/platform/tenants | Solo PlatformAdmin; SIN tenant activo. |
| GET/POST /api/v1/essences | Tenant auth; búsqueda paginada, SKU único, límite plan. |
| POST /api/v1/inventory/adjustments | Movimiento y balance transaccional; motivo/permiso. |
| POST /api/v1/sales | Idempotency-Key; atomicidad venta/líneas/stock/pagos. |
| POST /api/v1/receivables/{id}/payments | Abono sin sobrepago, idempotencia y auditoría. |
| GET /api/v1/reports/sales?from=&to= | Agregación SQL, paginación cuando aplica. |
| POST /api/v1/billing/checkout | Generar sesión de pago del proveedor, precio resuelto en backend. |
| POST | Firma verificada; persistir evento y procesar idempotente. |
| /api/v1/billing/webhooks/{provider} |   |

Usar ProblemDetails (RFC 9457), OpenAPI, DTOs tipados, validation pipeline, 400/401/403/404/409/422 y 429 coherentes. Para límites de plan definir 403/409/422 o código específico de dominio; evitar abusar de HTTP 402 como semántica universal de cuota.

## 5. Laboratorio guiado: pasos incrementales

## Paso 01 · Congelar y auditar Danelix actual

Objetivo: Tener mapa verificable antes de programar

□ Exportar estructura PostgreSQL real: constraints, triggers, RLS, policies, funciones RPC, índices y grants (sin secretos).

- Catalogar pantallas, reglas, endpoints, permisos, buckets, jobs, webhooks y dependencias a Supabase.

□ Crear matriz de equivalencias módulo viejo -> nuevo y registrar datos críticos y estrategia de rollback.

Validaciones y pruebas: Prueba: ejecutar casos clave en sistema antiguo y grabar resultados esperados; revisar 2 tenants distintos.

Terminado cuando: Documento de inventario + matriz de riesgos + copias comprobadas.

## Paso 02 · Crear monorepo, ADRs y calidad básica

Objetivo: Entorno reproducible

- Crear solución .NET 10, Angular y PostgreSQL en Docker Compose.

- Agregar editorconfig, analizador, lint, formatter, pre-commit opcional y CI que compile y ejecute tests.

- Escribir ADR-001 monolito modular y ADR-002 estrategia multi-tenant.

Validaciones y pruebas: Pruebas: docker compose up; healthcheck API; lint/build Angular; pipeline CI.

Terminado cuando: Desarrollador nuevo levanta proyecto siguiendo README.


## Paso 03 · Definir dominio y migración EF Core

Objetivo: Esquema inicial versionado

- Alinear el dominio con ADR-004 antes de persistirlo: `User` global; `Tenant`, `Branch`, `TenantMembership` (`TenantOwner`, `TenantAdmin`, `TenantMember`) y `BranchMembership` (`BranchManager`, `BranchStaff`, estado).

- Registrar `PlatformRole` en Identity, separado de `TenantMembership`; no incorporar `PlatformAdmin` a memberships de negocio ni usar un rol `SuperBoss`.

- Mapear el dominio con EF Core y generar migraciones separadas por módulo/contexto conforme a ADR-003.

- Crear datos seed solo para desarrollo que demuestren dos tenants, varias sucursales, roles tenant/sucursal y un usuario con memberships en más de una sucursal. Los roles de plataforma se seed-ean por separado.

- Agregar índices y constraints compuestos de pertenencia.

Validaciones y pruebas: migrar BD vacía y dos veces; FK de sucursal/membership cruzada entre tenants debe fallar; probar unicidad de memberships y que un tenant no quede sin TenantOwner activo bajo operaciones concurrentes.

Terminado cuando: Migraciones automáticas comprobadas en BD de test.

## Paso 04 · Separar Platform Admin de tenant

Objetivo: Corregir el problema de identidad estructural

- Diseñar login global y selección de workspace solo para usuarios con memberships tenant; un mismo User puede pertenecer a varios tenants.

- `PlatformAdmin` y `SupportOperator` viven separados de los roles/memberships tenant. PlatformAdmin accede a `/platform` sin tenant activo y administra cuentas globales de usuario, tenants, planes y configuración SaaS; el rol no autoriza cambiar memberships de negocio ni leer datos operativos tenant.

- Al provisionar un tenant, invitar/asignar explícitamente a su primer `TenantOwner`. El soporte no consulta datos de negocio sin una concesión explícita; el flujo temporal auditado se completa en Paso 18.

- Crear endpoints /me/context y /platform/tenants, claims mínimos y roles verificables en BD.

Validaciones y pruebas: Unit: políticas; integration: PlatformAdmin sin tenant puede operar en `/platform` pero no leer datos de tenant; un TenantOwner/Admin no obtiene acceso a `/platform` por ese rol; un usuario con ambos contextos debe autorizarlos de forma independiente.

Terminado cuando: Matriz autorización plataforma/tenant/sucursal en verde.

## Paso 05 · Autenticación endurecida

## Objetivo: Sesiones y recuperación seguras

- Implementar ASP.NET Core Identity o mecanismo maduro equivalente, hashes adaptativos, MFA TOTP y códigos de recuperación de un solo uso.

□ Elegir cookie BFF httpOnly+SameSite+CSRF para web o access-token corto+refresh rotation segura; documentar decisión.

- Agregar rate limit, lockout, sesiones revocables, reset por token hasheado y entrega de email.

Validaciones y pruebas: Tests: 5 fallos, lockout, expiración, refresh/revocación, CSRF, TOTP correcto/incorrecto y que no se registren secretos.

Terminado cuando: Auth E2E; sin passwords/secretos en logs.

## Paso 06 · Autorización tenant y sucursal

Objetivo: Cero filtraciones entre perfumerías

- Implementar `ITenantContext` y el alcance de sucursal desde ruta/sesión y memberships verificadas; nunca desde body arbitrario.

- Implementar policies fijas del servidor: TenantOwner/TenantAdmin abarcan todas las sucursales; TenantMember requiere BranchMembership; BranchManager administra solo BranchStaff de sus sucursales; BranchStaff no administra usuarios.

- Impedir escalada de privilegios: solo TenantOwner asigna/revoca TenantAdmin y transfiere propiedad; TenantOwner/Admin pueden administrar roles de sucursal; BranchManager solo gestiona BranchStaff de sus sucursales; TenantAdmin no cambia facturación.

- Las memberships asignadas en varias sucursales conceden solo la unión de esos alcances; los reportes se agregan únicamente sobre sucursales autorizadas.

- Evaluar RLS, middleware de conexión y conexión pooling; añadir suite de ataques cross-tenant.

Validaciones y pruebas: tenant A no lee/edita tenant B; gerente de sucursal A no consulta ni administra B; empleado no accede a otra sucursal; TenantOwner/Admin sí tienen alcance tenant-wide; IDs de tenant/sucursal manipulados no elevan permisos; Include, SQL raw y background jobs respetan el contexto; tests de grants impiden asignar roles superiores.

Terminado cuando: Ningún test de aislamiento se salta: gate bloqueante de merge.

## Paso 07 · Primer vertical slice: Esencias

Objetivo: Aprender patrón end-to-end pequeño

- Entidad Essences, validaciones, unique SKU activo por tenant, soft-delete.

- GET paginado+filtros, POST, PATCH y DELETE; Form Angular con errores 409 y 422.

- El stock inicial entra mediante comando de recepción/movimiento, no update casual del catálogo.

Validaciones y pruebas: Unit: nombre obligatorio, precio no negativo; integration: SKU único, soft delete, aislamiento, paginación y cuota concurrente.

Terminado cuando: Pantalla Angular y API completas con tests.


## Paso 08 · Envases e imágenes

Objetivo: Eliminar mutaciones directas inseguras

- Crear DTO Bottle y validators; subir imagen mediante API o URL firmada privada con rutas tenant-scoped.

- Verificar MIME real, peso, dimensiones, permiso y procesar imagen en worker si procede.

- Reemplazar cantidad editada en tabla por ajuste de inventario auditable.

Validaciones y pruebas: Tests: extensión falsa, acceso imagen otro tenant, cantidad negativa, stock sin movimiento rechazado.

Terminado cuando: Envases CRUD + storage segura + auditoría.

## Paso 09 · Motor de inventario

## Objetivo: Consistencia bala/bodega/envases

- Definir SKU+ubicación: bala, bodega y envase con unidades explícitas y balances versionados.

□ Crear comandos Receive, Transfer, Adjust, Consume y Reverse; transacciones y bloqueo FOR UPDATE o actualización condicional.

- Movimiento inmutable con before/after, actor, causa, fecha, referencia e idempotency.

Validaciones y pruebas: Integration con Postgres real/Testcontainers: 20 consumos concurrentes no dejan saldo negativo; rollback sin movement huérfano.

Terminado cuando: Inventario auditable, sin modificación directa.

## Paso 10 · Conteos y aprobaciones

Objetivo: Reconciliar inventario confiablemente

- Agregar sesión de conteo, ítems, tolerancias por tenant y evidencia de discrepancias.

- Conteo toma snapshot/version; aprobar revalida contra cambios de stock (recuento o conflicto).

- Permitir rechazo, nota, aprobaciones por lote en transacciones separadas controladas.

Validaciones y pruebas: Tests: doble aprobación, snapshot obsoleto, conteo de otra sucursal, tolerancia límite, lote parcialmente inválido.

Terminado cuando: Ciclo conteo/aprobación/rechazo reproducible.

## Paso 11 · Venta real: primer caso complejo

Objetivo: Practicar ACID y reglas de negocio

- Modelar Sale+Items+Payments y recetas de consumo por tipo (nuevo, recarga, solo envase).

□ Validar stock y descuentos bala primero, bodega después; snapshots de costo/precio; commit atómico con movimientos.

- Agregar idempotencia y eventos outbox para alertas/pedidos posteriores; entrada_dinero pasa a CashMovement.

Validaciones y pruebas: Tests: venta concurrente último stock; request repetida no duplica venta; venta falla -> stock igual; cuotas y tenant.

Terminado cuando: Venta auditable con movimientos correctos.

## Paso 12 · Crédito y cuentas por cobrar

Objetivo: Pagar parcialmente sin inconsistencias

- Relacionar venta a crédito opcional con receivable; preservar cuentas informativas independientes si negocio lo necesita.

- Registrar abonos separados, saldo calculado, vencimiento, anulación por compensación y control de sobrepago.

- Crear listado Angular pendientes/vencidas/pagadas; ningún checkbox pagado sin asiento de pago.

Validaciones y pruebas: Tests: dos abonos concurrentes; pago duplicado; abono > saldo; reversión; diferencias de zona horaria.


Terminado cuando: Saldo conciliable con suma de pagos.

## Paso 13 · Compras y proveedores

Objetivo: Cerrar cadena stock-costo-proveedores

- Crear supplier, PurchaseOrder+Lines, Receipt y Payable+Payments.

- Al recibir insumo, incrementar inventario transaccional y conservar costo snapshot.

- Mantener idempotencia de cuenta/pago y anulación con motivo; reparar FK compuestas.

Validaciones y pruebas: Tests: no sobrepago, recibo no duplicado, pedido parcial, proveedor cross-tenant, reversión controlada.

Terminado cuando: Recepciones y cuentas proveedor trazables.

## Paso 14 · Gastos, caja y conciliación

Objetivo: Evitar confundir utilidad con caja

- Modelar Expenses y CashSession (apertura/cierre) opcional; CashMovement separado de ventas.

□ Definir KPI: venta bruta, descuentos, costo vendido, utilidad bruta, gastos operativos, utilidad estimada y efectivo cobrado.

- Documentar tratamiento de fiados, devoluciones, compras de inventario y cobros para evitar doble contabilización.

Validaciones y pruebas: Tests: fiado aumenta venta pero no caja; abono aumenta caja pero no segunda venta; ingreso manual no infla productos.

Terminado cuando: Reporte de reconciliación de un día de ejemplo.

## Paso 15 · Reporting backend

Objetivo: Pasar estadísticas del cliente a SQL

- Diseñar GET KPIs y gráficas por tenant/sucursal/período y timezone Bogota.

- Crear consultas agregadas SQL/EF, índices por (tenant_id,fecha), paginación y límites de rango.

- Medir EXPLAIN ANALYZE, comparativas antes/después, caché solo cuando convenga.

Validaciones y pruebas: Tests: límites fin de mes/año, zonas horarias, devoluciones, multi-tenant; benchmark con dataset sintetizado.

Terminado cuando: KPIs correctos y tiempo p95 documentado.

## Paso 16 · Planes y entitlements

Objetivo: Cuotas sin condiciones de carrera

- Separar Plan, Feature/Entitlement, Subscription y Usage; normalizar niveles por tenant.

- Limitar usuarios, esencias y ventas mensuales mediante enforcement servidor y locks/atomicidad; no fail-open.

- Hacer tests de límites al borde, upgrade/downgrade y cuenta suspendida.

Validaciones y pruebas: Tests: dos usuarios creando última esencia simultáneamente solo uno pasa; sin servicio de cuotas negar según política.

Terminado cuando: Cuotas correctas y auditables.

## Paso 17 · Pasarela de pagos y suscripciones

Objetivo: SaaS monetizable correctamente

- Comparar Stripe/Wompi/Mercado Pago/PayU según incorporación legal en Colombia, recurrencia, cobros y payout.

- Integrar un provider adapter, checkout server-side, webhook firmado, idempotency por event_id, cola/reintentos.

- Modelar estados trialing/active/past_due/canceled, períodos y grace policy; nunca activar por redirect de éxito.

Validaciones y pruebas: Tests: webhook repetido/fuera de orden, firma inválida, pago rechazado, renovación, reembolso, upgrade y acceso suspendido.


Terminado cuando: Sandbox end-to-end + reconciliación proveedor/BD.

## Paso 18 · Soporte, auditoría y notificaciones

Objetivo: Operación del SaaS

- Soporte `SupportOperator` accesible globalmente por scope explícito; usuario tenant solo tickets propios.

- Definir la política de aprobación/consentimiento y luego implementar concesiones temporales de soporte con tenant/sucursal, permisos mínimos, motivo/ticket, expiración, actor y auditoría; solo lectura por defecto.

- Outbox + worker para email/notificaciones/stock bajo; claves anti-duplicado.

- Audit event estructurado con diff sanitizado, actor, tenant, correlación y retención.

Validaciones y pruebas: reintentos worker no duplican notificación; SupportOperator sin grant no lee datos de tenant; grant vencido/fuera de scope se rechaza; acceso y cambios quedan auditados; logs sin PII crítica.

Terminado cuando: Eventos recuperables después de caída del worker.

## Paso 19 · Angular shell + frontend modular

Objetivo: Aprender Angular sin reescribir todo a ciegas

- Crear shell standalone, lazy routes `/app` y `/platform`, selección de tenant y selector de sucursal según memberships, services API tipados y manejo común de errores.

- Usar reactive forms, Signals para estado local, interceptors y guards UX (no seguridad real).

- Implementar slices conforme endpoints listos, tests de componentes y accesibilidad.

Validaciones y pruebas: login/selección de tenant y sucursal, vistas de dueño/admin/gerente/empleado, permisos de controles, formularios inválidos, errores 409/422, loading y vacío. Los guards son UX, no autorización real.

Terminado cuando: Frontend sin acceso directo a PostgreSQL/Supabase.

## Paso 20 · E2E y seguridad

Objetivo: Validación de recorridos de negocio

- Playwright: login + esencias + inventario + venta + crédito + compra + pagos SaaS mock.

- SAST/dependency scans, amenazas OWASP, secretos, uploads, backups, CSRF/CORS/CSP y rate-limits.

- Pruebas de recuperación de fallo, operación duplicada, reintentos y auditoría.

Validaciones y pruebas: suite con tenants A/B, sucursales A1/A2, roles de plataforma/tenant/sucursal, escalada de privilegios, IDOR, soporte sin/con grant, webhook spoofing y acciones simultáneas.

Terminado cuando: Criterios de seguridad y regresión verdes en CI.

## Paso 21 · Docker + VPS

Objetivo: Operar la app de verdad

- Dockerfiles multi-stage .NET/Angular, Compose API/worker/proxy/postgres y healthchecks.

- Linux con usuario sin root, SSH key, UFW, TLS, redes privadas, secretos fuera de repo y backups externos cifrados.

- GitHub Actions build/test/scan/deploy y rollback a imagen anterior; restaurar backup en ambiente temporal.

Validaciones y pruebas: Tests: restore real, apagar/reiniciar contenedor, migración previa al deploy y rollback ensayado.

Terminado cuando: Staging accesible por HTTPS y runbook de recuperación.

## Paso 22 · K3s como laboratorio

Objetivo: Aprender orquestación cuando Compose sea sólido

- Desplegar API/worker/Angular con Deployments, Services, ingress, ConfigMaps, Secrets, probes y recursos.

- Dejar PostgreSQL gestionado externamente o con estrategia stateful/backup claramente documentada.

- Probar rolling updates, restart de pods, persistencia, cambios de config y observabilidad.

Validaciones y pruebas: Tests: rollout/rollback, readiness, límites de memoria, caída de pod; aclarar que 1 VPS no da HA.

Terminado cuando: Clúster de laboratorio reproducible sin sacrificar producción.


## Paso 23 · IA + pgvector

- Objetivo: Añadir una función con utilidad real

- Primero buscador semántico en catálogo/documentos por tenant, chunks, embeddings y búsqueda híbrida.

- Crear endpoints autorizados y registro de fuentes; consultas financieras exactas mediante herramientas SQL allowlisted, de solo lectura y scoped.

- Evaluar precisión, coste, fuga entre tenants e inyección de instrucciones en documentos.

Validaciones y pruebas: Tests: búsquedas cruzadas devuelven cero; preguntas de cifras exactas no se responden por similitud; prompt injection bloqueada por diseño de tools.

Terminado cuando: Asistente con citas, control de permisos y métricas de calidad.

## Paso 24 · Ensayo migración + cutover

Objetivo: Pasar de Supabase/React sin perder datos

- Definir ETL por tabla, mapeo de IDs, reconstrucción de movimientos, identidades y snapshots.

- Simular export/import, conciliación por tenant, conteos, montos, checksums y usuarios; estrategia de contraseñas (rehash gradual o reset), MFA y activos.

- Hacer dual-run solo si diseño soporta consistencia; preferir ventana de solo lectura, delta final, validación, DNS cutover y rollback ensayado.

Validaciones y pruebas: Tests: totales de ventas/saldos/stock por empresa equivalentes y diferencias justificadas; prueba de rollback.

Terminado cuando: Acta de migración con firmas, checklist y restauración probada.

## 6. Orden de módulos y dependencias

Secuencia de dependencias: Identity/Tenancy -> Catalog -> Inventory -> Sales -> Receivables -> Procurement -> Finance/Reports -> Billing -> Platform operations -> AI. Angular se implementa verticalmente desde el paso 7 y no solo al final. Docker/CI se usan desde el paso 2; K3s solo al final.

## 7. Matriz de roles y permisos a construir

| Rol | Contexto | Capacidades |
| --- | --- | --- |
| PlatformAdmin | /platform sin tenant | Gestionar cuentas globales de usuario, empresas/tenants, planes y facturación SaaS; sin lectura implícita de datos de negocio. Operaciones sensibles auditadas. |
| SupportOperator | /platform sin tenant | Responder tickets y diagnósticos limitados; datos de negocio solo con concesión temporal, acotada y auditada. |
| TenantOwner | /app/{tenant} | Alcance de todo el tenant; administra equipo, sucursales, configuración, reportes y facturación; transfiere propiedad de forma auditada. |
| TenantAdmin | /app/{tenant} | Alcance operativo de todas las sucursales; gestiona equipo y operación, sin transferir propiedad ni gestionar facturación. |
| TenantMember | /app/{tenant} | Membresía base; sin acceso operativo hasta tener una o más memberships de sucursal. |
| BranchManager | /app/{tenant}/branches/{branch} | Solo sucursales asignadas; reportes locales y gestión de BranchStaff de esas sucursales; no concede roles tenant ni de gerente. |
| BranchStaff | /app/{tenant}/branches/{branch} | Ejecuta acciones autorizadas por módulo en sus sucursales asignadas; no administra usuarios. |

Política estricta: PlatformAdmin no accede a ventas/stock por su rol; SupportOperator requiere concesión temporal, acotada, justificada y auditada (solo lectura por defecto). No mezclar roles de plataforma con `TenantMembership`. `TenantOwner`/`TenantAdmin` tienen alcance tenant-wide; los roles de sucursal se limitan a las sucursales asignadas. Todo tenant conserva al menos un TenantOwner activo.


## 8. Entregables por PR y plantilla de aprendizaje

Para cada issue: Problema de negocio -> Invariante -> Modelo de datos -> Endpoint/DTO -> Validaciones -> Pruebas unitarias -> Pruebas integración con PostgreSQL -> Pantalla Angular -> Revisión seguridad tenant -> Demo -> ADR breve.

Ejemplo PR “Crear esencia”: esquema Essence; UNIQUE tenant/SKU activo; POST /essences; validator nombre/marca/SKU/precio; errores ProblemDetails; autorización create:essence; limit plan; prueba 409 duplicado; prueba tenant A vs B; Angular form; screenshots y explicación del criterio de diseño.

Preguntas para entrevista tras cada módulo: ¿Qué rompe bajo concurrencia? ¿Cómo se revierte? ¿Cuál es la frontera de transacción? ¿Cómo evitas IDOR? ¿Cómo mediste una mejora? ¿Cómo restauras datos? ¿Qué compromiso arquitectónico aceptaste?

## 9. Criterios de salida a producción

- 0 violaciones en la suite tenant-isolation e IDOR; MFA y sesiones revisadas.

- Policies verificadas para PlatformAdmin, SupportOperator, TenantOwner, TenantAdmin, TenantMember, BranchManager y BranchStaff; cero escaladas de rol o alcance.

- Ventas, recibos y pagos idempotentes; stock nunca negativo o inconsistente tras concurrencia.

- Conciliación ventas/gastos/cuentas/stock por empresa aprobada con datos históricos.

- Webhooks firmados, reintentos, conciliación y sandbox de facturación verificados.

- Pruebas restore completadas; backups externos cifrados y retención definida.

- CI/CD, monitoreo de errores, latencia p95, alertas, rollback y runbooks listos.

- Términos, privacidad, tratamiento de datos y requisitos legales/fiscales del proveedor de pagos revisados.

## 10. Documentación técnica de consulta

https://learn.microsoft.com/aspnet/core/ https://learn.microsoft.com/ef/core/ https://angular.dev/ https://www.postgresql.org/docs/ https://github.com/pgvector/pgvector https://docs.docker.com/compose/ https://docs.k3s.io/ https://docs.stripe.com/webhooks https://docs.wompi.co/

Nota: la elección de pasarela debe hacerse después de confirmar capacidad de suscripción/cobro recurrente, soporte para comerciantes colombianos, requisitos de empresa, manejo de tokens, medios de pago y comisiones actuales. El esquema aportado describe diseño lógico; sin dump físico ni repositorio no se garantiza la presencia real de índices, triggers, políticas y funciones mencionadas.
