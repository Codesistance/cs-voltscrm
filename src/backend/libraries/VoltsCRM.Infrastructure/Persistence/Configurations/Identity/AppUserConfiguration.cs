using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VoltsCRM.Infrastructure.Identity;

namespace VoltsCRM.Infrastructure.Persistence.Configurations.Identity;

public class AppUserConfiguration : IEntityTypeConfiguration<AppUser>
{
    public void Configure(EntityTypeBuilder<AppUser> builder)
    {
        // Table name/schema stay as the ASP.NET Identity defaults ("AspNetUsers" -> identity schema,
        // applied by the AspNet* loop in AppDbContext.OnModelCreating). Only configure the new column.
        builder.Property(u => u.UserType).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(u => u.FirstName).HasMaxLength(100);
        builder.Property(u => u.LastName).HasMaxLength(100);
        builder.HasIndex(u => u.UserType);
    }
}
