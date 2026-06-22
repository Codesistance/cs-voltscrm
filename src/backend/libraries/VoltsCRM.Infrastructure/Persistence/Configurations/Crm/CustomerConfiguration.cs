using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VoltsCRM.Domain.Entities.Crm;

namespace VoltsCRM.Infrastructure.Persistence.Configurations.Crm;

public class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        builder.ToTable("customers", "crm");
        builder.HasKey(c => c.Id);

        builder.Property(c => c.AccountNumber).HasMaxLength(20).IsRequired();
        builder.HasIndex(c => c.AccountNumber).IsUnique();

        builder.OwnsOne(c => c.PersonalInfo, pi =>
        {
            pi.Property(p => p.FirstName).HasColumnName("first_name").HasMaxLength(100).IsRequired();
            pi.Property(p => p.LastName).HasColumnName("last_name").HasMaxLength(100).IsRequired();
            pi.Property(p => p.Gender).HasColumnName("gender").HasConversion<string>();
            pi.Property(p => p.Phone).HasColumnName("phone").HasMaxLength(20).IsRequired();
            pi.Property(p => p.Email).HasColumnName("email").HasMaxLength(200);
        });

        builder.OwnsOne(c => c.Location, loc =>
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
        builder.Navigation(c => c.Location).IsRequired();

        builder.Property(c => c.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(c => c.CreatedAt).IsRequired();
        builder.Property(c => c.UpdatedAt).IsRequired();

        builder.HasMany(c => c.ServiceLocations)
            .WithOne()
            .HasForeignKey(s => s.CustomerId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
