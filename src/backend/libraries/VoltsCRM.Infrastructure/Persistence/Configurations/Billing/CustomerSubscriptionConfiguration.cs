using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VoltsCRM.Domain.Entities.Billing;

namespace VoltsCRM.Infrastructure.Persistence.Configurations.Billing;

public class CustomerSubscriptionConfiguration : IEntityTypeConfiguration<CustomerSubscription>
{
    public void Configure(EntityTypeBuilder<CustomerSubscription> builder)
    {
        builder.ToTable("customer_subscriptions", "billing");
        builder.HasKey(s => s.Id);
        builder.Property(s => s.BillingType).HasConversion<string>().HasMaxLength(20);
        builder.Property(s => s.Status).HasConversion<string>().HasMaxLength(20);

        builder.OwnsOne(s => s.NegotiatedPrice, m =>
        {
            m.Property(p => p.Amount).HasColumnName("negotiated_price_amount").HasPrecision(18, 4);
            m.Property(p => p.Currency).HasColumnName("negotiated_price_currency").HasMaxLength(3);
        });

        builder.HasMany(s => s.DeployedItems)
            .WithOne()
            .HasForeignKey(d => d.SubscriptionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(s => s.CustomerId);
        builder.HasIndex(s => s.ServicePlanId);
    }
}
