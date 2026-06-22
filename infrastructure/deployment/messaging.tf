# SNS topic → SQS queue fan-out for async work (gateway webhooks, notification jobs).
# Consumed by the Worker in later phases; provisioned now to complete the Phase 19 scope.

resource "aws_sns_topic" "notifications" {
  name = "${local.prefix}-notifications"
}

resource "aws_sqs_queue" "work_dlq" {
  name                      = "${local.prefix}-work-dlq"
  message_retention_seconds = 1209600 # 14 days
}

resource "aws_sqs_queue" "work" {
  name                       = "${local.prefix}-work"
  visibility_timeout_seconds = 60
  message_retention_seconds  = 345600 # 4 days

  redrive_policy = jsonencode({
    deadLetterTargetArn = aws_sqs_queue.work_dlq.arn
    maxReceiveCount     = 5
  })
}

resource "aws_sns_topic_subscription" "work" {
  topic_arn = aws_sns_topic.notifications.arn
  protocol  = "sqs"
  endpoint  = aws_sqs_queue.work.arn
}

# Allow the SNS topic to deliver messages to the queue.
resource "aws_sqs_queue_policy" "work" {
  queue_url = aws_sqs_queue.work.id
  policy = jsonencode({
    Version = "2012-10-17"
    Statement = [{
      Effect    = "Allow"
      Principal = { Service = "sns.amazonaws.com" }
      Action    = "sqs:SendMessage"
      Resource  = aws_sqs_queue.work.arn
      Condition = {
        ArnEquals = { "aws:SourceArn" = aws_sns_topic.notifications.arn }
      }
    }]
  })
}
