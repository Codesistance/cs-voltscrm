# VoltsCRM — Build TODO

Remaining work toward the approved 20‑phase build, split by area.

| Area | Project(s) | Owner |
|---|---|---|
| **Backend** | `VoltsCRM.API` / `Application` / `Domain` / `Infrastructure` | Claude Code |
| **UI** | `src/frontend/web` (React + Vite) | **Cursor** |
| **Worker** | `VoltsCRM.Worker` (background service) | Claude Code |

Legend: `[x]` done · `[~]` partial / shell · `[ ]` todo

**Last updated:** 2026-06-20 (config-driven payment gateways + no-op `voltspayments` + portal self-service
payment + real webhook security shipped; Phase 9 & 18b unblocked)

> **How to use the review sections below.** Items prefixed `S#` / `A#` / `P#` are written to be
> applied by another LLM without further discovery. Each names the exact file(s), the problem, the
> fix, and acceptance criteria. Severity/priority is on each item. **Do not start from the old
> "highest-priority" list — it was written before allocation/import/overdue/geo/gateway scaffolding
> landed and is corrected in "Status reconciliation" below.**

---

## 🚀 v1 launch scope & priorities (confirmed with product owner, 2026-06-18)

**Goal (umbrella):** get the business off spreadsheets onto one system; in doing so serve all four
value pillars — billing-on-system, collections, field operations, customer self-service.

**Launch-critical (v1):**
1. **Online payments** — `P3`. ✅ **UNBLOCKED 2026-06-20.** Gateways are now config-driven and the
   first-party **no-op `voltspayments`** gateway runs a full payment end-to-end (initiate + webhook +
   inline reconcile) with no external provider. Picking a real provider (M-Pesa/Stripe) is now a
   business choice, not a build blocker — a real adapter is a drop-in (`IPaymentGateway` + config row).
2. **Customer self-service portal** — `P8`. Portal UI over read APIs **plus self-service payment (18b)
   done backend-side** (`/api/portal/me/gateways` + `POST /api/portal/me/payments`). FE Pay button to wire.
3. **Customer CSV import** — `P5`, scoped to **customer records only** at launch (balances/
   subscriptions are *not* migrated in v1). Hardening `S9` + `A3`–`A5` ride along with this.

**Post-launch backlog (explicitly de-scoped from v1):**
- **Automated-billing Worker** — `P1` / `A7`. De-scoped from v1 per product owner. At launch, invoices
  are produced via the manual `POST /api/invoices/generate` trigger, **not** the Worker. Recurring
  invoicing, overdue sweep, and reminders (`P2`) move to post-launch.
- Token vending (Phase 15), extended migration (balances/subscriptions — see `P5`), additional
  adapters (Phase 20), maps/field-agent routing (Phase 14).

> Note: security items `S1`–`S11` and correctness items `A1`–`A8` are **not** scope toggles — they
> apply regardless of v1/post-launch and should be fixed as the relevant code is touched. `S1`
> (seeded-admin credential) must be resolved before **any** non-local deployment.

---

## 📦 Phase 18 — implementation spec (read portal + profile)

Apply-ready work items from the approved plan
(`~/.claude/plans/update-readme-by-asking-lexical-corbato.md`). Scope: complete the **read portal** by
adding a **read-only enriched profile**. Self-service payment (18b) is **out of scope** here — deferred
to a gateway-dependent follow-up (see `P3`). Everything else in the read portal
(`summary`/`invoices`/`subscriptions`/`payments`) is already built; only profile is missing.

**Conventions to follow:** mirror the existing portal vertical slice. Backend query/handler pattern =
[PortalSummaryQuery.cs](src/backend/libraries/VoltsCRM.Application/Features/Portal/PortalSummaryQuery.cs);
endpoint group pattern = [PortalEndpoints.cs](src/backend/api/VoltsCRM.API/Endpoints/PortalEndpoints.cs);
frontend pattern = `features/portal/api/{types,portalApi,queries}.ts` + `routes/`.

### Backend  *(Claude Code)*

**18-BE-1 — Add `PortalProfileDto`.**
File: [PortalDtos.cs](src/backend/libraries/VoltsCRM.Application/Features/Portal/PortalDtos.cs)
(already has `using VoltsCRM.Application.Common.Models;`, so `AddressDto` is in scope). Add:
```csharp
public sealed record PortalProfileDto(
    string AccountNumber, string FullName, string Phone, string? Email,
    string Status, AddressDto Address);
```
Reuse `AddressDto` from
[LocationModels.cs](src/backend/libraries/VoltsCRM.Application/Common/Models/LocationModels.cs). Keep it
flat/self-contained — do **not** reference the Customers-feature DTOs.

**18-BE-2 — Add query + handler.**
New file `src/backend/libraries/VoltsCRM.Application/Features/Portal/PortalProfileQuery.cs`:
`PortalProfileQuery(Guid CustomerId) : IRequest<PortalProfileDto>`.
- Handler ctor `(IAppDbContext db)`, mirrors `PortalSummaryQueryHandler`.
- ⚠️ **Avoid client-side evaluation.** `PersonalInfo.FullName` is a computed C# property and may not
  translate in a `.Select(...)`. Project the **owned scalar columns** first, then compose in memory:
  ```csharp
  var c = await db.Customers.AsNoTracking()
      .Where(x => x.Id == query.CustomerId)
      .Select(x => new {
          x.AccountNumber,
          x.PersonalInfo.FirstName, x.PersonalInfo.LastName,
          x.PersonalInfo.Phone, x.PersonalInfo.Email,
          x.Status,
          x.Location.Address.Street, x.Location.Address.City,
          x.Location.Address.Region, x.Location.Address.Country
      })
      .FirstOrDefaultAsync(ct)
      ?? throw new NotFoundException(nameof(Customer), query.CustomerId);

  return new PortalProfileDto(
      c.AccountNumber,
      $"{c.FirstName} {c.LastName}".Trim(),
      c.Phone, c.Email, c.Status.ToString(),
      new AddressDto(c.Street, c.City, c.Region, c.Country));
  ```
  Use `NotFoundException` from
  [Common/Exceptions/NotFoundException.cs](src/backend/libraries/VoltsCRM.Application/Common/Exceptions/NotFoundException.cs);
  `Customer` is in `VoltsCRM.Domain.Entities.Crm`.
- **Acceptance:** returns the caller's account number, full name, phone, email, status, and address; no
  client-side-evaluation warning in EF logs.

**18-BE-3 — Add the endpoint.**
File: [PortalEndpoints.cs](src/backend/api/VoltsCRM.API/Endpoints/PortalEndpoints.cs). Add to the
existing `/api/portal/me` group (already `RequireAuthorization().RequireUserType(UserType.Customer)`):
```csharp
group.MapGet("/profile", ProfileAsync);
```
Handler mirrors `SummaryAsync`: resolve via the existing `ResolveCustomerIdAsync`; if null →
`TypedResults.Forbid()`; else `TypedResults.Ok(await sender.Send(new PortalProfileQuery(customerId.Value), ct))`.
- **Acceptance:** `GET /api/portal/me/profile` → 200 for a Customer token; 403 for Admin/Agent
  (`RequireUserType`); 403 for a Customer whose `AppUser.CustomerId` is null.

**18-BE-4 — Integration test.**
New file `tests/api/VoltsCRM.Integration.Tests/Tests/PortalProfileTests.cs`, mirroring
`PermissionEnforcementTests` and reusing `IntegrationTestBase` / `TestUsers` / `TestTokenFactory`.
Cases: (a) Customer token → 200, payload `AccountNumber` matches the seeded customer; (b) Admin token →
403; (c) Agent token → 403; (d) Customer with no linked `CustomerId` → 403.
- **Acceptance:** all cases pass (needs Docker for the Postgres test container).

### Frontend  *(Cursor)*

**18-FE-1 — Type.**
File: [types.ts](src/frontend/web/src/features/portal/api/types.ts). Add:
```ts
export interface PortalProfile {
  accountNumber: string
  fullName: string
  phone: string
  email: string | null
  status: string
  address: { street: string; city: string; region: string; country: string }
}
```
(Reuse a shared `Address` type from `@/shared/api/types` if one exists; otherwise inline as above.)

**18-FE-2 — API client.**
File: [portalApi.ts](src/frontend/web/src/features/portal/api/portalApi.ts). Add to `portalApi`:
```ts
profile: () => get<PortalProfile>(`${BASE}/profile`),
```
and add `PortalProfile` to the type import.

**18-FE-3 — Query hook.**
File: [queries.ts](src/frontend/web/src/features/portal/api/queries.ts). Add
`profile: () => [...portalKeys.all, 'profile'] as const` to `portalKeys`, and:
```ts
export function usePortalProfile() {
  return useQuery({ queryKey: portalKeys.profile(), queryFn: portalApi.profile })
}
```

**18-FE-4 — Page.**
File: [PortalProfilePage.tsx](src/frontend/web/src/features/portal/routes/PortalProfilePage.tsx).
Replace the auth-only display with `usePortalProfile()`; render account number, full name, phone,
email, status, and full address in the existing `Card`. Add loading/empty states consistent with the
other portal pages (`PortalInvoicesPage`/`PortalPaymentsPage`). The `/portal/profile` route + nav entry
already exist — **no routing changes**.
- **Acceptance:** logged in as a customer, `/portal/profile` shows the enriched details; `npm run build`
  (tsc) passes.

### Definition of done (Phase 18, this scope)
- `GET /api/portal/me/profile` live, customer-scoped, IDOR-safe; integration test green.
- Portal Profile page renders enriched contact details; other portal pages unaffected.
- Mark Phase 18 ✅ in the matrix below (read portal complete); leave 18b (self-service payment) tracked
  separately under `P3`.

---

## Status reconciliation (TODO vs. actual code — corrected this review)

Several items previously listed as "todo / not started" are **already implemented**. Verified against
source on 2026-06-18:

| Previously listed as TODO | Actual state | Evidence |
|---|---|---|
| Payment allocation on record | **Done** — full apply + reverse, incl. overpayment → `PaymentAccount` credit | [RecordPaymentCommand.cs](src/backend/libraries/VoltsCRM.Application/Features/Payments/RecordPaymentCommand.cs), [PaymentLifecycleCommands.cs:74](src/backend/libraries/VoltsCRM.Application/Features/Payments/PaymentLifecycleCommands.cs#L74) |
| Apply discount on payment | **Done** — single-payment grant applied at record time | [RecordPaymentCommand.cs:102](src/backend/libraries/VoltsCRM.Application/Features/Payments/RecordPaymentCommand.cs#L102) |
| `PaymentAccount` credit/debit | **Done** — in completion/reversal helper | [PaymentLifecycleCommands.cs:113](src/backend/libraries/VoltsCRM.Application/Features/Payments/PaymentLifecycleCommands.cs#L113) |
| Mark-overdue API | **Done** — endpoints for invoices **and** installments | [InvoiceEndpoints.cs:19](src/backend/api/VoltsCRM.API/Endpoints/InvoiceEndpoints.cs#L19), [InstallmentEndpoints.cs:19](src/backend/api/VoltsCRM.API/Endpoints/InstallmentEndpoints.cs#L19) |
| CSV import API | **Done** (rows-as-JSON dry-run + commit; hardening needed — see S9/A3–A5) | [ImportEndpoints.cs](src/backend/api/VoltsCRM.API/Endpoints/ImportEndpoints.cs) |
| `customers/geo` endpoint | **Done** | [CustomerEndpoints.cs:16](src/backend/api/VoltsCRM.API/Endpoints/CustomerEndpoints.cs#L16) |
| Payment-gateway framework + config API | **Scaffolded** — `IPaymentGateway` + 2 **stub** adapters + keyed DI + settings API | [IPaymentGateway.cs](src/backend/libraries/VoltsCRM.Application/Common/Interfaces/IPaymentGateway.cs), [PaymentGatewayStubs.cs](src/backend/libraries/VoltsCRM.Infrastructure/Integrations/PaymentGatewayStubs.cs), [DependencyInjection.cs:82](src/backend/libraries/VoltsCRM.Infrastructure/DependencyInjection.cs#L82) |
| Token-vending framework + config API | **Scaffolded** — `ITokenVendingPlatform` + KPLC **stub** + keyed DI + settings API | same files; [SettingsEndpoints.cs](src/backend/api/VoltsCRM.API/Endpoints/SettingsEndpoints.cs) |
| Auto-debit mandates | **Partial** — settings entity + API exist; no mandate CRUD, no scheduler | [SettingsEndpoints.cs:19](src/backend/api/VoltsCRM.API/Endpoints/SettingsEndpoints.cs#L19) |
| `GenerateInvoicesCommand` N+1 | **Fixed** — customers batch-loaded (B8) | [GenerateInvoicesCommand.cs:115](src/backend/libraries/VoltsCRM.Application/Features/Invoices/GenerateInvoicesCommand.cs#L115) |
| PaymentAccount concurrency token (Security #6) | **Half-done** — `RowVersion`/`xmin` token present; **no `DbUpdateConcurrencyException` handling** (see A1) | [PaymentAccountConfiguration.cs:16](src/backend/libraries/VoltsCRM.Infrastructure/Persistence/Configurations/Billing/PaymentAccountConfiguration.cs#L16) |

---

## 🔴 Security review (security-architect)

Ordered by severity. Apply S1 before any non-local deployment.

### S1 — [RESOLVED 2026-06-18] Seeded admin password no longer derivable from source
- **Was:** the seeded super-admin password was `Base64(HMAC-SHA256("VoltsCRM", ddMMyyyy))[..16] + "!Aa1"`
  with the HMAC key **hardcoded in source**, so anyone with the repo/binary could compute any day's
  password and log in as super-admin (security-by-obscurity; the date is not entropy).
- **Decision (with owner):** keep the reseeding admin, the daily rotation, and the Super-Admin disable
  kill-switch (`IsActive=false` is durable — the seeder never re-enables it) — but make the **HMAC key
  the secret**, sourced per-environment from **AWS SSM Parameter Store**, injected as env var
  `Seed__HmacKey` (config `Seed:HmacKey`), exactly like `Jwt__Key`.
- **Implemented:** key is now a parameter
  ([SeedCredentialGenerator.cs](src/backend/libraries/VoltsCRM.Infrastructure/Identity/SeedCredentialGenerator.cs)),
  bound via `SeedOptions` and **fail-closed** at startup (≥32 chars) in
  [Program.cs](src/backend/api/VoltsCRM.API/Program.cs); `DbSeeder` passes it; the `SeedAdminUser`
  migration no longer bakes a deterministic password (random placeholder, overwritten by the seeder);
  the `seed-password` CLI prints nothing without the key (no longer a source-derivable oracle). Infra:
  `seed_hmac_key` SSM Parameter Store SecureString + IAM + ECS `Seed__HmacKey` injection
  ([secrets.tf](infrastructure/deployment/secrets.tf), [ecs.tf](infrastructure/deployment/ecs.tf)).
  Local dev key in [docker-compose.yml](docker-compose.yml) / user-secrets.
- **Acceptance:** ✅ password not derivable from source; startup fails-closed without `Seed:HmacKey`;
  rotation + Super-Admin disable retained. Operator action: populate `/voltscrm-production/seed-hmac-key`
  in SSM Parameter Store (see [infrastructure/README.md](infrastructure/README.md)).

### S2 — [RESOLVED 2026-06-20] Webhook signature validation pattern fixed
- **Was:** `ValidateWebhookSignature` returned `true` unconditionally for the gateway stubs; any wired
  webhook would accept forged callbacks (financial fraud).
- **Implemented:** real constant-time HMAC-SHA256 verification with a timestamp/replay window
  ([WebhookSignature.cs](src/backend/libraries/VoltsCRM.Infrastructure/Integrations/WebhookSignature.cs),
  `CryptographicOperations.FixedTimeEquals`); the no-op `voltspayments` adapter uses it for real. The
  remaining stubs (M-Pesa/Stripe) are now flagged `IStubGateway` and **cannot be exposed**: the webhook
  endpoint returns 404 for a stub-backed key, and a **startup guard** in
  [Program.cs](src/backend/api/VoltsCRM.API/Program.cs) fails closed if any gateway config with
  `visibility=true` maps to a stub. Forged/tampered/replayed payloads → 401.
- **Acceptance:** ✅ unit + integration tests (`WebhookSignatureTests`, `PaymentWebhookTests`) prove
  tampered signatures fail and stub routes 404. Real adapters inherit the secure path by reusing the helper.

### S3 — [HIGH] Gateway / token-vending secrets stored in plaintext
- **Files:** [SettingsCommands.cs](src/backend/libraries/VoltsCRM.Application/Features/Settings/SettingsCommands.cs) + the `PaymentGatewaySettings` / `TokenVendingSettings` entities under `Domain.Entities.Organisation`.
- **Issue:** `TokenVendingSettings.ApiKey` (and any future gateway secret) is persisted unencrypted; masking (`••••••••`) happens only on the read DTO, not at rest. Separately, the payment-gateway input only captures `Provider`/`MerchantId`/`PublicKey` (a *public* value) — there is **no field for the actual API secret / private key / webhook secret**, so the integration story is both insecure and incomplete.
- **Fix:** decide the secret-of-record location (prefer an AWS SSM Parameter Store reference stored in the row, not the secret itself); if storing in-DB is required, encrypt with `IDataProtector` / column encryption. Add the missing secret field(s) and never return them on read.
- **Acceptance:** DB inspection shows no plaintext API secrets; secrets are write-only via the API.

### S4 — [MEDIUM] Auth rate limiter is global (unpartitioned)
- **File:** [Program.cs:125](src/backend/api/VoltsCRM.API/Program.cs#L125).
- **Issue:** `AddFixedWindowLimiter("auth")` has no partition key → a single shared 10/min bucket for *all* clients. (a) One attacker exhausts the global budget and locks out every legitimate login (DoS); (b) it does not throttle per-attacker brute force.
- **Fix:** use a `PartitionedRateLimiter` keyed by client IP (`HttpContext.Connection.RemoteIpAddress`, honoring a trusted forwarded-headers config) and/or submitted username.
- **Acceptance:** one IP hitting the limit does not 429 a different IP.

### S5 — [MEDIUM] No account lockout on failed login
- **Files:** [AuthEndpoints.cs:41](src/backend/api/VoltsCRM.API/Endpoints/AuthEndpoints.cs#L41), [DependencyInjection.cs:59](src/backend/libraries/VoltsCRM.Infrastructure/DependencyInjection.cs#L59).
- **Issue:** login calls `UserManager.CheckPasswordAsync` directly; no `options.Lockout` configured and no `AccessFailedCount` tracking, so per-account brute force is bounded only by the global limiter in S4.
- **Fix:** configure Identity lockout (e.g. 5 failures → 15 min) and increment/reset failure counts on login (or route through `SignInManager` with `lockoutOnFailure: true`).
- **Acceptance:** N consecutive bad passwords locks that account without affecting others.

### S6 — [MEDIUM] Weak password policy for a financial system
- **File:** [DependencyInjection.cs:61](src/backend/libraries/VoltsCRM.Infrastructure/DependencyInjection.cs#L61).
- **Issue:** `RequiredLength = 8`, `RequireNonAlphanumeric = false`.
- **Fix:** raise minimum to ≥12 and reconsider complexity; optionally add a breached-password check. Coordinate with the seeded-password suffix in S1.
- **Acceptance:** new/changed passwords below the new bar are rejected.

### S7 — [LOW] JWT accepted algorithm not pinned
- **File:** [Program.cs:88](src/backend/api/VoltsCRM.API/Program.cs#L88) (`TokenValidationParameters`).
- **Issue:** no `ValidAlgorithms` constraint; tokens are issued HS256 but validation does not restrict the accepted algorithm set.
- **Fix:** add `ValidAlgorithms = [SecurityAlgorithms.HmacSha256]`.
- **Acceptance:** a token presented with any other `alg` is rejected.

### S8 — [LOW] No refresh-token reuse detection
- **File:** [AuthEndpoints.cs:75](src/backend/api/VoltsCRM.API/Endpoints/AuthEndpoints.cs#L75).
- **Issue:** rotation works and `ReplacedByTokenHash` is recorded, but re-presenting an already-revoked token simply 401s; the recorded chain is never used to detect theft.
- **Fix:** on receiving a revoked-but-known token, revoke the entire token family for that user (defence-in-depth against refresh-token theft).
- **Acceptance:** replaying a rotated-out token invalidates the active session chain.

### S9 — [MEDIUM] No size / row cap on CSV import
- **Files:** [ImportEndpoints.cs](src/backend/api/VoltsCRM.API/Endpoints/ImportEndpoints.cs), [ImportCommands.cs](src/backend/libraries/VoltsCRM.Application/Features/Import/ImportCommands.cs).
- **Issue:** dry-run/commit accept an unbounded `Rows[]` JSON array; both also materialise *all* existing account numbers into memory (see A5). A large payload is a memory/DoS vector.
- **Fix:** cap row count (e.g. ≤10k/request) and request body size; reject oversized payloads with 413/400. (Inbound CSV is parsed client-side, so CSV-formula-injection is lower risk, but sanitise any value later re-exported.)
- **Acceptance:** an over-cap import is rejected before processing.

### S10 — [LOW] Geocoding proxy not rate-limited
- **File:** [GeocodingEndpoints.cs:18](src/backend/api/VoltsCRM.API/Endpoints/GeocodingEndpoints.cs#L18).
- **Issue:** authenticated (good) but uncapped; an authed user can hammer upstream Nominatim, breaching its usage policy. SSRF risk is low (fixed `BaseUrl`).
- **Fix:** add a per-user rate-limit policy to the geocoding group.

### S11 — [VERIFY + tests] Object-level authz on FieldAgent endpoints (OWASP A01)
- **Files:** [AgentEndpoints.cs](src/backend/api/VoltsCRM.API/Endpoints/AgentEndpoints.cs); compare with portal which is already IDOR-safe (derives `customerId` from the token, [PortalEndpoints.cs:89](src/backend/api/VoltsCRM.API/Endpoints/PortalEndpoints.cs#L89)).
- **Task:** confirm FieldAgent reads/writes are scoped to *assigned* customers (not "any customer, because the caller is an agent"). Then add security regression tests: cross-type token rejection, agent-accessing-unassigned-customer = 403, portal-accessing-another-customer = 403. Test projects are currently thin (handoff notes them empty).

---

## 🟠 Architecture & correctness review (software-architect)

### A1 — [HIGH] Concurrency token present but `DbUpdateConcurrencyException` is never handled
- **Files:** [PaymentAccountConfiguration.cs:16](src/backend/libraries/VoltsCRM.Infrastructure/Persistence/Configurations/Billing/PaymentAccountConfiguration.cs#L16) (`RowVersion`), [PaymentLifecycleCommands.cs](src/backend/libraries/VoltsCRM.Application/Features/Payments/PaymentLifecycleCommands.cs), [RecordPaymentCommand.cs](src/backend/libraries/VoltsCRM.Application/Features/Payments/RecordPaymentCommand.cs), [GlobalExceptionHandler.cs](src/backend/api/VoltsCRM.API/Setup/GlobalExceptionHandler.cs).
- **Issue:** the optimistic token will throw `DbUpdateConcurrencyException` under concurrent balance writes, but no handler catches/retries it (grep: zero references). It surfaces as a 500.
- **Fix:** add a bounded retry (reload + reapply) around the payment write paths, and map unrecoverable concurrency conflicts to **409** in the global handler. Closes the second half of Security #6.
- **Acceptance:** two concurrent payments crediting the same account either both apply correctly or one retries; neither corrupts the balance nor 500s.

### A2 — [HIGH] Over-allocation to an invoice/installment is not prevented
- **File:** [RecordPaymentCommand.cs:124](src/backend/libraries/VoltsCRM.Application/Features/Payments/RecordPaymentCommand.cs#L124) (`ValidateAllocationsAsync`); effects in [PaymentLifecycleCommands.cs:76](src/backend/libraries/VoltsCRM.Application/Features/Payments/PaymentLifecycleCommands.cs#L76).
- **Issue:** validation checks existence/ownership/not-already-`Paid`, but **not** that allocation amount ≤ the invoice/installment *outstanding* balance, and does not account for other **pending** payments already allocated to the same target. Two pending payments can each allocate the full amount; on completion both call `Invoice.RecordPayment` → overpaid invoice / negative outstanding.
- **Fix:** validate each allocation against current outstanding (minus amounts reserved by other pending allocations), and/or guard inside `Invoice.RecordPayment` / `Installment.MarkPaid` to reject/clamp overpayment. Verify the domain methods' current behaviour and decide clamp-vs-throw.
- **Acceptance:** allocating more than outstanding is rejected with a validation error; concurrent pending allocations cannot jointly overpay.

### A3 — [MEDIUM] Import validation duplicated and divergent between dry-run and commit
- **File:** [ImportCommands.cs](src/backend/libraries/VoltsCRM.Application/Features/Import/ImportCommands.cs) — `ImportDryRunHandler.ValidateRow` (rich, reports errors) vs `ImportCommitHandler` (inline checks, silently `skip`).
- **Issue:** a row that passes dry-run can be silently skipped on commit (or vice-versa) because the two code paths enforce different rules. Users see a clean preview, then a different outcome.
- **Fix:** extract one shared validator used by both; commit must enforce exactly what dry-run previewed and report (not silently skip) anything it rejects.
- **Acceptance:** identical input yields consistent valid/invalid classification across dry-run and commit.

### A4 — [MEDIUM] Import hardcodes country `"KE"` and discards address fields
- **File:** [ImportCommands.cs:135](src/backend/libraries/VoltsCRM.Application/Features/Import/ImportCommands.cs#L135).
- **Issue:** commit builds `Location` with country `"KE"`, empty street/region, and `Gender.Unknown`; conflicts with the project's "generic, non-locale-specific" domain rule, and the `Mapping` parameter is accepted but unused.
- **Fix:** drive country and column-to-field mapping from the (already-present) `Mapping` input; default country via config, not a literal.

### A5 — [MEDIUM] Import loads the entire `AccountNumber` set into memory + dry-run/commit race
- **File:** [ImportCommands.cs:41](src/backend/libraries/VoltsCRM.Application/Features/Import/ImportCommands.cs#L41) and `:107`.
- **Issue:** both handlers do `db.Customers.Select(c => c.AccountNumber).ToListAsync()`. Won't scale, and a customer created between dry-run and commit isn't reflected.
- **Fix:** query only the batch's account numbers (`Where(c => batch.Contains(c.AccountNumber))`), and rely on a unique constraint + catch as the authoritative dedupe at commit.

### A6 — [LOW] Stale allocation window on the pending→complete path
- **File:** [RecordPaymentCommand.cs](src/backend/libraries/VoltsCRM.Application/Features/Payments/RecordPaymentCommand.cs), [PaymentLifecycleCommands.cs](src/backend/libraries/VoltsCRM.Application/Features/Payments/PaymentLifecycleCommands.cs).
- **Issue:** allocations are validated at record time but applied at completion; the target invoice could change state in between. Fine for cash auto-complete; relevant for the gateway/pending flow.
- **Fix:** re-validate allocations inside the completion handler (ties into A2), or document the constraint.

### A7 — [CLEANUP] Worker is still the dotnet template
- **Files:** [Worker.cs](src/backend/worker/VoltsCRM.Worker/Worker.cs) (logs every 1s), [Program.cs](src/backend/worker/VoltsCRM.Worker/Program.cs).
- **Issue:** not wired to Application/Infrastructure; blocks recurring invoicing, overdue sweep, auto-debit, notifications (see P1/P2). This is the single largest functional gap.
- **Fix:** host the Application/Infrastructure DI, then implement the jobs listed under "Worker" below.

### A8 — [VERIFY] `ListDiscountGrantsQuery` customer-name enrichment
- **File:** [ListDiscountGrantsQuery.cs](src/backend/libraries/VoltsCRM.Application/Features/Discounts/ListDiscountGrantsQuery.cs).
- **Task:** confirm whether the DTO now joins the customer display name (old TODO said the UI shows a truncated GUID). If not, project the name and remove the UI follow-up.

### ~~A9 — [HIGH — live 500s] Unmapped computed `Invoice.Balance`/`AmountDue` break server-side queries~~ ✅ FIXED
`GET /api/reports/dashboard-summary`, `/api/reports/aging`, and `/api/portal/me/summary` return 500:
`Invoice.Balance`/`AmountDue` are get-only computed props (`Ignore`d in EF
[config](src/backend/libraries/VoltsCRM.Infrastructure/Persistence/Configurations/Billing/InvoiceConfiguration.cs#L19-L20)),
so server-side LINQ over them can't translate. Fix = rewrite those three handlers' server-side LINQ to
use mapped columns (`GrossAmount - DiscountAmount - AmountPaid`); no migration.

**Applied:** Replaced `i.Balance` with `(i.GrossAmount - i.DiscountAmount - i.AmountPaid)` in:
- `DashboardSummaryQuery.cs` (outstanding sum + overdue count)
- `AgingReportQuery.cs` (where + select)
- `PortalSummaryQuery.cs` (select projection)

Integration tests added: `InvoiceBalanceQueryTests.cs`

---

## 🟢 Product review (product-analyst)

### P1 — [HIGH, sequencing] Elevate the Worker — it is the billing value loop
- **Why:** recurring invoice generation + overdue sweep are what make this a *billing* system rather than a manual ledger. They are currently unbuilt (A7) and listed last, below token vending and extra adapters. North-star metrics (collection rate, overdue %, DSO) are uncomputable without them.
- **Action:** re-prioritise Worker (recurring invoicing = Phase 10 job; overdue sweep) **above** Phases 15/20.
- **Success criteria:** invoices auto-generate monthly for each active subscription (idempotent — reuse `GenerateInvoicesCommand`); a daily sweep flips past-due invoices/installments to overdue (endpoints already exist — A-side just needs scheduling).

### P2 — [HIGH] No customer notifications (receipts, due reminders, overdue notices)
- **Why:** reminders are a direct lever on collection rate / DSO — the core commercial metric. The plumbing exists ([IEmailSender](src/backend/libraries/VoltsCRM.Application/Common/Interfaces/IEmailSender.cs) + SES wiring in [DependencyInjection.cs:55](src/backend/libraries/VoltsCRM.Infrastructure/DependencyInjection.cs#L55)) but no jobs/templates.
- **Action:** define the MVP notification set (receipt on payment, due reminder N days before, overdue notice) and instrument open/click if feasible. Build as Worker jobs.
- **Success criteria:** measurable reduction in overdue % vs. a no-reminder baseline.

### P3 — [LAUNCH-CRITICAL] Ship the first *real* payment gateway
- **Why:** framework + stubs exist but every adapter is fake (S2). The product value (and the M-Pesa/Stripe domain intent) requires ≥1 real integration with webhook + reconciliation. Confirmed launch-critical for v1.
- ⚠️ **BLOCKED — first provider undecided.** This decision gates the whole Phase 9 build and portal self-service payment. Resolve it first.
- **Action:** decide the first target (the KE/KPLC signals in the code hint at M-Pesa, but this is the open decision — do not assume), then implement adapter + webhook (S2) + status reconciliation. Depends on S3 (secret storage).

### P4 — [MEDIUM] Define success metrics / north star + instrumentation
- **Why:** no phase has measurable acceptance criteria; "done" is currently "code merged." Open product question #7.
- **Action:** define and instrument: % of billing on-system vs. spreadsheet, collection rate, overdue %, DSO, time-to-onboard a customer. The Reports module supplies lagging views; add the events/queries for leading indicators. Each future work item should carry a baseline → target.

### P5 — [MEDIUM] Treat CSV import as an onboarding (possibly launch-blocking) capability
- **Why:** it is the migration path off the legacy spreadsheet (open product question #5). Functionally built (rows API) but needs UI wiring + hardening (S9, A3–A5).
- **Open question to resolve:** import currently creates **Customers only** (name/phone/city/amount). Must legacy **balances, subscriptions, or plans** also migrate? If yes, scope an extended importer — this likely moves import earlier in the plan.

### P6 — [MEDIUM] Negotiated-pricing governance
- **Why:** `CustomerSubscription.NegotiatedPrice` lets a field agent set an arbitrary price with no approval/audit beyond `CreatedById` — a revenue-leakage / fraud surface on a billing system. Open product question #6.
- **Action:** decide whether negotiated price needs an approval workflow, a min/max guardrail, or just an audit trail; spec accordingly.

### P7 — [LOW] Server-side statement / invoice / receipt PDF export
- **Why:** report export is client-side CSV today; PDF statements and receipts are needed for the portal and for P2 notifications. (Already in the Reports list — reframed with rationale.)

### P8 — [LOW] Portal MVP scope decision
- **Why:** open product question #4 — read-only (view bills/balance) vs. self-service payment at launch. Read APIs already exist; self-service payment depends on P3.

---

## Completed (verified 2026-06-18)

- [x] 1 — Solution scaffold + domain model + value objects
- [x] 2 — EF Core + PostgreSQL migrations + seeding
- [x] 3 — Identity + JWT auth (RBAC + admin/agent/portal areas)
- [x] 4 — Inventory CRUD + stock movements
- [x] 5 — Service Plan catalogue + line-items
- [x] 6 — Customer CRUD + service locations (`Location` value object + map picker)
- [x] 7 — Subscriptions: lifecycle + deployed items
- [x] 8 — Payments: record + complete/fail/reverse **+ allocation + single-payment discount** *(over-allocation guard A2, concurrency handling A1 outstanding)*
- [x] 10 — Invoices: generate, list, detail, line items, payment account, **mark-overdue endpoint**
- [x] 11 — Discount grants: grant, revoke, list, **apply-on-payment** *(expire-on-read/scheduled still open)*
- [x] 12 — Installment plans: create, list, detail, **mark-overdue endpoint**
- [x] 13 — Reports: dashboard summary, collections, aging, customer statement
- [x] 14 — Geocoding search **+ `customers/geo` endpoint** *(agents/geo + routing assignment open)*
- [x] 18 — Portal read APIs: summary, invoices, subscriptions, payments *(IDOR-safe: scoped to token's own `customerId`)*

---

## Backend  *(Claude Code)* — remaining

### Phase 9 — Payment gateway adapter framework
- [x] `IPaymentGateway` abstraction + keyed-service registration
- [x] **Config-driven gateway registry** — `PaymentGatewayConfig` (`keyName`/`displayName`/`visibility`/`data` jsonb);
  a gateway is offered only when **implemented ∩ visible** via `IPaymentGatewayCatalog`
  ([PaymentGatewayCatalog.cs](src/backend/libraries/VoltsCRM.Infrastructure/Integrations/PaymentGatewayCatalog.cs))
- [x] First **real** adapter — first-party **no-op `voltspayments`** gateway (seeded visible by default);
  full initiate → reconcile/webhook → complete works end-to-end with no external dependency
  ([VoltspaymentsGateway.cs](src/backend/libraries/VoltsCRM.Infrastructure/Integrations/VoltspaymentsGateway.cs))
- [x] Webhook endpoint with **real** signature validation + startup guard — see **S2**
  ([WebhookEndpoints.cs](src/backend/api/VoltsCRM.API/Endpoints/WebhookEndpoints.cs),
  [WebhookSignature.cs](src/backend/libraries/VoltsCRM.Infrastructure/Integrations/WebhookSignature.cs))
- [~] Secret storage for gateway credentials — webhook secret now masked/write-only + injected from config
  (not baked in source); column encryption still open — see **S3**
- [ ] *(no longer blocking)* Real M-Pesa/Stripe adapter — drop-in: implement `IPaymentGateway`, remove the
  `IStubGateway` marker, add a config row. Provider decision is now a business choice, not a build blocker (**P3**)

### Phase 10 / Billing — remaining
- [ ] **A1** — `DbUpdateConcurrencyException` handling on balance writes (+409 mapping)
- [ ] **A2** — over-allocation guard
- [ ] Recurring monthly invoice generation (Worker — **P1**)

### Phase 11 — Discounts — remaining
- [ ] Expire grants (scheduled or on-read)

### Phase 14 — Map view + GPS routing — remaining
- [ ] `field-agents/geo` (by bounds) + field-agent routing assignment endpoint
- [ ] **S11** — verify/secure agent object-level authz

### Phase 15 — Token vending — remaining
- [~] `ITokenVendingPlatform` + keyed services + settings API *(done; KPLC stub)*
- [ ] Vend endpoint + vending history; first **real** platform adapter

### Phase 16 — Auto-debit (backend portion) — remaining
- [ ] Auto-debit **mandate** entity + CRUD (settings exist; mandates do not)
- [ ] Scheduler in Worker (**P1**)

### Phase 17 — CSV import — hardening
- [ ] **S9** size/row cap · **A3** shared validation · **A4** mapping/country · **A5** scalable dedupe
- [ ] Decide extended scope (balances/subscriptions) — see **P5**

### Phase 18 — Customer portal — remaining
- [x] **Read-only enriched profile** — ✅ done & verified 2026-06-18 (build clean; `PortalProfileTests`
  4/4 green; frontend `tsc -b`+build clean). `18-BE-1..4` + `18-FE-1..4` complete.
- [x] **Self-service payment (18b)** — ✅ done & verified 2026-06-20. `GET /api/portal/me/gateways` +
  `POST /api/portal/me/payments` (IDOR-safe, customer-scoped); pays an invoice's outstanding balance via a
  chosen visible gateway, completing inline through the existing `CompletePaymentCommand` path. Integration
  tests green (`PortalPaymentTests`). FE Pay button still to wire (Cursor).

### Phase 19 — Infrastructure & CI/CD
- [ ] Terraform AWS (ECS Fargate, RDS, ElastiCache, S3, ALB, CloudFront, SES, SNS+SQS, SSM Parameter Store)
  - Add `Payments__Voltspayments__WebhookSecret` (and future gateway secrets) as an SSM Parameter
    Store SecureString + ECS injection, exactly like `Seed__HmacKey` (dev value is in docker-compose).
- [ ] `production.tfvars` + per-environment config
- [ ] CI/CD pipeline (build, test, migrate, deploy) — ensure S1 seeding is prod-safe

### Phase 20 — Additional adapters
- [ ] More `IPaymentGateway` / `ITokenVendingPlatform` real implementations

### Cross-cutting / cleanup
- [ ] **S1–S10** security items above
- [x] **A9** *(HIGH — live 500s)* — fixed: rewrote the 3 handlers (dashboard-summary, aging, portal summary) to use mapped columns (`GrossAmount - DiscountAmount - AmountPaid`); `InvoiceBalanceQueryTests` added
- [ ] **A3–A8** architecture items above
- [ ] Reports: server-side PDF export (**P7**)
- [ ] Integration tests: admin provisioning *(needs Docker)* + **S11** security regression suite
- [ ] **A8** — verify/enrich `ListDiscountGrantsQuery` customer name
- [x] Refresh stale handoff docs — reconciled 2026-06-20; payments UI handoff captured in
  [CURSOR-UI-BRIEF-payments.md](CURSOR-UI-BRIEF-payments.md)

---

## Worker  *(Claude Code — `VoltsCRM.Worker`)* — see **A7 / P1 / P2**

- [ ] **A7** — wire Worker to Application/Infrastructure (currently the dotnet template)
- [ ] **P1** — recurring invoice generation (monthly per active subscription; idempotent)
- [ ] **P1** — overdue sweep (call existing mark-overdue commands on a schedule)
- [ ] **P2** — notifications: receipts, due reminders, overdue notices (SES/SNS)
- [ ] **Phase 16** — auto-debit scheduler via `IPaymentGateway`
- [ ] (Optional) SQS / outbox for gateway webhooks + async work

---

## UI  *(Cursor)* — follow-ups (no backend blocker unless noted)

- [ ] Payment allocation rows on record-payment form *(backend ready — allocation input exists)*
- [ ] Wire CSV import wizard to `/api/import/*` *(backend ready; pairs with S9/A3–A5)*
- [ ] **Payments UI** — gateway registry list + portal self-service payment (full spec below) *(backend ready)*
- [ ] Wire settings shells (auto-debit, token vending) to the existing `/api/settings/*` *(backend ready)*
- [ ] Agent map: real GPS pins from `customers/geo` *(ready)*; field-agent pins once `field-agents/geo` lands
- [ ] Installment plan **create** page (`POST /api/installment-plans` exists)
- [ ] Subscription **edit** route
- [ ] Agent home KPIs *(backend `GetAgentKpisQuery` exists — verify endpoint, then wire)*
- [ ] Enrich discount list with customer name once **A8** confirmed

---

## 📦 Payments UI spec *(Cursor)* — apply-ready

Backend ✅ live & tested (2026-06-20); no backend changes needed. Follow the existing vertical-slice
pattern (`features/<area>/api/{types,*Api,queries}.ts` + `routes/*Page.tsx`); reuse the `get/put/post`
helpers and React Query key factories. `npm run build` (tsc) must stay clean. Canonical example slice:
`src/frontend/web/src/features/serviceplans/api/*`.

### Part 1 — Admin: payment-gateway registry list (`features/settings`)
Replaces the old single-form page (provider/merchantId/publicKey). Model is now **one row per gateway**,
keyed by unique `keyName`, with an admin visibility toggle + free-form `data` map. A gateway can be made
**visible only if `implemented: true`** (an adapter exists). At launch the only implemented gateway is
**`voltspayments`** (seeded visible).

API (admin `settings.manage`):
```
GET /api/settings/payment-gateways
  → { keyName, displayName, visibility, implemented, data: Record<string,string> }[]   // secret values masked "••••••••"
PUT /api/settings/payment-gateways/{keyName}
  body { displayName, visibility, data: Record<string,string> } → config
  // write-only secrets: a value left as "••••••••" keeps the stored secret; a new value overwrites.
  // making a not-implemented gateway visible → 400.
PUT /api/settings/payment-gateways/{keyName}/visibility  body { visible } → config
```
Tasks: replace `PaymentGatewaySettings` types with `PaymentGatewayConfig`/`UpsertPaymentGatewayConfig`;
`paymentGateways.{list,upsert,setVisibility}` in `settingsApi`; `usePaymentGateways` + upsert/visibility
mutations (invalidate list). Page: one row per gateway with displayName, mono `keyName`, an **Implemented**
badge, a visibility switch (disable turning ON when `!implemented`, tooltip; surface the 400), and an Edit
form for `displayName` + a key/value `data` editor. Secret fields prefill with `"••••••••"`; untouched →
send back as-is; new value → overwrites. (Secret heuristic: key name contains `secret`/`key`/`password`/
`token`.) `voltspayments` needs no `data` for basic use.

### Part 2 — Customer portal: self-service payment (`features/portal`)
Add paying an invoice's outstanding balance with a chosen visible gateway. With `voltspayments` the payment
completes instantly; the response also carries an optional `checkoutUrl` for future redirect-style gateways.

API (customer token; IDOR-safe):
```
GET  /api/portal/me/gateways → { keyName, displayName }[]   // only visible ∩ implemented
POST /api/portal/me/payments
  body { invoiceId?, amount?, gatewayKey } → { paymentId, status, checkoutUrl|null }   // invoiceId pays its outstanding balance
```
Tasks: add `AvailableGateway` + `InitiatePaymentResult` types; `gateways()` + `pay()` in `portalApi`;
`usePortalGateways` + `usePayInvoice` mutation (on success invalidate portal `invoices`/`summary`/
`payments`). UI: **Pay** button on each unpaid invoice (and/or the summary outstanding-balance card) →
dialog showing amount due + gateway picker (preselect if one; "no methods available" if zero) → on confirm
`pay({ invoiceId, gatewayKey })`; if `checkoutUrl` returned, redirect, else show success and let the
invalidated queries refresh. Disable button while in-flight; hide it when no outstanding balance; surface
server validation errors.

### Acceptance
- Settings: list renders; visibility toggles persist; Implemented badge matches API; can't enable an
  unimplemented gateway; secret round-trips without being blanked; `tsc`/build clean.
- Portal: a customer pays an unpaid invoice with `voltspayments` → invoice goes paid and balance/history
  refresh without manual reload; `tsc`/build clean.
