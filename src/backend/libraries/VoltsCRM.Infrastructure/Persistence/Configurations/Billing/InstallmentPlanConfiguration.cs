using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VoltsCRM.Domain.Entities.Billing;

namespace VoltsCRM.Infrastructure.Persistence.Configurations.Billing;

public class InstallmentPlanConfiguration : IEntityTypeConfiguration<InstallmentPlan>
{
    public void Configure(EntityTypeBuilder<InstallmentPlan> builder)
    {
        builder.ToTable("installment_plans", "billing");
        builder.HasKey(i => i.Id);
        builder.Property(i => i.TotalAmount).HasPrecision(18, 4);
        builder.Property(i => i.DepositAmount).HasPrecision(18, 4);
        builder.Property(i => i.Currency).HasMaxLength(3);
        builder.Ignore(i => i.RemainingAmount);

        builder.HasMany(i => i.Installments)
            .WithOne(s => s.InstallmentPlan)
            .HasForeignKey(s => s.InstallmentPlanId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
