namespace VoltsCRM.Infrastructure.Identity;

/// <summary>
/// A single, code-defined access permission (e.g. <c>invoices.view</c>). The canonical list lives
/// in <c>VoltsCRM.Application.Authorization.Permissions</c>; this table is the persisted catalogue
/// the role editor reads from. Rows are upserted by the seeder, not created by users.
/// </summary>
public class Permission
{
    /// <summary>Primary key — the stable permission string, e.g. <c>customers.manage</c>.</summary>
    public string Key { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    /// <summary>UI grouping for the role editor, e.g. "Customers".</summary>
    public string Group { get; set; } = string.Empty;
}
