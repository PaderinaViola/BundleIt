# ADR-003: Incremental Extraction of Billing / Payouts via Strangler Fig and Resilient Cross-Context Communication

**Date:** 2026-11-27
**Status:** Accepted
**Scope:** Task 2 (Stage 6) — Architectural Decision Record (`ADR-003`), Non-Functional Requirement (NFR) Quality Attribute Scenarios, and Polly v8 Resilience Specification.
**Related Documents:** [`context-map.md`](./context-map.md), [`decomposition-roadmap.md`](./decomposition-roadmap.md), [`stateDiagram-purchase.md`](./stateDiagram-purchase.md).

---

## 1. Context

BundleIt (`EShopService.sln`, `.NET 10`) is evolving from a modular monolith into a resilient distributed architecture. Strategic domain analysis (`context-map.md`) and force analysis (`decomposition-roadmap.md`) identified **`Billing / Payouts`** (`EShopService.Billing`) as a **Generic** subdomain sitting at the leaf edge of our dependency graph.

`Billing / Payouts` integrates directly with external third-party financial providers (**Payment Gateway** and **Payout Provider**) that are outside our operational control and exhibit occasional high latency (15–45 seconds) or transient HTTP `503`/`504` failures. Simultaneously, `Purchasing` (our **Core** subdomain) must guarantee strict transactional invariants—specifically ensuring that a `Purchase` is never double-charged on retries (`PaymentAmountMismatchException`) and that `PurchasePaid` reliably triggers `AccessGranted` in `Content Library` without blocking user-facing cart or catalog operations during external gateway outages.

---

## 2. Decision

We will extract **`Billing / Payouts`** as our first standalone microservice (`eshopservice-billing-api`) using the **Strangler Fig** pattern and enforce the following architectural rules for cross-context communication:

1. **Hexagonal Port Isolation & Contract-First HTTP:**
   - `Purchasing.Application` communicates with `Billing / Payouts` exclusively through the outbound domain port `IPaymentGatewayPort`.
   - Synchronous calls from `Purchasing.Infrastructure` (`HttpBillingGatewayAdapter`) target versioned URI endpoints (`/api/v1/payments/captures`, `/api/v1/payments/{id}/refunds`) governed by an OpenAPI 3.1 specification (`docs/contracts/openapi-billing.yaml`) and **Tolerant Reader** DTOs (no shared `.dll` contract assemblies between services).
2. **Mandatory Idempotency & RFC 9457 Error Semantics:**
   - Every mutating `POST` command across service boundaries requires a deterministic `Idempotency-Key` header (stored atomically with the payment record in `billing.IdempotentRequests`) so network retries never cause duplicate charges or duplicate refunds.
   - All domain and validation errors are returned using **IETF RFC 9457 (`application/problem+json`)** with W3C `traceparent` correlation headers, mapped centrally via ASP.NET Core `IExceptionHandler`.
3. **Standardized Client Resilience Pipeline (Polly v8):**
   - `HttpBillingGatewayAdapter` wraps outbound HTTP calls in an ordered Polly v8 pipeline: **Fallback / Domain Mapping $\rightarrow$ Total Request Timeout $\rightarrow$ Exponential Retry with Decorrelated Jitter $\rightarrow$ Circuit Breaker + Bulkhead $\rightarrow$ Attempt Timeout**.
4. **Asynchronous Event-Driven State Propagation (Transactional Outbox):**
   - State-changing outcomes (`PurchasePaid`, `RefundApproved`, `PayoutCompleted`) are persisted atomically with an outbox record in the service's local database (`Database-per-Service`) and published asynchronously to downstream consumers (`Content Library`, `Email / Notification Service`), eliminating distributed two-phase commits.

---

## 3. Considered Options

| Option | Pros | Cons | Verdict |
|---|---|---|---|
| **Option A: Keep `Billing / Payouts` inside the monolith permanently** | Zero network latency; single deployment unit. | External payment/payout gateway hangs exhaust shared ASP.NET thread pools, risking cascading failure into `Purchasing` and `Catalogue`; broader PCI/secret audit scope. | **Rejected** |
| **Option B: Big-Bang rewrite of all 5 Bounded Contexts into microservices** | Immediate full distribution. | High risk of distributed monolith anti-pattern while `Purchasing` and `Licensing` boundaries are still maturing; massive operational complexity for a small team. | **Rejected** |
| **Option C: Incremental Strangler Fig extraction of `Billing / Payouts` + Port/ACL + Polly v8 & Outbox (Chosen)** | Zero changes to `Purchasing.Domain`; isolates external financial gateways; enforces `Database-per-Service` and idempotency; reversible via canary feature flag. | Requires maintaining an HTTP adapter, OpenAPI contract tests, and an outbox worker. | **Accepted** |

---

## 4. Consequences

### Positive
- **Fault & Thread-Pool Isolation:** Slow responses or outages from the external Payment Gateway or Payout Provider are contained inside `eshopservice-billing-api` and fast-failed via Circuit Breaker in `< 5 ms` once tripped, leaving `Purchasing` cart actions and `Catalogue` browsing unaffected.
- **Zero Domain Pollution:** `Purchasing.Domain` remains 100% free of HTTP, JSON, and third-party gateway concepts (verified by CI Fitness Functions `FF-01` and `FF-05`).
- **Safe Automatic Retries:** Deterministic `Idempotency-Key` enforcement allows Polly to safely retry transient `503`/`504`/timeout errors without risking duplicate customer charges.

### Negative / Trade-offs
- **Eventual Consistency:** `AccessRight` provisioning in `Content Library` after `PurchasePaid` occurs asynchronously (typically `< 200 ms`), requiring the UI to handle brief transitional `Paid` states gracefully.
- **Operational Overhead:** Introduces a second container, separate database schema/migrations (`BillingDbContext`), and distributed tracing (`traceparent` + OpenTelemetry) in `docker-compose.yml`.

---

## 5. Non-Functional Requirement (NFR) Quality Attribute Scenarios

### NFR-01: Fault Isolation & Availability Under External Payment Gateway Degradation

| Scenario Element | Specification |
|---|---|
| **Source of Stimulus** | External **Payment Gateway** (third-party upstream provider) and concurrent storefront buyers. |
| **Stimulus** | During a peak seasonal bundle launch (200 requests/sec across browsing and checkout), the external Payment Gateway degrades, responding after **30 seconds** or returning **HTTP `503 Service Unavailable`** on 70% of requests. |
| **Artifact** | `HttpBillingGatewayAdapter` in `Purchasing.Infrastructure` and `eshopservice-billing-api`. |
| **Environment** | Production Docker cluster under peak launch load (`200 req/s`). |
| **Response** | 1. Individual HTTP attempts time out at **2.0 s** (`AttemptTimeout`), preventing 100-second default `.NET HttpClient` socket exhaustion.<br>2. Polly retries transient `503`/timeout errors up to **2 times** with exponential backoff + jitter using the same `Idempotency-Key` within a **7.5 s** `TotalRequestTimeout`.<br>3. Once failure ratio reaches **50% over a 30 s window** (minimum 10 requests), the **Circuit Breaker** opens for **15 s**, rejecting subsequent payment capture calls in **< 5 ms** (`BrokenCircuitException` $\rightarrow$ RFC 9457 `503` with `Retry-After: 15`).<br>4. **Bulkhead** caps concurrent billing connections at **20**, preserving thread-pool capacity for `Cart` and `Catalogue` endpoints. |
| **Response Measure** | - `Catalogue` browsing and `Cart` mutations maintain **$p_{95} \le 150\text{ ms}$** latency and **$\ge 99.9\%$ availability** throughout the external outage.<br>- Failed payment calls fail fast in **$\le 5\text{ ms}$** once the circuit opens, with **zero** thread-pool starvation crashes. |

### NFR-02: Financial Consistency & Idempotency Under Transient Network Failures

| Scenario Element | Specification |
|---|---|
| **Source of Stimulus** | Unreliable internal/external network dropping TCP connections after payment capture is processed by `Billing / Payouts` but before the HTTP response reaches `Purchasing`. |
| **Stimulus** | `HttpBillingGatewayAdapter` experiences a socket timeout on `POST /api/v1/payments/captures` and automatically retries the request with the same `Idempotency-Key` and payload hash. |
| **Artifact** | `Billing` idempotency middleware (`billing.IdempotentRequests`), `PaymentGatewayTranslator` (ACL), and `Purchase` aggregate. |
| **Environment** | Normal production operation with intermittent packet loss. |
| **Response** | 1. `Billing` detects the existing `Idempotency-Key` with matching `PayloadHash` and returns the cached `200 OK` `PaymentReceipt` without calling the external gateway a second time.<br>2. If the same `Idempotency-Key` is ever sent with a altered `Money` amount, `Billing` rejects it immediately with **HTTP `409 Conflict` / `422 Unprocessable Content`** (`IdempotencyKeyConflictException`).<br>3. `Purchase.ConfirmPayment` verifies `paid == PriceSnapshot` (Invariant #1) and transitions `Pending` $\rightarrow$ `Paid` exactly once. |
| **Response Measure** | **0% duplicate charges** across 10,000 simulated timeout-and-retry chaos test runs; **100% ledger reconciliation accuracy** between `Purchasing` and `Billing`. |

---

## 6. Polly v8 Resilience Parameter Contract (`Purchasing` $\rightarrow$ `Billing`)

To prevent misconfigured timeout hierarchies (Lecture 4, Slides 36 & 40), `HttpBillingGatewayAdapter` enforces the following mathematically consistent parameters ($\text{TotalRequestTimeout} > (N_{\text{retries}} + 1) \cdot \text{AttemptTimeout} + \text{Backoff}$):

| Polly v8 Pipeline Layer (Outer $\rightarrow$ Inner) | Configured Value | Architectural Justification |
|---|---|---|
| **1. Fallback / Exception Mapper** | Maps `BrokenCircuitException` / `TimeoutRejectedException` $\rightarrow$ `PaymentGatewayUnavailableException` | Translates infrastructure resilience failures into domain-meaningful exceptions for RFC 9457 `503` responses. |
| **2. Total Request Timeout** | **`7.5 s`** ($> 3 \times 2.0\text{ s} + 1.5\text{ s}$) | Bounds worst-case user wait time during checkout so browser clients never hang indefinitely. |
| **3. Retry (Exponential + Decorrelated Jitter)** | **`MaxRetryAttempts = 2`**, `BaseDelay = 300 ms`, `UseJitter = true` | Handles transient `408`, `429`, `502`, `503`, `504`, and network exceptions; **explicitly disabled** for deterministic `400`, `404`, `409`, `422` errors. |
| **4. Circuit Breaker + Bulkhead** | `FailureRatio = 0.50`, `SamplingDuration = 30 s`, `MinimumThroughput = 10`, `BreakDuration = 15 s`; `MaxConcurrency = 20` | Cuts traffic immediately during sustained gateway outages and caps concurrent outbound sockets. |
| **5. Attempt Timeout** | **`2.0 s`** | Aborts hung individual TCP/HTTP attempts early so retries can execute within the total budget. |