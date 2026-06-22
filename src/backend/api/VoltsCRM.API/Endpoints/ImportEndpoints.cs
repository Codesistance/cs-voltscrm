using MediatR;
using VoltsCRM.API.Auth;
using VoltsCRM.Application.Authorization;
using VoltsCRM.Application.Features.Import;

namespace VoltsCRM.API.Endpoints;

public static class ImportEndpoints
{
    public static IEndpointRouteBuilder MapImportEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/import").WithTags("Import");

        group.MapPost("/dry-run", DryRunAsync).RequirePermission(Permissions.CustomersManage);
        group.MapPost("/commit", CommitAsync).RequirePermission(Permissions.CustomersManage);

        return app;
    }

    private static async Task<IResult> DryRunAsync(
        ISender sender,
        ImportDryRunRequest request,
        CancellationToken ct)
        => TypedResults.Ok(await sender.Send(new ImportDryRunCommand(request.Rows), ct));

    private static async Task<IResult> CommitAsync(
        ISender sender,
        ImportCommitRequest request,
        CancellationToken ct)
        => TypedResults.Ok(await sender.Send(new ImportCommitCommand(request.Rows, request.Mapping), ct));
}

public sealed record ImportDryRunRequest(IReadOnlyList<ImportRowInput> Rows);

public sealed record ImportCommitRequest(
    IReadOnlyList<ImportRowInput> Rows,
    Dictionary<string, string>? Mapping = null);
