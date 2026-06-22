using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VoltsCRM.Domain.Entities.Crm;

namespace VoltsCRM.Infrastructure.Persistence.Configurations.Crm;

public class ServiceLocationConfiguration : IEntityTypeConfiguration<ServiceLocation>
{
    public void Configure(EntityTypeBuilder<ServiceLocation> builder)
    {
        builder.ToTable("service_locations", "crm");
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Label).HasMaxLength(200).IsRequired();
        builder.HasIndex(s => s.CustomerId);
        builder.HasIndex(s => s.IsActive);

        builder.OwnsOne(s => s.Location, loc =>
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
        builder.Navigation(s => s.Location).IsRequired();
    }
}
