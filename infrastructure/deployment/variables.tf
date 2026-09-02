variable "app_name" {
  description = "Short application name used as a resource prefix"
  type        = string
  default     = "voltscrm"
}

variable "environment" {
  description = "Deployment environment (production)"
  type        = string
}

variable "aws_region" {
  description = "AWS region"
  type        = string
}

# ── Networking ────────────────────────────────────────────────────────────────

variable "vpc_cidr" {
  description = "CIDR block for the VPC"
  type        = string
  default     = "10.0.0.0/16"
}

# A NAT gateway costs ~$32/month plus data processing, and the subnets still span two AZs
# for the ALB and the RDS subnet group. One shared gateway halves that at the cost of the
# second AZ losing egress if the gateway's AZ goes down — acceptable for a dev environment.
variable "single_nat_gateway" {
  description = "When true, route both private subnets through one NAT gateway instead of one per AZ. Set false for AZ-independent egress."
  type        = bool
  default     = true
}

# ── Container images ──────────────────────────────────────────────────────────

# Stable moving tag: the backend deploy workflow pushes this tag and rolls the
# services with force-new-deployment, so Terraform never needs the commit SHA.
variable "api_image_tag" {
  description = "Docker image tag for VoltsCRM.API"
  type        = string
  default     = "production"
}

variable "worker_image_tag" {
  description = "Docker image tag for VoltsCRM.Worker"
  type        = string
  default     = "production"
}

# ── ECS ───────────────────────────────────────────────────────────────────────

variable "api_cpu" {
  description = "vCPU units for the API task (1024 = 1 vCPU)"
  type        = number
  default     = 512
}

variable "api_memory" {
  description = "Memory in MiB for the API task"
  type        = number
  default     = 1024
}

variable "api_desired_count" {
  description = "Desired number of API task replicas"
  type        = number
  default     = 2
}

variable "worker_cpu" {
  description = "vCPU units for the Worker task"
  type        = number
  default     = 256
}

variable "worker_memory" {
  description = "Memory in MiB for the Worker task"
  type        = number
  default     = 512
}

variable "worker_desired_count" {
  description = "Desired number of Worker task replicas"
  type        = number
  default     = 1
}

# ── RDS ───────────────────────────────────────────────────────────────────────

variable "db_instance_class" {
  description = "RDS instance type"
  type        = string
  default     = "db.t3.small"
}

variable "db_allocated_storage" {
  description = "Initial RDS storage in GiB"
  type        = number
  default     = 20
}

variable "db_name" {
  description = "PostgreSQL database name"
  type        = string
  default     = "voltscrm"
}

variable "db_username" {
  description = "PostgreSQL master username"
  type        = string
  default     = "voltscrm"
}

variable "db_backup_retention_days" {
  description = "Days of automated RDS backups to keep (0 disables them)"
  type        = number
  default     = 1
}

variable "db_deletion_protection" {
  description = "Block `terraform destroy` from deleting the database. Turn on for anything holding real data."
  type        = bool
  default     = false
}

variable "db_skip_final_snapshot" {
  description = "Skip the final snapshot when the instance is destroyed. Turn off for anything holding real data."
  type        = bool
  default     = true
}

# ── Observability ─────────────────────────────────────────────────────────────

variable "log_retention_days" {
  description = "CloudWatch retention for the ECS task log groups"
  type        = number
  default     = 7
}

variable "enable_container_insights" {
  description = "ECS Container Insights. Off by default — it bills per custom metric and a dev cluster rarely reads them."
  type        = bool
  default     = false
}

# ── API configuration ─────────────────────────────────────────────────────────

variable "cors_allowed_origins" {
  description = "Allowed CORS origin for the SPA when use_custom_domain = true (e.g. https://app.example.com). When use_custom_domain = false this is ignored and the S3 website endpoint is used instead."
  type        = string
  default     = ""
}

variable "jwt_issuer" {
  description = "JWT token issuer claim"
  type        = string
  default     = "VoltsCRM"
}

variable "jwt_audience" {
  description = "JWT token audience claim"
  type        = string
  default     = "VoltsCRM"
}

variable "jwt_access_expiry_minutes" {
  description = "JWT access token lifetime in minutes"
  type        = number
  default     = 15
}

variable "jwt_refresh_expiry_days" {
  description = "JWT refresh token lifetime in days"
  type        = number
  default     = 7
}

# ── Custom domain / TLS ───────────────────────────────────────────────────────
# Single switch for the edge topology:
#   true  → CloudFront serves the SPA from a private S3 bucket under app_domain
#           (cloudfront_acm_certificate_arn), and the ALB serves the API over HTTPS
#           (acm_certificate_arn). Auth keeps its secure httpOnly refresh cookie.
#   false → no CloudFront. The SPA is served from an S3 static-website endpoint and
#           the API runs over plain HTTP on the ALB's auto-generated DNS name. Auth
#           switches to a cookie-less (body) refresh so it works cross-origin over HTTP.

variable "enable_phoenix" {
  description = "When true, expose the Phoenix super-admin account-recovery page (route /phoenix + the /api/admin/phoenix endpoints). When false, the endpoints are not mapped and the SPA route is not built, so the path does not exist. Even when true the endpoint still requires an authenticated super admin."
  type        = bool
  default     = true
}

variable "use_custom_domain" {
  description = "When true, provision the CloudFront SPA edge + ALB HTTPS under custom domains. When false, serve the SPA from S3 static-website hosting and the API over plain HTTP on the ALB's auto-generated DNS name."
  type        = bool
  default     = false
}

variable "acm_certificate_arn" {
  description = "ARN of an ACM certificate in the ALB's region (var.aws_region, e.g. eu-west-1) for the ALB HTTPS listener (API domain). Required when use_custom_domain = true; ignored otherwise."
  type        = string
  default     = ""
}

variable "cloudfront_acm_certificate_arn" {
  description = "ARN of an ACM certificate in us-east-1 (a CloudFront requirement) for the CloudFront SPA distribution (covers app_domain). Required when use_custom_domain = true; ignored otherwise."
  type        = string
  default     = ""
}

variable "app_domain" {
  description = "Custom domain for the SPA, set as the CloudFront alias (e.g. app.example.com). Required when use_custom_domain = true; ignored otherwise."
  type        = string
  default     = ""
}

variable "api_domain" {
  description = "Hostname for the CloudFront → ALB origin hop (e.g. api.example.com). CNAME it to the ALB and ensure acm_certificate_arn covers it. The SPA still calls the API same-origin at app_domain/api; this name is only the origin hop. Required when use_custom_domain = true; ignored otherwise."
  type        = string
  default     = ""
}

# ── Secrets ───────────────────────────────────────────────────────────────────
# Secrets are SSM Parameter Store SecureString parameters (see secrets.tf). SSM has no
# deletion-recovery window, so there is nothing to configure here.

# ── CloudFront ────────────────────────────────────────────────────────────────

variable "cloudfront_price_class" {
  description = "CloudFront price class (PriceClass_100 = US/Europe only)"
  type        = string
  default     = "PriceClass_100"
}

# ── ElastiCache Redis ─────────────────────────────────────────────────────────

variable "redis_node_type" {
  description = "ElastiCache node type"
  type        = string
  default     = "cache.t3.micro"
}

variable "redis_num_cache_clusters" {
  description = "Number of Redis nodes (>1 enables automatic failover)"
  type        = number
  default     = 1
}

# ── SES ───────────────────────────────────────────────────────────────────────

variable "ses_domain" {
  description = "Domain to verify with SES for sending transactional email"
  type        = string
}

variable "ses_from_address" {
  description = "Default From address for outbound email (must be within ses_domain)"
  type        = string
}
