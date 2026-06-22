using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VoltsCRM.Domain.Entities.Organisation;

namespace VoltsCRM.Infrastructure.Persistence.Configurations.Organisation;

public class AgentConfiguration : IEntityTypeConfiguration<Agent>
{
    public void Configure(EntityTypeBuilder<Agent> builder)
    {
        builder.ToTable("agents", "crm");
        builder.HasKey(a => a.Id);
        builder.Property(a => a.UserId).HasMaxLength(450).IsRequired();
        builder.HasIndex(a => a.UserId).IsUnique();
        builder.Property(a => a.Territory).HasMaxLength(200);

        builder.OwnsOne(a => a.Location, loc =>
        {
            loc.OwnsOne(l => l.Address, a =>
            {
                a.Property(p => p.Street).HasColumnName("street").HasMaxLength(200);
                a.Property(p => p.City).HasColumnName("city").HasMaxLength(100).IsRequired();
                a.Property(p => p.Region).HasColumnName("region").HasMaxLength(100);
                a.Property(p => p.Country).HasColumnName("country").HasMaxLength(100).IsRequired();
            });
            loc.OwnsOne(l => l.Coordinates, g =>
            {
                g.Property(p => p.Latitude).HasColumnName("latitude");
                g.Property(p => p.Longitude).HasColumnName("longitude");
            });
            loc.Navigation(l => l.Address).IsRequired();
        });
        builder.Navigation(a => a.Location).IsRequired();
    }
}
