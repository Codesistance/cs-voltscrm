# SES domain identity for transactional email (receipts, reminders, overdue notices).
# DNS is managed externally — the verification TXT and DKIM CNAME records are exposed as
# outputs (see outputs.tf) and must be published manually before SES leaves "pending".

resource "aws_ses_domain_identity" "main" {
  domain = var.ses_domain
}

resource "aws_ses_domain_dkim" "main" {
  domain = aws_ses_domain_identity.main.domain
}

resource "aws_ses_email_identity" "from" {
  email = var.ses_from_address
}
