using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VoltsCRM.Domain.Entities.Organisation;

namespace VoltsCRM.Infrastructure.Persistence.Configurations.Organisation;

public class PaymentGatewayConfigConfiguration : IEntityTypeConfiguration<PaymentGatewayConfig>
{
    public void Configure(EntityTypeBuilder<PaymentGatewayConfig> builder)
    {
        builder.ToTable("payment_gateway_configs", "organisation");
        builder.HasKey(s => s.Id);
        builder.Property(s => s.KeyName).HasMaxLength(50).IsRequired();
        builder.HasIndex(s => s.KeyName).IsUnique();
        builder.Property(s => s.DisplayName).HasMaxLength(100).IsRequired();
        builder.Property(s => s.Visibility).IsRequired();

        // Free-form provider config/secrets persisted as jsonb.
        var dataComparer = new ValueComparer<Dictionary<string, string>>(
            (a, b) => JsonSerializer.Serialize(a, (JsonSerializerOptions?)null) == JsonSerializer.Serialize(b, (JsonSerializerOptions?)null),
            v => v == null ? 0 : JsonSerializer.Serialize(v, (JsonSerializerOptions?)null).GetHashCode(),
            v => JsonSerializer.Deserialize<Dictionary<string, string>>(JsonSerializer.Serialize(v, (JsonSerializerOptions?)null), (JsonSerializerOptions?)null)!);

        builder.Property(s => s.Data)
            .HasColumnType("jsonb")
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => string.IsNullOrEmpty(v)
                    ? new Dictionary<string, string>()
                    : JsonSerializer.Deserialize<Dictionary<string, string>>(v, (JsonSerializerOptions?)null)!)
            .Metadata.SetValueComparer(dataComparer);
    }
}

public class AutoDebitSettingsConfiguration : IEntityTypeConfiguration<AutoDebitSettings>
{
    public void Configure(EntityTypeBuilder<AutoDebitSettings> builder)
    {
        builder.ToTable("auto_debit_settings", "organisation");
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Provider).HasMaxLength(50).IsRequired();
    }
}

public class TokenVendingSettingsConfiguration : IEntityTypeConfiguration<TokenVendingSettings>
{
    public void Configure(EntityTypeBuilder<TokenVendingSettings> builder)
    {
        builder.ToTable("token_vending_settings", "organisation");
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Provider).HasMaxLength(50).IsRequired();
        builder.Property(s => s.ApiKey).HasMaxLength(500).IsRequired();
    }
}
