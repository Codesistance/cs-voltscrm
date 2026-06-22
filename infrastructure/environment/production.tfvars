app_name    = "voltscrm"
environment = "production"
aws_region  = "us-east-1"

# ── Networking ────────────────────────────────────────────────────────────────

vpc_cidr = "10.0.0.0/16"

# ── TLS ───────────────────────────────────────────────────────────────────────
# Issue an ACM certificate for your domain (must be in us-east-1 for ALB)
# then paste the ARN here. Optional: leave empty to serve the API over plain
# HTTP on port 80 (no HTTPS listener) — for bring-up only, not production traffic.

acm_certificate_arn = ""

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

# ── CloudFront ────────────────────────────────────────────────────────────────

cloudfront_price_class = "PriceClass_100"

# ── ElastiCache Redis ─────────────────────────────────────────────────────────

redis_node_type          = "cache.t3.micro"
redis_num_cache_clusters = 1

# ── SES ───────────────────────────────────────────────────────────────────────
# Verify the domain, then publish the TXT + DKIM CNAME records (terraform output) to DNS.

ses_domain       = "yourdomain.com"
ses_from_address = "no-reply@yourdomain.com"
