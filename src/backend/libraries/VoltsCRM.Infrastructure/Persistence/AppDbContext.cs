using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using VoltsCRM.Application.Common.Interfaces;
using VoltsCRM.Domain.Entities.Billing;
using VoltsCRM.Domain.Entities.Crm;
using VoltsCRM.Domain.Entities.Inventory;
using VoltsCRM.Domain.Entities.Organisation;
using VoltsCRM.Infrastructure.Auditing;
using VoltsCRM.Infrastructure.Identity;

namespace VoltsCRM.Infrastructure.Persistence;

public class AppDbContext(DbContextOptions<AppDbContext> options)
    : IdentityDbContext<AppUser>(options), IAppDbContext
{
    // Identity (auth)
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    // Identity (admin RBAC) — kept off IAppDbContext as these are Infrastructure.Identity types
    public DbSet<AdministrationUser> AdministrationUsers => Set<AdministrationUser>();
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<AdminRole> AdminRoles => Set<AdminRole>();
    public DbSet<AdminRolePermission> AdminRolePermissions => Set<AdminRolePermission>();
    public DbSet<AdminUserRole> AdminUserRoles => Set<AdminUserRole>();

    // CRM
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<ServiceLocation> ServiceLocations => Set<ServiceLocation>();

    // Billing
    public DbSet<ServicePlan> ServicePlans => Set<ServicePlan>();
    public DbSet<PlanLineItem> PlanLineItems => Set<PlanLineItem>();
    public DbSet<CustomerSubscription> CustomerSubscriptions => Set<CustomerSubscription>();
    public DbSet<DeployedItem> DeployedItems => Set<DeployedItem>();
    public DbSet<PaymentAccount> PaymentAccounts => Set<PaymentAccount>();
    public DbSet<Invoice> Invoices => Set<Invoice>();
    public DbSet<InvoiceLineItem> InvoiceLineItems => Set<InvoiceLineItem>();
    public DbSet<InstallmentPlan> InstallmentPlans => Set<InstallmentPlan>();
    public DbSet<Installment> Installments => Set<Installment>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<PaymentAllocation> PaymentAllocations => Set<PaymentAllocation>();
    public DbSet<DiscountGrant> DiscountGrants => Set<DiscountGrant>();

    // Inventory
    public DbSet<InventoryItem> InventoryItems => Set<InventoryItem>();
    public DbSet<InventoryCategory> InventoryCategories => Set<InventoryCategory>();
    public DbSet<StockMovement> StockMovements => Set<StockMovement>();

    // Organisation
    public DbSet<Agent> Agents => Set<Agent>();
    public DbSet<PaymentGatewayConfig> PaymentGatewayConfigs => Set<PaymentGatewayConfig>();
    public DbSet<AutoDebitSettings> AutoDebitSettings => Set<AutoDebitSettings>();
    public DbSet<TokenVendingSettings> TokenVendingSettings => Set<TokenVendingSettings>();

    // Auditing (append-only)
    public DbSet<AuditEvent> AuditEvents => Set<AuditEvent>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        // Move Identity tables to identity schema
        foreach (var entity in builder.Model.GetEntityTypes())
        {
            var tableName = entity.GetTableName();
            if (tableName is not null && tableName.StartsWith("AspNet"))
                entity.SetSchema("identity");
        }
    }
}
