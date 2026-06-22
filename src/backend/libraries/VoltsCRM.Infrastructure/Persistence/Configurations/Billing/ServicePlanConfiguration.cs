using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VoltsCRM.Domain.Entities.Billing;

namespace VoltsCRM.Infrastructure.Persistence.Configurations.Billing;

public class ServicePlanConfiguration : IEntityTypeConfiguration<ServicePlan>
{
    public void Configure(EntityTypeBuilder<ServicePlan> builder)
    {
        builder.ToTable("service_plans", "billing");
        builder.HasKey(s => s.Id);
        builder.Property(s => s.PlanCode).HasMaxLength(30).IsRequired();
        builder.HasIndex(s => s.PlanCode).IsUnique();
        builder.Property(s => s.Name).HasMaxLength(200).IsRequired();
        builder.Property(s => s.BillingType).HasConversion<string>().HasMaxLength(20);
        builder.Property(s => s.BillingCycle).HasConversion<string>().HasMaxLength(20);
        builder.Property(s => s.Status).HasConversion<string>().HasMaxLength(20);

        builder.OwnsOne(s => s.BasePrice, m =>
        {
            m.Property(p => p.Amount).HasColumnName("base_price_amount").HasPrecision(18, 4);
            m.Property(p => p.Currency).HasColumnName("base_price_currency").HasMaxLength(3);
        });

        builder.HasMany(s => s.LineItems)
            .WithOne()
            .HasForeignKey(l => l.ServicePlanId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
