using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VoltsCRM.Infrastructure.Identity;

namespace VoltsCRM.Infrastructure.Persistence.Configurations.Identity;

public class AdminRoleConfiguration : IEntityTypeConfiguration<AdminRole>
{
    public void Configure(EntityTypeBuilder<AdminRole> builder)
    {
        builder.ToTable("admin_roles", "identity");
        builder.HasKey(r => r.Id);

        builder.Property(r => r.Name).HasMaxLength(100).IsRequired();
        builder.HasIndex(r => r.Name).IsUnique();
        builder.Property(r => r.Description).HasMaxLength(250);

        builder.HasMany(r => r.Permissions)
            .WithOne()
            .HasForeignKey(p => p.AdminRoleId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(r => r.Users)
            .WithOne()
            .HasForeignKey(u => u.AdminRoleId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
