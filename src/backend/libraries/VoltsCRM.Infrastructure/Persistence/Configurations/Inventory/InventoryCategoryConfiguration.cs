using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VoltsCRM.Domain.Entities.Inventory;

namespace VoltsCRM.Infrastructure.Persistence.Configurations.Inventory;

public class InventoryCategoryConfiguration : IEntityTypeConfiguration<InventoryCategory>
{
    public void Configure(EntityTypeBuilder<InventoryCategory> builder)
    {
        builder.ToTable("inventory_categories", "inventory");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Name).HasMaxLength(100).IsRequired();
        // Partial unique index so a soft-deleted category's name can be reused.
        builder.HasIndex(c => c.Name).IsUnique().HasFilter("\"DeletedAt\" IS NULL");
        builder.Property(c => c.Code).HasMaxLength(30);

        builder.HasQueryFilter(c => c.DeletedAt == null);
    }
}
