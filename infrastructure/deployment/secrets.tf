# Secrets are stored as SSM Parameter Store SecureString parameters. Unlike Secrets Manager,
# ECS can't extract a JSON sub-key from a parameter at injection time, so the DB password and the
# full connection string are kept as separate parameters rather than one JSON blob.

# DB master password — set the value manually or via CI after first apply.
# Sourced by RDS as the master password (see rds.tf).
resource "aws_ssm_parameter" "db_password" {
  name  = "/${local.prefix}/db/password"
  type  = "SecureString"
  value = "REPLACE_AFTER_FIRST_APPLY"

  lifecycle {
    ignore_changes = [value]
  }
}

# Full DB connection string injected into ECS tasks as ConnectionStrings__DefaultConnection.
# It needs the RDS host (which depends on this stack), so it can't be templated here — set it
# manually after the first apply. See infrastructure/README.md.
resource "aws_ssm_parameter" "db_connection_string" {
  name  = "/${local.prefix}/db/connection-string"
  type  = "SecureString"
  value = "REPLACE_AFTER_FIRST_APPLY"

  lifecycle {
    ignore_changes = [value]
  }
}

# JWT signing key — rotate in Parameter Store; never stored in source.
resource "aws_ssm_parameter" "jwt_key" {
  name  = "/${local.prefix}/jwt-key"
  type  = "SecureString"
  value = "REPLACE_AFTER_FIRST_APPLY"

  lifecycle {
    ignore_changes = [value]
  }
}

# Seed-admin HMAC key — the secret that makes the seeded admin's daily password unguessable.
# Injected into the API task as Seed__HmacKey; rotate in Parameter Store; never stored in source.
resource "aws_ssm_parameter" "seed_hmac_key" {
  name  = "/${local.prefix}/seed-hmac-key"
  type  = "SecureString"
  value = "REPLACE_AFTER_FIRST_APPLY"

  lifecycle {
    ignore_changes = [value]
  }
}
