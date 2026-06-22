using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VoltsCRM.Domain.Entities.Billing;

namespace VoltsCRM.Infrastructure.Persistence.Configurations.Billing;

public class DiscountGrantConfiguration : IEntityTypeConfiguration<DiscountGrant>
{
    public void Configure(EntityTypeBuilder<DiscountGrant> builder)
    {
        builder.ToTable("discount_grants", "billing");
        builder.HasKey(d => d.Id);
        builder.Property(d => d.DiscountType).HasConversion<string>().HasMaxLength(20);
        builder.Property(d => d.Scope).HasConversion<string>().HasMaxLength(30);
        builder.Property(d => d.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(d => d.Value).HasPrecision(18, 4);
        builder.Property(d => d.Reason).HasMaxLength(500);
        builder.Property(d => d.GrantedByUserId).HasMaxLength(450);
        builder.HasIndex(d => new { d.CustomerId, d.Status });
        builder.HasIndex(d => d.ValidUntil).HasFilter("\"ValidUntil\" IS NOT NULL");
    }
}
