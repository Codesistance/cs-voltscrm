using Microsoft.EntityFrameworkCore;
using VoltsCRM.Domain.Entities.Billing;
using VoltsCRM.Domain.Entities.Crm;
using VoltsCRM.Domain.Entities.Inventory;
using VoltsCRM.Domain.Entities.Organisation;

namespace VoltsCRM.Application.Common.Interfaces;

/// <summary>
/// Abstraction over the EF Core context so Application handlers stay free of Infrastructure.
/// Implemented by <c>AppDbContext</c>.
/// </summary>
public interface IAppDbContext
{
    // CRM
    DbSet<Customer> Customers { get; }
    DbSet<ServiceLocation> ServiceLocations { get; }

    // Billing
    DbSet<ServicePlan> ServicePlans { get; }
    DbSet<PlanLineItem> PlanLineItems { get; }
    DbSet<CustomerSubscription> CustomerSubscriptions { get; }
    DbSet<DeployedItem> DeployedItems { get; }
    DbSet<PaymentAccount> PaymentAccounts { get; }
    DbSet<Invoice> Invoices { get; }
    DbSet<InvoiceLineItem> InvoiceLineItems { get; }
    DbSet<InstallmentPlan> InstallmentPlans { get; }
    DbSet<Installment> Installments { get; }
    DbSet<Payment> Payments { get; }
    DbSet<PaymentAllocation> PaymentAllocations { get; }
    DbSet<DiscountGrant> DiscountGrants { get; }

    // Inventory
    DbSet<InventoryItem> InventoryItems { get; }
    DbSet<InventoryCategory> InventoryCategories { get; }
    DbSet<StockMovement> StockMovements { get; }

    // Organisation
    DbSet<Agent> Agents { get; }
    DbSet<PaymentGatewayConfig> PaymentGatewayConfigs { get; }
    DbSet<AutoDebitSettings> AutoDebitSettings { get; }
    DbSet<TokenVendingSettings> TokenVendingSettings { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
