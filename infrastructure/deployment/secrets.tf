# DB credentials — set the secret value manually or via CI after first apply
resource "aws_secretsmanager_secret" "db" {
  name                    = "${local.prefix}/db"
  recovery_window_in_days = 7
}

resource "aws_secretsmanager_secret_version" "db" {
  secret_id = aws_secretsmanager_secret.db.id
  # `connectionString` is injected into the ECS tasks as ConnectionStrings__DefaultConnection.
  # It can't be templated here (it needs the RDS host, which depends on this secret) — set both
  # `password` and the full `connectionString` manually after the first apply. See infrastructure/README.md.
  secret_string = jsonencode({
    username         = var.db_username
    password         = "REPLACE_AFTER_FIRST_APPLY"
    connectionString = "REPLACE_AFTER_FIRST_APPLY"
  })

  lifecycle {
    ignore_changes = [secret_string]
  }
}

# JWT signing key — rotate via Secrets Manager; never stored in source
resource "aws_secretsmanager_secret" "jwt_key" {
  name                    = "${local.prefix}/jwt-key"
  recovery_window_in_days = 7
}

resource "aws_secretsmanager_secret_version" "jwt_key" {
  secret_id     = aws_secretsmanager_secret.jwt_key.id
  secret_string = "REPLACE_AFTER_FIRST_APPLY"

  lifecycle {
    ignore_changes = [secret_string]
  }
}

# Seed-admin HMAC key — the secret that makes the seeded admin's daily password unguessable.
# Injected into the API task as Seed__HmacKey; rotate via Secrets Manager; never stored in source.
resource "aws_secretsmanager_secret" "seed_hmac_key" {
  name                    = "${local.prefix}/seed-hmac-key"
  recovery_window_in_days = 7
}

resource "aws_secretsmanager_secret_version" "seed_hmac_key" {
  secret_id     = aws_secretsmanager_secret.seed_hmac_key.id
  secret_string = "REPLACE_AFTER_FIRST_APPLY"

  lifecycle {
    ignore_changes = [secret_string]
  }
}
