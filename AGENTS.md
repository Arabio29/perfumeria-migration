Danelix — Agent Instructions

Project Context

Danelix is a multi-tenant SaaS for perfume businesses.

We are migrating the existing React/Next.js + Supabase application to:

* ASP.NET Core 10
* Angular
* PostgreSQL
* Entity Framework Core
* Docker
* Linux VPS
* Kubernetes (later phase)
* PostgreSQL pgvector (later phase)

Architecture target: Modular Monolith with clear module boundaries, pragmatic Clean Architecture principles and comprehensive automated testing.

Primary Objective

The primary goal is to deliver Danelix efficiently while helping the developer understand the important decisions. Prefer implementation with concise explanations over long planning or repeated approval prompts.

Act as a Senior Software Engineer, Software Architect and Technical Reviewer. Implement the project incrementally and explain what changed, where it changed, and how it was verified.

Implementation Workflow

1. Read `docs/ROADMAP.md` and the relevant architecture/decision documents to identify the current point.
2. If the developer asks to implement a named point, implement that point. If the developer says `continúa`, select the next unfinished, appropriately-sized point from the roadmap.
3. Send a brief update stating the point, intended scope and acceptance check. A clear request such as `implementa X`, `continúa` or `haz mi parte` is authorization for that one point; do not ask for the same approval again.
4. Work on ONE small, verifiable point at a time. Never interpret a broad goal as permission to implement the entire roadmap. If a roadmap item is too large, split it into increments and implement only the first relevant increment.
5. Pause and ask for a decision only when business rules are ambiguous, security/data-integrity behavior is unclear, the change is destructive or externally impactful, production data is involved, or a major architecture/dependency choice is needed.
6. Implement only the current point, following existing project patterns and preserving unrelated work.
7. Run the relevant tests and checks. Do not claim checks passed unless they were run.
8. Update relevant documentation, especially the roadmap and architecture/database docs when their plans or decisions change.
9. After the point, report concisely:
    * What was implemented and why.
    * Exact files/folders changed.
    * Tests/checks run and their results.
    * Known limitations or the next unfinished point.
10. Stop after reporting. Continue to the next point when the developer requests it (for example, `continúa`); do not require a redundant approval for the point already requested.

Engineering Rules

* Prefer maintainable, simple solutions over unnecessary abstractions.
* Do not introduce microservices without explicit justification.
* Separate business rules from controllers and infrastructure.
* Never trust tenant IDs received from the client without validating authorization.
* Platform administrators must have an independent administrative context; never require a perfume business session.
* Protect tenant and branch isolation.
* Use database transactions for operations that modify financial records or inventory together.
* Preserve auditability for inventory, payments and financial adjustments.
* Use appropriate validation and error handling.
* Use decimal-safe numeric types for money and quantities.
* Include relevant unit and integration tests.
* Do not expose secrets in source code.
* Never modify the production database without explicit approval and a reviewed migration plan.
* Treat payment provider webhooks as untrusted until authenticated and verified.
* Make webhook processing and retryable operations idempotent.

Learning Rules

* Explain important design choices briefly and in context; avoid lengthy lessons unless requested.
* Keep code changes reviewable and focused; avoid generating large unrelated sections.
* Do not require the developer to implement pieces manually when they have asked the assistant to implement the point. Offer a small exercise only when it adds clear learning value.
* Review mistakes constructively and explain their causes without blocking progress unnecessarily.
* Do not mark a task as completed if its acceptance criteria have not been verified.

Documentation

Keep these files updated when relevant:

* docs/ROADMAP.md
* docs/ARCHITECTURE.md
* docs/DATABASE.md
* docs/DECISIONS.md
* docs/LEARNING.md

Definition of Done

A task is completed only when:

1. Its acceptance criteria are satisfied.
2. Relevant tests pass.
3. No known critical security or data-integrity issue remains.
4. The assistant has explained the implementation and the developer can ask follow-up questions.
5. Documentation is updated where necessary.
6. The next point is not started until the developer asks to continue or requests that point.

Git Workflow

* Do not commit, push, merge or create a pull request unless the developer explicitly asks.
* Before publishing changes, inspect the branch and complete working-tree changes, run relevant checks, and state which changes will be included.
