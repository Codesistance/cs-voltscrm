using MediatR;
using Microsoft.EntityFrameworkCore;
using VoltsCRM.Application.Common.Interfaces;
using VoltsCRM.Domain.Common;
using VoltsCRM.Domain.Entities.Crm;

namespace VoltsCRM.Application.Features.Import;

/// <summary>Input row for CSV import.</summary>
public sealed record ImportRowInput(
    string? AccountNumber,
    string? FirstName,
    string? LastName,
    string? Phone,
    string? Email,
    string? City,
    decimal? Amount);

/// <summary>Result of dry-run validation.</summary>
public sealed record DryRunResult(int ValidRows, int InvalidRows, IReadOnlyList<string> Errors);

/// <summary>Result of import commit.</summary>
public sealed record CommitResult(int ImportedRows, int SkippedRows, string? Message);

/// <summary>Validates import rows without persisting.</summary>
public sealed record ImportDryRunCommand(IReadOnlyList<ImportRowInput> Rows) : IRequest<DryRunResult>;

/// <summary>Commits validated rows to the database.</summary>
public sealed record ImportCommitCommand(
    IReadOnlyList<ImportRowInput> Rows,
    Dictionary<string, string>? Mapping = null) : IRequest<CommitResult>;

public sealed class ImportDryRunHandler(IAppDbContext db) : IRequestHandler<ImportDryRunCommand, DryRunResult>
{
    public async Task<DryRunResult> Handle(ImportDryRunCommand cmd, CancellationToken ct)
    {
        var errors = new List<string>();
        int validRows = 0;
        int invalidRows = 0;

        var existingAccounts = await db.Customers
            .AsNoTracking()
            .Select(c => c.AccountNumber)
            .ToListAsync(ct);
        var existingSet = existingAccounts.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var seenAccounts = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        for (int i = 0; i < cmd.Rows.Count; i++)
        {
            var row = cmd.Rows[i];
            var rowNum = i + 1;
            var rowErrors = ValidateRow(row, rowNum, existingSet, seenAccounts);

            if (rowErrors.Count > 0)
            {
                invalidRows++;
                errors.AddRange(rowErrors);
            }
            else
            {
                validRows++;
                if (!string.IsNullOrWhiteSpace(row.AccountNumber))
                    seenAccounts.Add(row.AccountNumber);
            }
        }

        return new DryRunResult(validRows, invalidRows, errors);
    }

    private static List<string> ValidateRow(ImportRowInput row, int rowNum,
        HashSet<string> existingAccounts, HashSet<string> seenAccounts)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(row.AccountNumber))
            errors.Add($"Row {rowNum}: AccountNumber is required.");
        else if (row.AccountNumber.Length > 20)
            errors.Add($"Row {rowNum}: AccountNumber exceeds 20 characters.");
        else if (existingAccounts.Contains(row.AccountNumber))
            errors.Add($"Row {rowNum}: AccountNumber '{row.AccountNumber}' already exists.");
        else if (seenAccounts.Contains(row.AccountNumber))
            errors.Add($"Row {rowNum}: Duplicate AccountNumber '{row.AccountNumber}' in import.");

        if (string.IsNullOrWhiteSpace(row.FirstName))
            errors.Add($"Row {rowNum}: FirstName is required.");

        if (string.IsNullOrWhiteSpace(row.LastName))
            errors.Add($"Row {rowNum}: LastName is required.");

        if (string.IsNullOrWhiteSpace(row.Phone))
            errors.Add($"Row {rowNum}: Phone is required.");

        if (string.IsNullOrWhiteSpace(row.City))
            errors.Add($"Row {rowNum}: City is required.");

        if (row.Amount.HasValue && row.Amount.Value < 0)
            errors.Add($"Row {rowNum}: Amount cannot be negative.");

        return errors;
    }
}

public sealed class ImportCommitHandler(IAppDbContext db) : IRequestHandler<ImportCommitCommand, CommitResult>
{
    public async Task<CommitResult> Handle(ImportCommitCommand cmd, CancellationToken ct)
    {
        var existingAccounts = await db.Customers
            .AsNoTracking()
            .Select(c => c.AccountNumber)
            .ToListAsync(ct);
        var existingSet = existingAccounts.ToHashSet(StringComparer.OrdinalIgnoreCase);

        int imported = 0;
        int skipped = 0;

        foreach (var row in cmd.Rows)
        {
            // Skip rows with existing account numbers (idempotent)
            if (string.IsNullOrWhiteSpace(row.AccountNumber) || existingSet.Contains(row.AccountNumber))
            {
                skipped++;
                continue;
            }

            // Skip invalid rows
            if (string.IsNullOrWhiteSpace(row.FirstName) ||
                string.IsNullOrWhiteSpace(row.LastName) ||
                string.IsNullOrWhiteSpace(row.Phone) ||
                string.IsNullOrWhiteSpace(row.City))
            {
                skipped++;
                continue;
            }

            var personalInfo = new PersonalInfo(
                row.FirstName,
                row.LastName,
                Domain.Enums.Gender.Unknown,
                row.Phone,
                row.Email);

            var location = new Location(
                new Address(string.Empty, row.City, string.Empty, "KE"),
                null);

            var customer = new Customer(row.AccountNumber, personalInfo, location);
            db.Customers.Add(customer);
            existingSet.Add(row.AccountNumber);
            imported++;
        }

        if (imported > 0)
            await db.SaveChangesAsync(ct);

        return new CommitResult(imported, skipped, imported > 0 ? $"Successfully imported {imported} customers." : null);
    }
}
