#!/usr/bin/env bash
# Idempotently provision the Terraform remote-state backend: an S3 bucket for the
# state file and a DynamoDB table for state locking. Safe to run on every deploy —
# existing resources are left untouched and reported, missing ones are created.
#
# Required env:
#   TF_STATE_BUCKET   S3 bucket name for the Terraform state (globally unique)
#   TF_LOCK_TABLE     DynamoDB table name for the state lock
#   AWS_REGION        Region to create the resources in
set -euo pipefail

: "${TF_STATE_BUCKET:?TF_STATE_BUCKET is required}"
: "${TF_LOCK_TABLE:?TF_LOCK_TABLE is required}"
: "${AWS_REGION:?AWS_REGION is required}"

# ── S3 state bucket ────────────────────────────────────────────────────────────
if aws s3api head-bucket --bucket "$TF_STATE_BUCKET" 2>/dev/null; then
  echo "✓ State bucket already exists: $TF_STATE_BUCKET"
else
  echo "→ Creating state bucket: $TF_STATE_BUCKET ($AWS_REGION)"
  # us-east-1 must NOT receive a LocationConstraint; every other region requires it.
  if [ "$AWS_REGION" = "us-east-1" ]; then
    aws s3api create-bucket --bucket "$TF_STATE_BUCKET" --region "$AWS_REGION"
  else
    aws s3api create-bucket --bucket "$TF_STATE_BUCKET" --region "$AWS_REGION" \
      --create-bucket-configuration "LocationConstraint=$AWS_REGION"
  fi

  aws s3api put-bucket-versioning --bucket "$TF_STATE_BUCKET" \
    --versioning-configuration Status=Enabled

  aws s3api put-bucket-encryption --bucket "$TF_STATE_BUCKET" \
    --server-side-encryption-configuration \
    '{"Rules":[{"ApplyServerSideEncryptionByDefault":{"SSEAlgorithm":"AES256"}}]}'

  aws s3api put-public-access-block --bucket "$TF_STATE_BUCKET" \
    --public-access-block-configuration \
    BlockPublicAcls=true,IgnorePublicAcls=true,BlockPublicPolicy=true,RestrictPublicBuckets=true

  echo "✓ State bucket created and hardened: $TF_STATE_BUCKET"
fi

# ── DynamoDB lock table ────────────────────────────────────────────────────────
if aws dynamodb describe-table --table-name "$TF_LOCK_TABLE" --region "$AWS_REGION" >/dev/null 2>&1; then
  echo "✓ Lock table already exists: $TF_LOCK_TABLE"
else
  echo "→ Creating lock table: $TF_LOCK_TABLE ($AWS_REGION)"
  aws dynamodb create-table --table-name "$TF_LOCK_TABLE" --region "$AWS_REGION" \
    --attribute-definitions AttributeName=LockID,AttributeType=S \
    --key-schema AttributeName=LockID,KeyType=HASH \
    --billing-mode PAY_PER_REQUEST >/dev/null
  aws dynamodb wait table-exists --table-name "$TF_LOCK_TABLE" --region "$AWS_REGION"
  echo "✓ Lock table created: $TF_LOCK_TABLE"
fi
