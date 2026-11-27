# AGENTS.md — Architectural Constitution for BundleIt (`EShopService`)

This file is the root architectural constitution for AI coding agents and developers working in the `EShopService` (`.NET 10`) repository. Every task must obey the **Spec-Anchored** workflow: consult or update the specifications in `docs/` first, write failing tests (RED), implement the code (GREEN), and verify all fitness functions (`dotnet test`).

---

## 1. Map of Sources of Truth (Read Before Coding)

| Artifact | Path | Authority Over |
|---|---|---|
| **Value Objects & Invariants** | `docs/architecture/aggregates-and-invariants.md` | Ubiquitous Language, Value Objects (`Money`, `Quantity`, `TaxId`, `BillingDetails`, `PaymentDeadline`, `UsageSummary`, `SeriesPart`, `ArtFile`), strongly typed IDs, and Invariants #1–#17. |
| **Purchase State Machine** | `docs/architecture/stateDiagram-purchase.md` | `Purchase` lifecycle states, transitions `T1`–`T9`, `IRefundEligibilityPolicy` Strategy implementations, and Given-When-Then TDD test cases. |
| **Strategic Design & Context Map** | `docs/architecture/context-map.md` | Subdomain classification (`Core`, `Supporting`, `Generic`), `Purchasing` Bounded Context Canvas, Context Map patterns, `Shared Kernel` boundaries, and the Payment Gateway ACL specification. |
| **Architectural Fitness Functions** | `docs/architecture/fitness-functions.md` | Static guards (`FF-01`..`FF-05`), dynamic/contract SLO guards (`FF-06`..`FF-07`), boundary placement diagram, API versioning policy, and `CleanArchitectureGuardTests` predicates. |
| **Decomposition Roadmap** | `docs/architecture/decomposition-roadmap.md` | Disintegrators vs. Integrators, module extraction order (Stages 0–3), Data Surgery (`Database-per-Service`), and the Strangler Fig migration plan for `Billing / Payouts`. |
| **ADR-003 & NFR Scenarios** | `docs/architecture/adr-003-decomposition-and-communication.md` | Cross-context HTTP/event rules, Polly v8 resilience parameter contract, and NFR quality attribute scenarios. |
| **Billing OpenAPI 3.1 Contract** | `docs/contracts/openapi-billing.yaml` | Contract-First HTTP schema, required `Idempotency-Key` headers, JSON Schema Value Object bounds, request/response `examples`, and RFC 9457 `ProblemDetails` error responses. |

---

## 2. Hierarchy Rule ("Closest Wins") & Module Microarchitectures

When a subdirectory contains its own `AGENTS.md` (e.g., `src/Services/Purchasing/AGENTS.md`), its specific rules specialize this root constitution:

- **`Purchasing` & `Bundles` (Core Subdomains):** **Rich Domain Model + Hexagonal / Clean Architecture + CQRS.** All business invariants live inside Aggregate Roots (`Purchase`, `Cart`, `Bundle`) and immutable `sealed record` Value Objects. Controllers and Application Handlers must never contain domain validation logic.
- **`ContentLibrary` (Supporting Subdomain):** **Event Sourcing + CQRS.** `UsageRecord` captures opens/downloads as an immutable event stream; read models (`UsageSummary`, `Subscriber's library`) use `.AsNoTracking()` SQL projections.
- **`Licensing` (Supporting Subdomain):** **Rich Domain Model + Hexagonal Architecture.** Coordinates watermark injection and copyright `Report` takedowns with `ContentLibrary`.
- **`Billing` (Generic Subdomain):** **Transaction Script / Active Record + Anti-Corruption Layer (ACL).** External payment and payout provider SDKs/DTOs are strictly encapsulated behind `IPaymentGatewayPort` and `internal` translators/adapters in `EShopService.Billing.Infrastructure.Acl`.

---

## 3. Architectural Prohibitions & Fitness Functions (`FF-01`..`FF-07`)

Full technical predicates live in `docs/architecture/fitness-functions.md`. All pull requests and agent sessions must pass `CleanArchitectureGuardTests` (`dotnet test`):

1. **`FF-01` (Domain Purity):** `EShopService.*.Domain` and `BundleIt.SharedKernel` must have **0** dependencies on `*.Application`, `*.Infrastructure`, `*.Presentation`, `Microsoft.EntityFrameworkCore`, `Microsoft.AspNetCore.*`, `System.Text.Json`, or `Npgsql`, and **0** ORM/JSON attributes (`[Table]`, `[Key]`, `[Column]`, `[ForeignKey]`, `[JsonPropertyName]`).
2. **`FF-02` (Rich Domain Encapsulation):** Aggregate Roots and Entities in `EShopService.*.Domain` (`Purchase`, `Cart`, `CartLine`, `Bundle`, `BundleItem`, `AccessRight`, `UsageRecord`, `Payout`) must expose **0** `public set;` properties and **0** public parameterless constructors. State changes occur only via Ubiquitous Language methods and static Factory Methods.
3. **`FF-03` (Application & CQRS Encapsulation):** `EShopService.*.Application` must have **0** dependencies on `*.Infrastructure` or `*.Presentation`, and all `*CommandHandler` and `*QueryHandler` classes must be `internal sealed`.
4. **`FF-04` (ACL Boundary Containment):** External gateway models (`GatewayChargeRequestDto`, `GatewayChargeResponseDto`, `GatewayErrorResponseDto`), `PaymentGatewayTranslator`, `StripePaymentHttpAdapter`, and `PaymentGatewayAclFacade` inside `EShopService.Billing.Infrastructure.Acl` must be `internal` (`!IsPublic`) and never appear in `IPaymentGatewayPort` signatures.
5. **`FF-05` (Service & Schema Isolation — Database-per-Service):** Bounded Context projects (`EShopService.<ContextA>.*`) must have **0** `<ProjectReference>` or namespace dependencies on another Bounded Context (`EShopService.<ContextB>.*`), except `BundleIt.SharedKernel`. Database migrations must contain **0** cross-schema `FOREIGN KEY` constraints.
6. **`FF-06` & `FF-07` (Contract & Runtime SLOs):** 100% of HTTP responses must conform to `docs/contracts/openapi-billing.yaml` (`application/problem+json` with `errorCode`), meet $p_{95} < 400\text{ ms}$ latency and $< 0.1\%$ `5xx` error budgets, and produce **0** duplicate operations on repeated `Idempotency-Key` calls.

---

## 4. API & Integration Standards

- **Contract-First OpenAPI 3.1:** Implement HTTP controllers and typed `IHttpClientFactory` consumers strictly against `docs/contracts/openapi-billing.yaml`. Do not share `.dll` contract assemblies across services; use minimal **Tolerant Reader** DTOs on the consumer side.
- **Versioning & Backward Compatibility:** Follow `/api/v1/` URI versioning, Postel's Law (`additionalProperties: false` on requests, liberal reading on responses), and RFC 8594 `Deprecation` / `Sunset` headers as specified in `docs/architecture/fitness-functions.md`.
- **Business Intent Endpoints:** Model state transitions as intent sub-resources (`POST /api/v1/payments/authorizations`, `POST /api/v1/payments/{paymentId}/refunds`, `POST /api/v1/payouts/disbursements`), never anemic `PUT` state overwrites.
- **Mandatory Idempotency & RFC 9457 Problem Details:** Every mutating `POST` endpoint must require the `Idempotency-Key` UUID header. Never catch domain exceptions in controllers to build ad-hoc JSON; let the global `IExceptionHandler` map `DomainException` subtypes to `application/problem+json` (`400`, `404`, `409`, `422`, `503`) with `errorCode` and W3C `traceId`.

---

## 5. Known Spec-to-Code Backlog Items

- **`SeriesId` & `SeriesPart` in `EShopService.Bundles.Domain`:** Defined in `docs/architecture/aggregates-and-invariants.md` (Sections 3 & 4) but not yet added to `EShopService.Bundles.Domain/ValueObjects/Identifiers.cs` or `Bundle.cs`. When working on the `Bundles` context, implement `SeriesId` (`readonly record struct`), `SeriesPart` (`sealed record`), and the corresponding EF Core configuration + migration.

---

## 6. Closed Verification Loop & Command

Follow **Plan $\rightarrow$ Red $\rightarrow$ Green $\rightarrow$ Refactor + Mutation Check** for every change:
1. Write xUnit tests from the Given-When-Then tables in `docs/architecture/stateDiagram-purchase.md` **before** writing production code.
2. Run the full test and architecture guard suite:
   ```bash
   dotnet test --configuration Release
   ```
3. Perform a quick mutation sanity check (temporarily comment out a domain guard or invariant check to confirm the test suite turns RED, then restore it).