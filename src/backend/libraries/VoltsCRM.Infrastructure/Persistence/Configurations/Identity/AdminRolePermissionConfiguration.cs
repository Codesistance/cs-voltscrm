using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VoltsCRM.Infrastructure.Identity;

namespace VoltsCRM.Infrastructure.Persistence.Configurations.Identity;

public class AdminRolePermissionConfiguration : IEntityTypeConfiguration<AdminRolePermission>
{
    public void Configure(EntityTypeBuilder<AdminRolePermission> builder)
    {
        builder.ToTable("admin_role_permissions", "identity");
        builder.HasKey(rp => new { rp.AdminRoleId, rp.PermissionKey });

        builder.Property(rp => rp.PermissionKey).HasMaxLength(100).IsRequired();

        // Restrict: permissions are code-defined; never cascade-delete the catalogue from a grant.
        builder.HasOne<Permission>()
            .WithMany()
            .HasForeignKey(rp => rp.PermissionKey)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
