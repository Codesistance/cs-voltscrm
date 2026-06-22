using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VoltsCRM.Domain.Entities.Billing;

namespace VoltsCRM.Infrastructure.Persistence.Configurations.Billing;

public class InvoiceConfiguration : IEntityTypeConfiguration<Invoice>
{
    public void Configure(EntityTypeBuilder<Invoice> builder)
    {
        builder.ToTable("invoices", "billing");
        builder.HasKey(i => i.Id);
        builder.Property(i => i.GrossAmount).HasPrecision(18, 4);
        builder.Property(i => i.DiscountAmount).HasPrecision(18, 4);
        builder.Property(i => i.AmountPaid).HasPrecision(18, 4);
        builder.Property(i => i.Currency).HasMaxLength(3);
        builder.Property(i => i.Status).HasConversion<string>().HasMaxLength(20);

        builder.Ignore(i => i.AmountDue);
        builder.Ignore(i => i.Balance);

        builder.HasIndex(i => new { i.CustomerId, i.PeriodYear, i.PeriodMonth });
        builder.HasIndex(i => i.Status);
        builder.HasIndex(i => i.DueDate);

        builder.HasMany(i => i.LineItems)
            .WithOne()
            .HasForeignKey(l => l.InvoiceId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
