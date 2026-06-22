using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VoltsCRM.Domain.Entities.Billing;

namespace VoltsCRM.Infrastructure.Persistence.Configurations.Billing;

public class PlanLineItemConfiguration : IEntityTypeConfiguration<PlanLineItem>
{
    public void Configure(EntityTypeBuilder<PlanLineItem> builder)
    {
        builder.ToTable("plan_line_items", "billing");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Role).HasConversion<string>().HasMaxLength(20);
        builder.HasIndex(p => new { p.ServicePlanId, p.InventoryItemId });
    }
}
