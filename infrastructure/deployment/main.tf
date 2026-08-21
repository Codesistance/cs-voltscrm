terraform {
  required_version = ">= 1.9"

  required_providers {
    aws = {
      source  = "hashicorp/aws"
      version = "~> 5.0"
    }
    random = {
      source  = "hashicorp/random"
      version = "~> 3.6"
    }
  }

  backend "s3" {
    # Pass bucket, region, dynamodb_table via -backend-config or backend.hcl
    key = "voltscrm/terraform.tfstate"
  }
}

provider "aws" {
  region = var.aws_region

  default_tags {
    tags = {
      Application = "VoltsCRM"
      Environment = var.environment
      ManagedBy   = "Terraform"
    }
  }
}

locals {
  prefix = "${var.app_name}-${var.environment}"

  az_count = 2
  azs      = slice(data.aws_availability_zones.available.names, 0, local.az_count)
}

data "aws_availability_zones" "available" {
  state = "available"
}
