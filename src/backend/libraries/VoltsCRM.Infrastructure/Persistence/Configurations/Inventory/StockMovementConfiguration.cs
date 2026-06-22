using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VoltsCRM.Domain.Entities.Inventory;

namespace VoltsCRM.Infrastructure.Persistence.Configurations.Inventory;

public class StockMovementConfiguration : IEntityTypeConfiguration<StockMovement>
{
    public void Configure(EntityTypeBuilder<StockMovement> builder)
    {
        builder.ToTable("stock_movements", "inventory");
        builder.HasKey(s => s.Id);
        builder.Property(s => s.MovementType).HasConversion<string>().HasMaxLength(30);
        builder.Property(s => s.Reference).HasMaxLength(100);
        builder.Property(s => s.Notes).HasMaxLength(500);
        builder.HasIndex(s => s.InventoryItemId);
        builder.HasIndex(s => s.RelatedSubscriptionId);
        builder.HasIndex(s => s.CreatedAt);
    }
}
