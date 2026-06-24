output "alb_dns_name" {
  description = "DNS name of the API load balancer — point your API subdomain CNAME here (custom-domain branch)"
  value       = aws_lb.api.dns_name
}

output "use_custom_domain" {
  description = "Whether the custom-domain edge (CloudFront + ALB HTTPS) is provisioned"
  value       = var.use_custom_domain
}

output "spa_url" {
  description = "Public URL of the SPA — CloudFront/app domain (custom branch) or the S3 static-website endpoint (auto-FQDN branch)"
  value       = var.use_custom_domain ? "https://${var.app_domain}" : "http://${one(aws_s3_bucket_website_configuration.spa[*].website_endpoint)}"
}

output "cloudfront_domain" {
  description = "CloudFront distribution domain (e.g. d123.cloudfront.net) — CNAME app_domain here. Null in the auto-FQDN branch."
  value       = one(aws_cloudfront_distribution.spa[*].domain_name)
}

output "api_base_url" {
  description = "API base the SPA should call (VITE_API_BASE). Custom branch keeps the original same-origin relative /api; auto-FQDN branch uses the ALB DNS name over HTTP cross-origin."
  value       = var.use_custom_domain ? "/api" : "http://${aws_lb.api.dns_name}/api"
}

output "ecr_api_url" {
  description = "ECR repository URL for the API image"
  value       = aws_ecr_repository.api.repository_url
}

output "ecr_worker_url" {
  description = "ECR repository URL for the Worker image"
  value       = aws_ecr_repository.worker.repository_url
}

output "rds_endpoint" {
  description = "RDS PostgreSQL endpoint"
  value       = aws_db_instance.main.address
  sensitive   = true
}

output "db_password_parameter_arn" {
  description = "SSM Parameter Store ARN for the DB master password"
  value       = aws_ssm_parameter.db_password.arn
}

output "db_connection_string_parameter_arn" {
  description = "SSM Parameter Store ARN for the DB connection string"
  value       = aws_ssm_parameter.db_connection_string.arn
}

output "jwt_key_parameter_arn" {
  description = "SSM Parameter Store ARN for the JWT signing key"
  value       = aws_ssm_parameter.jwt_key.arn
}

output "seed_hmac_key_parameter_arn" {
  description = "SSM Parameter Store ARN for the seed-admin HMAC key"
  value       = aws_ssm_parameter.seed_hmac_key.arn
}

output "assets_bucket" {
  description = "S3 bucket name for uploaded assets"
  value       = aws_s3_bucket.assets.bucket
}

output "spa_bucket" {
  description = "S3 bucket name for the SPA build artifacts"
  value       = aws_s3_bucket.spa.bucket
}

output "cloudfront_distribution_id" {
  description = "CloudFront distribution ID — used by the deploy pipeline to create invalidations. Null in the auto-FQDN branch (no CloudFront)."
  value       = one(aws_cloudfront_distribution.spa[*].id)
}

# ── Consumed by the deploy pipeline's migration run-task ───────────────────────

output "ecs_cluster_name" {
  description = "ECS cluster name"
  value       = aws_ecs_cluster.main.name
}

output "api_task_definition_family" {
  description = "API task definition family — used to launch the one-off migrate task"
  value       = aws_ecs_task_definition.api.family
}

output "private_subnet_ids" {
  description = "Private subnet IDs for the migrate run-task network config"
  value       = aws_subnet.private[*].id
}

output "api_security_group_id" {
  description = "API security group ID for the migrate run-task network config"
  value       = aws_security_group.api.id
}

output "redis_endpoint" {
  description = "ElastiCache Redis primary endpoint"
  value       = aws_elasticache_replication_group.main.primary_endpoint_address
}

output "sns_topic_arn" {
  description = "SNS notifications topic ARN"
  value       = aws_sns_topic.notifications.arn
}

output "sqs_queue_url" {
  description = "SQS work queue URL"
  value       = aws_sqs_queue.work.url
}

# Publish these to your external DNS to verify the SES domain and enable DKIM signing.
output "ses_verification_token" {
  description = "Add as a TXT record at _amazonses.<domain> to verify the SES domain"
  value       = aws_ses_domain_identity.main.verification_token
}

output "ses_dkim_tokens" {
  description = "For each token, add a CNAME <token>._domainkey.<domain> → <token>.dkim.amazonses.com"
  value       = aws_ses_domain_dkim.main.dkim_tokens
}
