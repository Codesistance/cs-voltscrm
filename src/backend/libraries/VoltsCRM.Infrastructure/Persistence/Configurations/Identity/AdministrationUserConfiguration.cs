using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VoltsCRM.Infrastructure.Identity;

namespace VoltsCRM.Infrastructure.Persistence.Configurations.Identity;

public class AdministrationUserConfiguration : IEntityTypeConfiguration<AdministrationUser>
{
    public void Configure(EntityTypeBuilder<AdministrationUser> builder)
    {
        builder.ToTable("administration_users", "identity");
        builder.HasKey(a => a.Id);

        builder.Property(a => a.UserId).HasMaxLength(450).IsRequired();
        builder.HasIndex(a => a.UserId).IsUnique();

        builder.HasMany(a => a.Roles)
            .WithOne()
            .HasForeignKey(r => r.AdministrationUserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
