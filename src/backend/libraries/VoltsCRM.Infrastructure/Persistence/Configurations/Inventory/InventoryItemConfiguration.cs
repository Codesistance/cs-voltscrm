using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VoltsCRM.Domain.Entities.Inventory;

namespace VoltsCRM.Infrastructure.Persistence.Configurations.Inventory;

public class InventoryItemConfiguration : IEntityTypeConfiguration<InventoryItem>
{
    public void Configure(EntityTypeBuilder<InventoryItem> builder)
    {
        builder.ToTable("inventory_items", "inventory");
        builder.HasKey(i => i.Id);
        builder.Property(i => i.ItemCode).HasMaxLength(50).IsRequired();
        builder.HasIndex(i => i.ItemCode).IsUnique();
        builder.Property(i => i.Name).HasMaxLength(200).IsRequired();
        builder.Property(i => i.UnitOfMeasure).HasMaxLength(30);

        builder.HasOne(i => i.Category)
            .WithMany()
            .HasForeignKey(i => i.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.OwnsOne(i => i.UnitCost, m =>
        {
            m.Property(p => p.Amount).HasColumnName("unit_cost_amount").HasPrecision(18, 4);
            m.Property(p => p.Currency).HasColumnName("unit_cost_currency").HasMaxLength(3);
        });

        builder.HasQueryFilter(i => i.DeletedAt == null);
    }
}
