using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VoltsCRM.Domain.Entities.Billing;

namespace VoltsCRM.Infrastructure.Persistence.Configurations.Billing;

public class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        builder.ToTable("payments", "billing");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Amount).HasPrecision(18, 4);
        builder.Property(p => p.DiscountApplied).HasPrecision(18, 4);
        builder.Property(p => p.Currency).HasMaxLength(3);
        builder.Property(p => p.Method).HasConversion<string>().HasMaxLength(30);
        builder.Property(p => p.Channel).HasConversion<string>().HasMaxLength(30);
        builder.Property(p => p.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(p => p.PlatformProvider).HasMaxLength(50);
        builder.Property(p => p.PlatformReference).HasMaxLength(200);

        builder.HasIndex(p => new { p.CustomerId, p.PaymentDate });
        builder.HasIndex(p => p.PlatformReference).IsUnique().HasFilter("\"PlatformReference\" IS NOT NULL");
        builder.HasIndex(p => p.Status);

        builder.HasMany(p => p.Allocations)
            .WithOne()
            .HasForeignKey(a => a.PaymentId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
