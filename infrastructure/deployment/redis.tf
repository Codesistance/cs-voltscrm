resource "aws_security_group" "redis" {
  name   = "${local.prefix}-redis"
  vpc_id = aws_vpc.main.id

  ingress {
    from_port = 6379
    to_port   = 6379
    protocol  = "tcp"
    security_groups = [
      aws_security_group.api.id,
      aws_security_group.worker.id,
    ]
  }
}

resource "aws_elasticache_subnet_group" "main" {
  name       = local.prefix
  subnet_ids = aws_subnet.private[*].id
}

resource "aws_elasticache_replication_group" "main" {
  replication_group_id = local.prefix
  description          = "VoltsCRM ${var.environment} Redis"

  engine             = "redis"
  node_type          = var.redis_node_type
  num_cache_clusters = var.redis_num_cache_clusters
  port               = 6379

  subnet_group_name  = aws_elasticache_subnet_group.main.name
  security_group_ids = [aws_security_group.redis.id]

  at_rest_encryption_enabled = true
  transit_encryption_enabled = false

  automatic_failover_enabled = var.redis_num_cache_clusters > 1
}
