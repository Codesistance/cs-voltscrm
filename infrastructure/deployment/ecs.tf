resource "aws_ecs_cluster" "main" {
  name = local.prefix

  setting {
    name  = "containerInsights"
    value = "enabled"
  }
}

# ── IAM roles ─────────────────────────────────────────────────────────────────

data "aws_iam_policy_document" "ecs_assume" {
  statement {
    actions = ["sts:AssumeRole"]
    principals {
      type        = "Service"
      identifiers = ["ecs-tasks.amazonaws.com"]
    }
  }
}

resource "aws_iam_role" "ecs_execution" {
  name               = "${local.prefix}-ecs-execution"
  assume_role_policy = data.aws_iam_policy_document.ecs_assume.json
}

resource "aws_iam_role_policy_attachment" "ecs_execution" {
  role       = aws_iam_role.ecs_execution.name
  policy_arn = "arn:aws:iam::aws:policy/service-role/AmazonECSTaskExecutionRolePolicy"
}

# The execution role (not the task role) injects `secrets` valueFrom into containers at launch,
# so it needs GetParameters on the SSM parameters referenced by the task definitions below.
# SecureString parameters use the default `alias/aws/ssm` KMS key, which the ECS agent can decrypt
# without an explicit kms:Decrypt grant.
resource "aws_iam_role_policy" "ecs_execution_secrets" {
  name = "secrets-injection"
  role = aws_iam_role.ecs_execution.id
  policy = jsonencode({
    Version = "2012-10-17"
    Statement = [{
      Effect = "Allow"
      Action = ["ssm:GetParameters"]
      Resource = [
        aws_ssm_parameter.db_connection_string.arn,
        aws_ssm_parameter.jwt_key.arn,
        aws_ssm_parameter.seed_hmac_key.arn,
      ]
    }]
  })
}

resource "aws_iam_role" "ecs_task" {
  name               = "${local.prefix}-ecs-task"
  assume_role_policy = data.aws_iam_policy_document.ecs_assume.json
}

resource "aws_iam_role_policy" "ecs_task_secrets" {
  name = "secrets-access"
  role = aws_iam_role.ecs_task.id
  policy = jsonencode({
    Version = "2012-10-17"
    Statement = [{
      Effect = "Allow"
      Action = [
        "ssm:GetParameter",
        "ssm:GetParameters",
      ]
      Resource = [
        aws_ssm_parameter.db_connection_string.arn,
        aws_ssm_parameter.jwt_key.arn,
        aws_ssm_parameter.seed_hmac_key.arn,
      ]
    }]
  })
}

resource "aws_iam_role_policy" "ecs_task_s3" {
  name = "s3-access"
  role = aws_iam_role.ecs_task.id
  policy = jsonencode({
    Version = "2012-10-17"
    Statement = [{
      Effect   = "Allow"
      Action   = ["s3:GetObject", "s3:PutObject", "s3:DeleteObject"]
      Resource = "${aws_s3_bucket.assets.arn}/*"
    }]
  })
}

# Messaging + email access for the Worker (notifications) and API.
resource "aws_iam_role_policy" "ecs_task_messaging" {
  name = "messaging-access"
  role = aws_iam_role.ecs_task.id
  policy = jsonencode({
    Version = "2012-10-17"
    Statement = [
      {
        Effect   = "Allow"
        Action   = ["ses:SendEmail", "ses:SendRawEmail"]
        Resource = "*"
      },
      {
        Effect   = "Allow"
        Action   = ["sns:Publish"]
        Resource = aws_sns_topic.notifications.arn
      },
      {
        Effect = "Allow"
        Action = [
          "sqs:SendMessage",
          "sqs:ReceiveMessage",
          "sqs:DeleteMessage",
          "sqs:GetQueueAttributes",
        ]
        Resource = aws_sqs_queue.work.arn
      },
    ]
  })
}

# ── CloudWatch log groups ─────────────────────────────────────────────────────

resource "aws_cloudwatch_log_group" "api" {
  name              = "/ecs/${local.prefix}/api"
  retention_in_days = 30
}

resource "aws_cloudwatch_log_group" "worker" {
  name              = "/ecs/${local.prefix}/worker"
  retention_in_days = 30
}

# ── API task definition ───────────────────────────────────────────────────────

resource "aws_ecs_task_definition" "api" {
  family                   = "${local.prefix}-api"
  network_mode             = "awsvpc"
  requires_compatibilities = ["FARGATE"]
  cpu                      = var.api_cpu
  memory                   = var.api_memory
  execution_role_arn       = aws_iam_role.ecs_execution.arn
  task_role_arn            = aws_iam_role.ecs_task.arn

  container_definitions = jsonencode([{
    name         = "api"
    image        = "${aws_ecr_repository.api.repository_url}:${var.api_image_tag}"
    portMappings = [{ containerPort = 8080, protocol = "tcp" }]

    environment = [
      { name = "ASPNETCORE_ENVIRONMENT", value = "Production" },
      { name = "ASPNETCORE_URLS", value = "http://+:8080" },
      { name = "Jwt__Issuer", value = var.jwt_issuer },
      { name = "Jwt__Audience", value = var.jwt_audience },
      { name = "Jwt__AccessTokenExpiryMinutes", value = tostring(var.jwt_access_expiry_minutes) },
      { name = "Jwt__RefreshTokenExpiryDays", value = tostring(var.jwt_refresh_expiry_days) },
      { name = "Cors__AllowedOrigins", value = var.cors_allowed_origins },
      { name = "Aws__Region", value = var.aws_region },
      { name = "Aws__AssetsBucket", value = aws_s3_bucket.assets.bucket },
      { name = "Redis__Configuration", value = "${aws_elasticache_replication_group.main.primary_endpoint_address}:6379" },
      { name = "Aws__Ses__FromAddress", value = var.ses_from_address },
      { name = "Aws__Sns__TopicArn", value = aws_sns_topic.notifications.arn },
      { name = "Aws__Sqs__QueueUrl", value = aws_sqs_queue.work.url },
    ]

    # Secret values are injected by the execution role at launch (see ecs_execution_secrets).
    secrets = [
      { name = "ConnectionStrings__DefaultConnection", valueFrom = aws_ssm_parameter.db_connection_string.arn },
      { name = "Jwt__Key", valueFrom = aws_ssm_parameter.jwt_key.arn },
      { name = "Seed__HmacKey", valueFrom = aws_ssm_parameter.seed_hmac_key.arn },
    ]

    logConfiguration = {
      logDriver = "awslogs"
      options = {
        "awslogs-group"         = aws_cloudwatch_log_group.api.name
        "awslogs-region"        = var.aws_region
        "awslogs-stream-prefix" = "api"
      }
    }

    healthCheck = {
      command     = ["CMD-SHELL", "curl -f http://localhost:8080/health || exit 1"]
      interval    = 30
      timeout     = 5
      retries     = 3
      startPeriod = 60
    }
  }])
}

resource "aws_ecs_service" "api" {
  name            = "${local.prefix}-api"
  cluster         = aws_ecs_cluster.main.id
  task_definition = aws_ecs_task_definition.api.arn
  desired_count   = var.api_desired_count
  launch_type     = "FARGATE"

  network_configuration {
    subnets         = aws_subnet.private[*].id
    security_groups = [aws_security_group.api.id]
  }

  load_balancer {
    target_group_arn = aws_lb_target_group.api.arn
    container_name   = "api"
    container_port   = 8080
  }

  deployment_minimum_healthy_percent = 100
  deployment_maximum_percent         = 200

  depends_on = [aws_lb_listener.https]
}

# ── Worker task definition ────────────────────────────────────────────────────

resource "aws_ecs_task_definition" "worker" {
  family                   = "${local.prefix}-worker"
  network_mode             = "awsvpc"
  requires_compatibilities = ["FARGATE"]
  cpu                      = var.worker_cpu
  memory                   = var.worker_memory
  execution_role_arn       = aws_iam_role.ecs_execution.arn
  task_role_arn            = aws_iam_role.ecs_task.arn

  container_definitions = jsonencode([{
    name  = "worker"
    image = "${aws_ecr_repository.worker.repository_url}:${var.worker_image_tag}"

    environment = [
      { name = "DOTNET_ENVIRONMENT", value = "Production" },
      { name = "Aws__Region", value = var.aws_region },
      { name = "Aws__AssetsBucket", value = aws_s3_bucket.assets.bucket },
      { name = "Redis__Configuration", value = "${aws_elasticache_replication_group.main.primary_endpoint_address}:6379" },
      { name = "Aws__Ses__FromAddress", value = var.ses_from_address },
      { name = "Aws__Sns__TopicArn", value = aws_sns_topic.notifications.arn },
      { name = "Aws__Sqs__QueueUrl", value = aws_sqs_queue.work.url },
    ]

    secrets = [
      { name = "ConnectionStrings__DefaultConnection", valueFrom = aws_ssm_parameter.db_connection_string.arn },
    ]

    logConfiguration = {
      logDriver = "awslogs"
      options = {
        "awslogs-group"         = aws_cloudwatch_log_group.worker.name
        "awslogs-region"        = var.aws_region
        "awslogs-stream-prefix" = "worker"
      }
    }
  }])
}

resource "aws_ecs_service" "worker" {
  name            = "${local.prefix}-worker"
  cluster         = aws_ecs_cluster.main.id
  task_definition = aws_ecs_task_definition.worker.arn
  desired_count   = var.worker_desired_count
  launch_type     = "FARGATE"

  network_configuration {
    subnets         = aws_subnet.private[*].id
    security_groups = [aws_security_group.worker.id]
  }
}
