using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VoltsCRM.Infrastructure.Auditing;

namespace VoltsCRM.Infrastructure.Persistence.Configurations.Auditing;

public class AuditEventConfiguration : IEntityTypeConfiguration<AuditEvent>
{
    public void Configure(EntityTypeBuilder<AuditEvent> builder)
    {
        builder.ToTable("audit_events");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Action).HasMaxLength(64).IsRequired();
        builder.Property(e => e.Outcome).HasMaxLength(16).IsRequired();
        builder.Property(e => e.ActorEmail).HasMaxLength(256);
        builder.Property(e => e.TargetType).HasMaxLength(32);
        builder.Property(e => e.TargetId).HasMaxLength(256);
        builder.Property(e => e.TargetLabel).HasMaxLength(256);
        builder.Property(e => e.IpAddress).HasMaxLength(45);
        builder.Property(e => e.UserAgent).HasMaxLength(512);
        builder.Property(e => e.Details).HasColumnType("jsonb");

        builder.HasIndex(e => e.OccurredAt);
        builder.HasIndex(e => e.Action);
        builder.HasIndex(e => e.ActorEmail);
        builder.HasIndex(e => e.TargetId);
    }
}
