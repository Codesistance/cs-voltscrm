using Microsoft.Extensions.DependencyInjection;
using VoltsCRM.Application.Common.Interfaces;

namespace VoltsCRM.Infrastructure.Integrations;

/// <summary>
/// Catalog over the keyed <see cref="IPaymentGateway"/> registrations. Keyed DI can't enumerate its
/// keys, so the implemented-key set is supplied at registration time (see DependencyInjection) and
/// adapters are resolved on demand via <see cref="IServiceProvider.GetKeyedService{T}(object?)"/>.
/// </summary>
public sealed class PaymentGatewayCatalog(IServiceProvider sp, IReadOnlyCollection<string> implementedKeys)
    : IPaymentGatewayCatalog
{
    public IReadOnlyCollection<string> ImplementedKeys { get; } = implementedKeys;

    public IPaymentGateway? Resolve(string keyName)
    {
        if (string.IsNullOrWhiteSpace(keyName) || !ImplementedKeys.Contains(keyName))
            return null;
        return sp.GetKeyedService<IPaymentGateway>(keyName);
    }
}
