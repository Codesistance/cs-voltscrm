app_name    = "voltscrm"
environment = "production"
aws_region  = "eu-west-1"

# ── Networking ────────────────────────────────────────────────────────────────

vpc_cidr = "10.0.0.0/16"

# ── Custom domain / TLS ───────────────────────────────────────────────────────
# Single switch for the edge topology:
#   false → no CloudFront (unblocks accounts not yet CloudFront-verified). The SPA is
#           served from an S3 static-website endpoint and the API over plain HTTP on the
#           ALB's auto-generated DNS name. Auth uses a cookie-less (body) refresh.
#           Reach the app at `terraform output spa_url` / `api_base_url`. Bring-up only.
#   true  → CloudFront serves the SPA under app_domain (cloudfront_acm_certificate_arn)
#           and the ALB serves the API over HTTPS (acm_certificate_arn). The ALB cert must
#           be in the ALB's region (eu-west-1); the CloudFront cert must be in us-east-1
#           (a CloudFront requirement). Then CNAME the app/api subdomains at the outputs.

use_custom_domain              = false
acm_certificate_arn            = "" # ALB / API cert (eu-west-1, must cover api_domain) — required when true
cloudfront_acm_certificate_arn = "" # CloudFront / SPA cert (us-east-1)                 — required when true
app_domain                     = "" # SPA host, e.g. app.yourdomain.com                — required when true
api_domain                     = "" # CloudFront→ALB origin host, e.g. api.yourdomain.com — required when true

# ── Container images ──────────────────────────────────────────────────────────

api_image_tag    = "latest"
worker_image_tag = "latest"

# ── ECS sizing ────────────────────────────────────────────────────────────────

api_cpu           = 512
api_memory        = 1024
api_desired_count = 2

worker_cpu           = 256
worker_memory        = 512
worker_desired_count = 1

# ── RDS ───────────────────────────────────────────────────────────────────────

db_instance_class    = "db.t3.small"
db_allocated_storage = 20
db_name              = "voltscrm"
db_username          = "voltscrm"

# ── API configuration ─────────────────────────────────────────────────────────
# Replace with your actual frontend domain once DNS is configured.

cors_allowed_origins      = "https://app.yourdomain.com"
jwt_issuer                = "VoltsCRM"
jwt_audience              = "VoltsCRM"
jwt_access_expiry_minutes = 15
jwt_refresh_expiry_days   = 7

# ── Secrets ───────────────────────────────────────────────────────────────────
# Secrets are SSM Parameter Store SecureString parameters (see deployment/secrets.tf).
# Set their values after the first apply — see infrastructure/README.md. Nothing to set here.

# ── CloudFront ────────────────────────────────────────────────────────────────

cloudfront_price_class = "PriceClass_100"

# ── ElastiCache Redis ─────────────────────────────────────────────────────────

redis_node_type          = "cache.t3.micro"
redis_num_cache_clusters = 1

# ── SES ───────────────────────────────────────────────────────────────────────
# Verify the domain, then publish the TXT + DKIM CNAME records (terraform output) to DNS.

ses_domain       = "yourdomain.com"
ses_from_address = "no-reply@yourdomain.com"
