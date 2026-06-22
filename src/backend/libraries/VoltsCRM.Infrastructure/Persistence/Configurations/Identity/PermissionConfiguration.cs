using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VoltsCRM.Infrastructure.Identity;

namespace VoltsCRM.Infrastructure.Persistence.Configurations.Identity;

public class PermissionConfiguration : IEntityTypeConfiguration<Permission>
{
    public void Configure(EntityTypeBuilder<Permission> builder)
    {
        builder.ToTable("permissions", "identity");
        builder.HasKey(p => p.Key);

        builder.Property(p => p.Key).HasMaxLength(100);
        builder.Property(p => p.Description).HasMaxLength(250).IsRequired();
        builder.Property(p => p.Group).HasMaxLength(100).IsRequired();
    }
}
