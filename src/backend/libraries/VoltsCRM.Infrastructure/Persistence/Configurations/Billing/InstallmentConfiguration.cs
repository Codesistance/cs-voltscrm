using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VoltsCRM.Domain.Entities.Billing;

namespace VoltsCRM.Infrastructure.Persistence.Configurations.Billing;

public class InstallmentConfiguration : IEntityTypeConfiguration<Installment>
{
    public void Configure(EntityTypeBuilder<Installment> builder)
    {
        builder.ToTable("installments", "billing");
        builder.HasKey(i => i.Id);
        builder.Property(i => i.Amount).HasPrecision(18, 4);
        builder.Property(i => i.Status).HasConversion<string>().HasMaxLength(20);
    }
}
