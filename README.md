# VoltsCRM

A web-based **CRM and billing platform for a hardware provider**. Customers don't buy hardware
items directly — they subscribe to **ServicePlans** (bundles of inventory items + services).
The system tracks customers, subscriptions, inventory, invoicing, payments, discounts and
installment (BNPL) billing, with pluggable payment and token-vending integrations.

> **Status:** Foundation, auth/RBAC, and the full billing module API surface are built. Remaining
> work, the current-state matrix, and priorities are tracked in **[TODO-WORK.md](./TODO-WORK.md)** —
> the single source of truth for what's done and what's next.

---

## What this solves

VoltsCRM replaces partial Excel/spreadsheet records with **one structured, multi-user system** for a
hardware provider's customers and billing. Getting the business off spreadsheets is the umbrella
goal; the platform is built to deliver four outcomes:

- **Billing on-system** — every customer, subscription, invoice and payment recorded in one place
  (vs. scattered spreadsheets).
- **Stronger collections** — invoicing, discounts, installment (BNPL) plans, overdue tracking, and
  (post-launch) automated reminders, to reduce overdue balances / days-sales-outstanding.
- **Field operations** — field agents managing customers, hardware deployments, payment collection,
  and GPS-located service locations.
- **Customer self-service** — a portal where customers view bills/balance and (target) pay online.

---

## Tech stack

| Layer | Technology |
|---|---|
| Backend | ASP.NET Core 9, C#, Minimal APIs, Clean Architecture, MediatR, FluentValidation |
| Frontend | React 19 + TypeScript + Vite + Tailwind CSS v4 (shadcn-style UI), React Router 7, TanStack Query, React Hook Form + Zod, Axios |
| Database | PostgreSQL via EF Core 9 (Npgsql); schemas: `identity`, `crm`, `billing`, `inventory`, `organisation` |
| Auth | ASP.NET Identity + JWT Bearer (15 min access) + refresh tokens (7 days, rotated); user-type + dynamic admin RBAC |
| Logging | Serilog (JSON, console) |
| Infra | Terraform on AWS (ECS Fargate, RDS PostgreSQL, ElastiCache Redis, S3, ALB, CloudFront, SES, SNS+SQS, SSM Parameter Store) — DNS managed externally |
| Maps / Charts | React-Leaflet (agent map), Recharts (report charts) |
| CSV | CsvHelper — customer import API (dry-run + commit) wired |

---

## Repository map

```
VoltsCRM/
├─ README.md                      ← you are here
├─ TODO-WORK.md                   ← remaining work + priorities (source of truth)
├─ VoltsCRM.slnx
├─ src/
│  ├─ backend/
│  │  ├─ libraries/
│  │  │  ├─ VoltsCRM.Domain/          entities, value objects, enums (no deps)
│  │  │  ├─ VoltsCRM.Application/     MediatR handlers, validators, interfaces
│  │  │  └─ VoltsCRM.Infrastructure/  AppDbContext, EF configs, Identity, Migrations, integrations
│  │  ├─ api/VoltsCRM.API/            Minimal-API host, auth, endpoints, seeding
│  │  └─ worker/VoltsCRM.Worker/      background jobs (template only — not yet wired)
│  └─ frontend/web/                   React SPA (see src/frontend/web/README.md)
├─ tests/                             Domain / Application / Integration (domain + integration tests present)
├─ .github/workflows/                 CI (ci.yml) + deploy (deploy.yml)
└─ infrastructure/                    Terraform AWS IaC (scaffolded)
```

---

## Current state

Mapped against the approved 20-phase build plan. Legend: ✅ built (API; UI where applicable) ·
🟡 partial / scaffolded (stub or read-only; wiring pending) · ⬜ not started.

| # | Capability | Status |
|---|---|---|
| 1 | Solution scaffold + domain model (entities, enums, value objects) | ✅ |
| 2 | EF Core + PostgreSQL + migrations (8, through `SeedAdminUser`) + seeding | ✅ |
| 3 | ASP.NET Identity + JWT + refresh rotation + **user-type + dynamic admin RBAC** | ✅ |
| 4 | Inventory item CRUD + stock movements (API + React) | ✅ |
| 5 | ServicePlan catalogue | ✅ |
| 6 | Customer CRUD + service locations | ✅ |
| 7 | Subscription lifecycle + deployed items | ✅ |
| 8 | Payment recording + allocation + complete/fail/reverse | ✅ *(concurrency handling pending — TODO A1)* |
| 9 | Payment-gateway adapter framework + first adapter | 🟡 *(interface + keyed DI + **stub** adapters; real adapter + webhook pending — see v1 scope)* |
| 10 | Invoice generation + prepaid balance + mark-overdue | ✅ *(auto-generation is post-launch — Worker)* |
| 11 | Discount grants (grant / revoke / list / apply-on-payment) | ✅ |
| 12 | Installment plans (BNPL) | ✅ |
| 13 | Reports (dashboard, statement, collection, aging) | ✅ |
| 14 | Map view + GPS field-agent routing | 🟡 *(geocoding + `customers/geo` done; agents geo + routing pending)* |
| 15 | Token-vending adapter framework | 🟡 *(interface + keyed DI + KPLC **stub** + settings; vend endpoint pending)* |
| 16 | Auto-debit scheduler | 🟡 *(settings + API exist; mandate entity + Worker scheduler pending)* |
| 17 | CSV import | 🟡 *(customer dry-run + commit API exist; hardening + UI wiring pending)* |
| 18 | Customer self-service portal | 🟡 *(read APIs done; portal UI + self-service payment = v1 — see below)* |
| 19 | Terraform AWS + CI/CD | 🟡 *(IaC scaffolded; CI `ci.yml` + `deploy.yml` present)* |
| 20 | Additional payment/vending adapters | ⬜ |

**Also outstanding:**
- **Worker** is a template — it does not yet reference Application/Infrastructure and runs no jobs
  (recurring invoicing, overdue sweep, auto-debit, notifications). Post-launch (see v1 scope).
- **Tests** — domain unit tests + integration tests exist (auth flow, permission enforcement,
  self-lockout, access management); coverage is still expanding.

**Frontend:** auth + the admin/field/portal areas and the billing module pages are built; some
settings shells and wizards await their backend wiring (tracked in TODO-WORK).

---

## v1 launch scope

Confirmed launch-critical for v1 (everything else is post-launch backlog):

- **Online payments** — at least one real payment gateway with webhook + reconciliation.
  ⚠️ **Blocking open decision:** the first provider is *undecided*. This gates the Phase 9 build and
  full portal self-service payment — resolve before that work starts. Stub adapters exist today.
- **Customer self-service portal** — portal UI over the existing read APIs, including (target)
  self-service payment (depends on the gateway decision above).
- **Customer CSV import** — onboarding migration, **customer records only** at launch (balances and
  subscriptions are not migrated in v1).

**Post-launch backlog:** automated-billing Worker (recurring invoicing, overdue sweep, reminders) —
at launch, invoices are produced via the manual `POST /api/invoices/generate` trigger, not the
Worker; token vending; extended migration (balances/subscriptions); additional adapters; maps/routing.

---

## Known issues to address

Two live items are tracked in detail (with file references, fix, and acceptance criteria) in
[TODO-WORK.md](./TODO-WORK.md):

- **S1 (CRITICAL) — seeded admin credential.** The seeded `admin@voltscrm.local` password is computed
  per-day from a hardcoded HMAC key in source and re-applied on every `migrate`, with no environment
  gate. Must be made prod-safe before any non-local deployment.
- **A1 (HIGH) — payment concurrency handling.** `PaymentAccount` has an `xmin` concurrency token, but
  no `DbUpdateConcurrencyException` handling/retry exists on the balance write paths, so a conflict
  surfaces as a 500.

---

## Architecture: user-type separation + dynamic admin RBAC (implemented)

The user-type separation + dynamic admin RBAC model is **built** (it replaced the earlier
single-shell, three-static-role model):

- A `UserType` discriminator (`Customer | FieldAgent | Administration`) on the single
  `AspNetUsers` credential store, with 1:1 profile tables per type.
- Dynamic admin RBAC: `AdminRole` / `Permission` / join tables in the `identity` schema; a
  **code-defined permission registry**; `user_type` + `perm` claims in the JWT; a dynamic
  `IAuthorizationPolicyProvider` ([Program.cs](./src/backend/api/VoltsCRM.API/Program.cs)).
- Three self-contained UI areas — `/portal` (Customer), `/field` (FieldAgent), `/admin`
  (Administration) — each with its own shell/nav, plus an admin role-management UI.

---

## How to run (local dev)

Local dev runs **HTTPS-only**: API on `https://localhost:7233`, SPA on `https://localhost:5173`
(Vite proxies `/api` → API).

### Docker (recommended)
```bash
# One-time setup: export dev certificate
dotnet dev-certs https -ep ./certs/aspnetapp.pfx -p devcert
dotnet dev-certs https --trust

# Start everything (PostgreSQL + API with hot reload)
docker-compose up -d

# View logs
docker-compose logs -f api
```
- API available at `https://localhost:7233` (HTTPS) or `http://localhost:5003` (HTTP)
- PostgreSQL on `localhost:5433`
- Hot reload enabled — code changes auto-rebuild

### Backend (without Docker)
```bash
# from repo root
# 1. Provide secrets (not committed) for the API project:
dotnet user-secrets --project src/backend/api/VoltsCRM.API set "Jwt:Key" "<32+ char secret>"
dotnet user-secrets --project src/backend/api/VoltsCRM.API set "Seed:HmacKey" "<32+ char secret>"
dotnet user-secrets --project src/backend/api/VoltsCRM.API set \
  "ConnectionStrings:DefaultConnection" "Host=localhost;Port=5432;Database=voltscrm;Username=postgres;Password=<pwd>"

# 2. Run (migrations + seed run automatically in Development)
dotnet run --project src/backend/api/VoltsCRM.API
```
- `Jwt:Key` and `Seed:HmacKey` are both **required** and validated at startup (each must be ≥ 32
  chars) — see [Program.cs](./src/backend/api/VoltsCRM.API/Program.cs). `Seed:HmacKey` is the secret
  that derives the seeded admin's daily password; in deployed environments it comes from AWS SSM
  Parameter Store (see [infrastructure/README.md](./infrastructure/README.md)).

### Seeded admin login
The seeded admin is `admin@voltscrm.local`. Its password is **computed per day** (not a fixed value).
Print today's password with the CLI command:
```bash
dotnet run --project src/backend/api/VoltsCRM.API seed-password
# optionally for another date: ... seed-password --date ddMMyyyy
```
> ⚠️ The seeded admin is for **local dev only**. It must not be relied on in any non-local
> environment — see issue **S1** above and [TODO-WORK.md](./TODO-WORK.md).

### Frontend
```bash
cd src/frontend/web
npm run certs      # one-time: export the ASP.NET Core dev cert for HTTPS
npm install
npm run dev
```
See [src/frontend/web/README.md](./src/frontend/web/README.md) for details.

---

## Source planning documents

The canonical planning artifacts live outside the repo, in `~/.claude/plans/`:

- `i-want-to-build-logical-quiche.md` — the 18-item code review (foundation hardening).
- `how-are-the-ui-eager-quill.md` — user-type separation + dynamic admin RBAC redesign.
- `plan-the-ui-screens-bubbly-thacker.md` — HTTPS-only local dev setup.

Remaining work and priorities are tracked in [TODO-WORK.md](./TODO-WORK.md).
