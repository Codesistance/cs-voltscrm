using System.Collections.Frozen;

namespace VoltsCRM.Application.Authorization;

/// <summary>Catalogue entry describing one permission for the role-management UI.</summary>
public sealed record PermissionDefinition(string Key, string Group, string Description);

/// <summary>
/// The single source of truth for every access permission in the system. New admin pages/features
/// add their key here; the seeder upserts these into the <c>permissions</c> table and the
/// authorization layer validates against <see cref="Keys"/>. Customer and Agent areas are
/// fixed by user type and do not use this registry.
/// </summary>
public static class Permissions
{
    // Access control
    public const string AccessManage = "access.manage";

    // Customers
    public const string CustomersView = "customers.view";
    public const string CustomersManage = "customers.manage";

    // Agents
    public const string AgentsView = "agents.view";
    public const string AgentsManage = "agents.manage";

    // Inventory
    public const string InventoryView = "inventory.view";
    public const string InventoryManage = "inventory.manage";
    public const string InventoryCategoriesManage = "inventory.categories.manage";

    // Service plans
    public const string ServicePlansView = "serviceplans.view";
    public const string ServicePlansManage = "serviceplans.manage";

    // Subscriptions
    public const string SubscriptionsView = "subscriptions.view";
    public const string SubscriptionsManage = "subscriptions.manage";

    // Invoices
    public const string InvoicesView = "invoices.view";
    public const string InvoicesManage = "invoices.manage";

    // Payments
    public const string PaymentsView = "payments.view";
    public const string PaymentsRecord = "payments.record";

    // Discounts
    public const string DiscountsView = "discounts.view";
    public const string DiscountsManage = "discounts.manage";

    // Reports
    public const string ReportsView = "reports.view";

    // Settings
    public const string SettingsManage = "settings.manage";

    /// <summary>The full catalogue, grouped for the role editor.</summary>
    public static readonly IReadOnlyList<PermissionDefinition> All =
    [
        new(AccessManage, "Access Control", "Manage admin roles, permissions and assignments"),

        new(CustomersView, "Customers", "View customers"),
        new(CustomersManage, "Customers", "Create, edit and deactivate customers"),

        new(AgentsView, "Agents", "View agents"),
        new(AgentsManage, "Agents", "Create, invite, edit and deactivate agents"),

        new(InventoryView, "Inventory", "View inventory items and stock"),
        new(InventoryManage, "Inventory", "Create and edit inventory items and stock"),
        new(InventoryCategoriesManage, "Inventory", "Create, edit and deactivate inventory categories"),

        new(ServicePlansView, "Service Plans", "View service plans"),
        new(ServicePlansManage, "Service Plans", "Create and edit service plans"),

        new(SubscriptionsView, "Subscriptions", "View customer subscriptions"),
        new(SubscriptionsManage, "Subscriptions", "Create and manage subscriptions"),

        new(InvoicesView, "Invoices", "View invoices"),
        new(InvoicesManage, "Invoices", "Generate and adjust invoices"),

        new(PaymentsView, "Payments", "View payments"),
        new(PaymentsRecord, "Payments", "Record and allocate payments"),

        new(DiscountsView, "Discounts", "View discount grants"),
        new(DiscountsManage, "Discounts", "Create and revoke discount grants"),

        new(ReportsView, "Reports", "View reports (statements, aging, collections)"),

        new(SettingsManage, "Settings", "Manage system settings (payment gateways, auto-debit, token vending)"),
    ];

    /// <summary>All valid permission keys, for fast membership checks during authorization.</summary>
    public static readonly FrozenSet<string> Keys = All.Select(p => p.Key).ToFrozenSet();
}
