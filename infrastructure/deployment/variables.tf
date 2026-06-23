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

# ── API configuration ─────────────────────────────────────────────────────────

variable "cors_allowed_origins" {
  description = "Comma-separated list of allowed CORS origins"
  type        = string
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

# ── TLS ───────────────────────────────────────────────────────────────────────

variable "acm_certificate_arn" {
  description = "ARN of an ACM certificate in us-east-1 for the ALB HTTPS listener. When empty, the ALB serves traffic over plain HTTP on port 80 instead."
  type        = string
  default     = ""
}

# ── Secrets ───────────────────────────────────────────────────────────────────

variable "secrets_recovery_window_in_days" {
  description = "Recovery window for Secrets Manager secrets on destroy. 0 deletes immediately (so a destroy + re-apply isn't blocked by a name still scheduled for deletion); 7–30 keeps a recovery window."
  type        = number
  default     = 0

  validation {
    condition     = var.secrets_recovery_window_in_days == 0 || (var.secrets_recovery_window_in_days >= 7 && var.secrets_recovery_window_in_days <= 30)
    error_message = "secrets_recovery_window_in_days must be 0 or between 7 and 30 (AWS Secrets Manager constraint)."
  }
}

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
