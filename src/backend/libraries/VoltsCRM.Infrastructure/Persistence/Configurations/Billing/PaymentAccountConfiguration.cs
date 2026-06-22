using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VoltsCRM.Domain.Entities.Billing;

namespace VoltsCRM.Infrastructure.Persistence.Configurations.Billing;

public class PaymentAccountConfiguration : IEntityTypeConfiguration<PaymentAccount>
{
    public void Configure(EntityTypeBuilder<PaymentAccount> builder)
    {
        builder.ToTable("payment_accounts", "billing");
        builder.HasKey(p => p.Id);
        builder.HasIndex(p => p.CustomerId).IsUnique();
        builder.Property(p => p.Balance).HasPrecision(18, 4);
        builder.Property(p => p.Currency).HasMaxLength(3);
        builder.Property(p => p.RowVersion).IsRowVersion();
    }
}
