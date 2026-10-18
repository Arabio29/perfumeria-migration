# ADR-001 — Monolito modular como arquitectura base

- **Estado:** Aceptada
- **Fecha:** 2026-10-17
- **Decisor:** Eliasib (desarrollo), con asistencia de arquitectura

## Contexto

Danelix se reconstruye como SaaS multi-tenant para perfumerías. El dominio abarca módulos bien diferenciados (Identity/Tenancy, Catalog, Inventory, Sales, Receivables, Procurement, Finance, Reporting, Billing, Platform, Support, Notifications, AI) que comparten datos y requieren consistencia transaccional fuerte: una venta debe escribir venta, líneas, pagos y movimientos de stock en **una sola transacción**, y el stock no puede quedar negativo ni desincronizado.

Además, el desarrollo está a cargo de una sola persona cuyo objetivo es aprender arquitectura y operarla de verdad, con presupuesto mínimo de infraestructura.

## Decisión

Danelix se despliega como **una sola aplicación ASP.NET Core** (más un worker de background en fases posteriores) construida como **monolito modular**:

- Cada módulo es un **proyecto separado**: `src/Modules/<Módulo>/` con `Domain/`, `Application/`, `Infrastructure/` y `Contracts/`.
- La API (`src/Danelix.Api`) solo compone módulos y configura el pipeline; no contiene reglas de negocio.
- La comunicación entre módulos ocurre **exclusivamente por sus `Contracts`** (interfaces, DTOs, eventos internos). Un módulo no referencia la `Infrastructure` ni las entidades internas de otro.
- Una única base de datos PostgreSQL compartida (ver ADR-002).
- Sin MediatR ni buses internos por defecto: casos de uso explícitos y rastreables.
- `tests/Architecture` (fase posterior) hará cumplir los límites automáticamente en CI.

## Alternativas consideradas

1. **Microservicios desde el inicio.** Descartada: exige madurez operativa (descubrimiento de servicios, mensajería, sagas, despliegues y observabilidad distribuidos) que un solo desarrollador no puede sostener; y las operaciones distribuidas dificultan —no facilitan— invariantes como «nunca stock negativo».
2. **Monolito en capas tradicional** (Controllers → Services → Repositories). Descartada: los límites se erosionan con el tiempo («todo llama a todo»). La aplicación anterior sufrió exactamente eso (lógica de negocio dispersa entre componentes y RPC de base de datos).
3. **Monolito modular** — elegida. Es el punto medio: límites internos explícitos con el coste operativo de una sola aplicación.

## Consecuencias

**Positivas**

- Un despliegue, un CI, depuración y trazas simples; transacciones ACID reales entre módulos.
- Cada módulo puede crecer, cambiar o extraerse a servicio propio más adelante sin reescribir el dominio.
- Esfuerzo concentrado en aprender arquitectura y dominio, no en operar infraestructura distribuida.

**Negativas / riesgos**

- Un bug grave puede degradar toda la aplicación (mitigado con tests y despliegues reproducibles).
- Los límites requieren disciplina: sin tests de arquitectura, un monolito modular degenera en capas tradicionales.
- No hay escalado independiente por módulo (aceptable en el horizonte actual del negocio).

## Reglas derivadas

- Un módulo jamás consulta tablas de otro módulo; usa sus `Contracts`.
- Los `Contracts` son mínimos y estables; los internals de cada módulo no son públicos.
- Toda regla de negocio vive en `Domain`/`Application` del módulo correspondiente, nunca en la API ni en la UI.
