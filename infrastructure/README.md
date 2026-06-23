# VoltsCRM Infrastructure

Terraform (AWS) + GitHub Actions for the VoltsCRM API, Worker, and React SPA.

| Layer | What |
|---|---|
| Compute | ECS Fargate — `api` (behind ALB) and `worker` services |
| Data | RDS PostgreSQL 16, ElastiCache Redis |
| Edge | CloudFront + S3 (SPA), ALB (API), ACM TLS |
| Messaging | SNS topic → SQS queue (+ DLQ), SES for email |
| Secrets | SSM Parameter Store SecureString (DB password + connection string, JWT signing key, seed HMAC key) |
| Registry | ECR (`-api`, `-worker`) |

Terraform lives in [deployment/](deployment/); environment values in
[environment/production.tfvars](environment/production.tfvars). State is in S3 with a DynamoDB lock
table (configured via `-backend-config`, see below).

## First-time bootstrap (manual, one-off)

These steps create things Terraform can't bootstrap itself or that depend on external DNS.

1. **Remote state backend.** Create an S3 bucket (versioned) and a DynamoDB lock table
   (`LockID` string hash key). Pass them to `terraform init`:
   ```bash
   cd infrastructure/deployment
   terraform init \
     -backend-config="bucket=<state-bucket>" \
     -backend-config="dynamodb_table=<lock-table>" \
     -backend-config="region=us-east-1"
   ```

2. **TLS certificate.** Request an ACM certificate **in `us-east-1`** for the API + app domains,
   validate it, and set `acm_certificate_arn` in `production.tfvars`. Also set
   `cors_allowed_origins`, `ses_domain`, and `ses_from_address`.

   > `acm_certificate_arn` is optional (defaults to `""`). When set, the ALB serves the API over
   > HTTPS on port 443 and redirects port 80 to it. When left empty, no HTTPS listener is created
   > and port 80 forwards straight to the API target group — useful for bringing the stack up before
   > a cert is issued, but **don't run production traffic over plain HTTP**.

3. **First apply.**
   ```bash
   terraform apply -var-file=../environment/production.tfvars
   ```
   The `db/password`, `db/connection-string`, `jwt-key`, and `seed-hmac-key` SSM SecureString
   parameters are created with `REPLACE_AFTER_FIRST_APPLY` placeholders. Terraform ignores later
   changes to their values (`ignore_changes = [value]`), so set the real values out-of-band as
   below. SSM has no deletion-recovery window, so a `terraform destroy` + re-apply is never blocked
   by a name still "scheduled for deletion".

4. **Set the real secret values.** After RDS exists, read its endpoint
   (`terraform output rds_endpoint`) and overwrite each parameter:
   ```bash
   # DB master password (sourced by RDS on create — see rds.tf).
   aws ssm put-parameter --overwrite --type SecureString \
     --name /voltscrm-production/db/password --value "<strong-password>"

   # Full DB connection string injected into the ECS tasks. Keep the password here in sync
   # with the master password above.
   aws ssm put-parameter --overwrite --type SecureString \
     --name /voltscrm-production/db/connection-string \
     --value "Host=<rds_endpoint>;Port=5432;Database=voltscrm;Username=voltscrm;Password=<strong-password>"

   # JWT signing key (>= 32 chars).
   aws ssm put-parameter --overwrite --type SecureString \
     --name /voltscrm-production/jwt-key --value "$(openssl rand -base64 48)"

   # Seed-admin HMAC key (>= 32 chars) — derives the seeded admin's daily password.
   # This is the entropy that keeps that password unguessable; it must not be in source.
   aws ssm put-parameter --overwrite --type SecureString \
     --name /voltscrm-production/seed-hmac-key --value "$(openssl rand -base64 48)"
   ```
   > The RDS master password is sourced from `db/password` on create
   > ([rds.tf](deployment/rds.tf)); keep it in sync with the password embedded in
   > `db/connection-string`.

5. **Verify SES.** Publish the DNS records to your external DNS provider, then wait for SES to
   verify:
   ```bash
   terraform output ses_verification_token   # TXT at _amazonses.<domain>
   terraform output ses_dkim_tokens          # CNAME <token>._domainkey.<domain> -> <token>.dkim.amazonses.com
   ```
   New SES accounts start in the sandbox — request production access to send to unverified
   recipients.

6. **Point DNS.** CNAME the app subdomain at `terraform output cloudfront_domain` and the API
   subdomain at `terraform output alb_dns_name` (DNS is managed externally — no Route 53).

## CI/CD

- **`.github/workflows/ci.yml`** — builds the backend, runs all `tests/**/*.Tests.csproj`
  (integration tests self-host Postgres via Testcontainers), and lints + builds the SPA. Runs on
  every push and PR.
- **`.github/workflows/deploy.yml`** — on push to `main`: builds & pushes the API/Worker images to
  ECR (tag = commit SHA), `terraform apply` (rolls the ECS services), runs DB migrations as a
  one-off Fargate task (`api` container with command `migrate`), then builds the SPA and syncs it to
  S3 + invalidates CloudFront.

### Required GitHub config (set up manually)

AWS auth is intentionally **not** provisioned by this repo. Wire it yourself, then add:

| Kind | Name | Purpose |
|---|---|---|
| Secret | `AWS_DEPLOY_ROLE_ARN` | IAM role assumed by the deploy job (OIDC) |
| Variable | `AWS_REGION` | e.g. `us-east-1` |
| Variable | `TF_BACKEND_BUCKET` | Terraform state bucket |
| Variable | `TF_BACKEND_DYNAMODB_TABLE` | Terraform lock table |

The deploy role needs: ECR push, ECS (register/update/run-task/describe), the Terraform-managed
resources, S3 sync to the SPA bucket, and CloudFront `CreateInvalidation`.

## Migrations

The API image doubles as the migration runner: `dotnet VoltsCRM.API.dll migrate` applies pending EF
Core migrations and seeds baseline data, then exits (see
[Program.cs](../src/backend/api/VoltsCRM.API/Program.cs)). The deploy workflow runs this as a Fargate
task **before** the rolled services finish stabilising, so the schema is current when new tasks
serve traffic. RDS is private, so migrations must run from inside the VPC — never from CI directly.

## Notes

- ElastiCache Redis, SES, and SNS/SQS are provisioned now (full Phase 19 scope) but are not yet
  consumed by application code — the Worker notification/queue phases wire them up later. Connection
  details are passed to the tasks as `Redis__Configuration`, `Aws__Ses__FromAddress`,
  `Aws__Sns__TopicArn`, and `Aws__Sqs__QueueUrl`.
- The repo must be initialised as a git repo with a GitHub remote before Actions can run.
