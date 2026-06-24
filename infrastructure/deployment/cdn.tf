resource "aws_s3_bucket" "spa" {
  bucket = "${local.prefix}-spa"
}

# Locked down for the CloudFront/OAC branch; relaxed for the S3-website branch so a
# public-read bucket policy can attach.
resource "aws_s3_bucket_public_access_block" "spa" {
  bucket                  = aws_s3_bucket.spa.id
  block_public_acls       = var.use_custom_domain
  block_public_policy     = var.use_custom_domain
  ignore_public_acls      = var.use_custom_domain
  restrict_public_buckets = var.use_custom_domain
}

resource "aws_s3_bucket" "assets" {
  bucket = "${local.prefix}-assets"
}

resource "aws_s3_bucket_public_access_block" "assets" {
  bucket                  = aws_s3_bucket.assets.id
  block_public_acls       = true
  block_public_policy     = true
  ignore_public_acls      = true
  restrict_public_buckets = true
}

# The SPA bucket is served two ways depending on use_custom_domain:
#   true  → private bucket fronted by CloudFront (OAC) under a custom domain (below).
#   false → S3 static-website hosting, served directly over HTTP (further below).
#
# Everything here uses the default (var.aws_region) provider. CloudFront is a global
# service, so its distribution can be managed from any region, and its ACM certificate
# (cloudfront_acm_certificate_arn) is supplied as an ARN that must be issued in us-east-1.

# ── Custom-domain branch: CloudFront in front of a private SPA bucket ──────────

resource "aws_cloudfront_origin_access_control" "spa" {
  count                             = var.use_custom_domain ? 1 : 0
  name                              = "${local.prefix}-spa"
  origin_access_control_origin_type = "s3"
  signing_behavior                  = "always"
  signing_protocol                  = "sigv4"
}

resource "aws_s3_bucket_policy" "spa_cloudfront" {
  count  = var.use_custom_domain ? 1 : 0
  bucket = aws_s3_bucket.spa.id
  policy = jsonencode({
    Version = "2012-10-17"
    Statement = [{
      Sid    = "AllowCloudFront"
      Effect = "Allow"
      Principal = {
        Service = "cloudfront.amazonaws.com"
      }
      Action   = "s3:GetObject"
      Resource = "${aws_s3_bucket.spa.arn}/*"
      Condition = {
        StringEquals = {
          "AWS:SourceArn" = aws_cloudfront_distribution.spa[0].arn
        }
      }
    }]
  })
}

resource "aws_cloudfront_distribution" "spa" {
  count               = var.use_custom_domain ? 1 : 0
  enabled             = true
  default_root_object = "index.html"
  price_class         = var.cloudfront_price_class
  aliases             = [var.app_domain]

  origin {
    domain_name              = aws_s3_bucket.spa.bucket_regional_domain_name
    origin_id                = "s3-spa"
    origin_access_control_id = aws_cloudfront_origin_access_control.spa[0].id
  }

  # API origin — CloudFront forwards /api/* to the ALB over HTTPS. The origin domain is
  # api_domain (CNAME'd to the ALB) so it matches the ALB's ACM certificate; the viewer
  # only ever sees app_domain, so the SPA's relative /api calls stay same-origin and the
  # secure refresh cookie keeps working.
  origin {
    domain_name = var.api_domain
    origin_id   = "alb-api"

    custom_origin_config {
      http_port              = 80
      https_port             = 443
      origin_protocol_policy = "https-only"
      origin_ssl_protocols   = ["TLSv1.2"]
    }
  }

  default_cache_behavior {
    target_origin_id       = "s3-spa"
    viewer_protocol_policy = "redirect-to-https"
    allowed_methods        = ["GET", "HEAD"]
    cached_methods         = ["GET", "HEAD"]
    compress               = true

    forwarded_values {
      query_string = false
      cookies { forward = "none" }
    }
  }

  # /api/* is dynamic and auth-bearing — pass straight through to the ALB with no caching,
  # forwarding the Authorization header and cookies. Host is left as the origin (api_domain)
  # so it keeps matching the ALB certificate; the API accepts any host (AllowedHosts = "*").
  ordered_cache_behavior {
    path_pattern           = "/api/*"
    target_origin_id       = "alb-api"
    viewer_protocol_policy = "redirect-to-https"
    allowed_methods        = ["GET", "HEAD", "OPTIONS", "PUT", "POST", "PATCH", "DELETE"]
    cached_methods         = ["GET", "HEAD"]
    compress               = true

    min_ttl     = 0
    default_ttl = 0
    max_ttl     = 0

    forwarded_values {
      query_string = true
      headers      = ["Authorization", "Origin"]
      cookies { forward = "all" }
    }
  }

  # SPA fallback — return index.html for unknown paths so React Router handles routing
  custom_error_response {
    error_code         = 403
    response_code      = 200
    response_page_path = "/index.html"
  }

  custom_error_response {
    error_code         = 404
    response_code      = 200
    response_page_path = "/index.html"
  }

  restrictions {
    geo_restriction { restriction_type = "none" }
  }

  viewer_certificate {
    acm_certificate_arn      = var.cloudfront_acm_certificate_arn
    ssl_support_method       = "sni-only"
    minimum_protocol_version = "TLSv1.2_2021"
  }
}

# ── Auto-FQDN branch: S3 static-website hosting, public-read over HTTP ─────────

resource "aws_s3_bucket_website_configuration" "spa" {
  count  = var.use_custom_domain ? 0 : 1
  bucket = aws_s3_bucket.spa.id

  index_document { suffix = "index.html" }

  # SPA fallback — serve index.html for unknown paths so React Router handles routing.
  error_document { key = "index.html" }
}

resource "aws_s3_bucket_policy" "spa_public" {
  count  = var.use_custom_domain ? 0 : 1
  bucket = aws_s3_bucket.spa.id
  policy = jsonencode({
    Version = "2012-10-17"
    Statement = [{
      Sid       = "AllowPublicRead"
      Effect    = "Allow"
      Principal = "*"
      Action    = "s3:GetObject"
      Resource  = "${aws_s3_bucket.spa.arn}/*"
    }]
  })

  # The public-access block must be relaxed before a public bucket policy can attach.
  depends_on = [aws_s3_bucket_public_access_block.spa]
}
