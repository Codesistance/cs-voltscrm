using MediatR;
using VoltsCRM.API.Auth;
using VoltsCRM.Application.Authorization;
using VoltsCRM.Application.Features.Reports;

namespace VoltsCRM.API.Endpoints;

public static class ReportEndpoints
{
    public static IEndpointRouteBuilder MapReportEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/reports").WithTags("Reports");

        group.MapGet("/dashboard-summary", DashboardSummaryAsync).RequirePermission(Permissions.ReportsView);
        group.MapGet("/collections", CollectionSummaryAsync).RequirePermission(Permissions.ReportsView);
        group.MapGet("/aging", AgingReportAsync).RequirePermission(Permissions.ReportsView);
        group.MapGet("/customers/{customerId:guid}/statement", CustomerStatementAsync).RequirePermission(Permissions.ReportsView);

        return app;
    }

    private static async Task<IResult> DashboardSummaryAsync(ISender sender, CancellationToken ct)
        => TypedResults.Ok(await sender.Send(new DashboardSummaryQuery(), ct));

    private static async Task<IResult> CollectionSummaryAsync(
        ISender sender,
        DateOnly from,
        DateOnly to,
        CancellationToken ct)
        => TypedResults.Ok(await sender.Send(new CollectionSummaryQuery(from, to), ct));

    private static async Task<IResult> AgingReportAsync(ISender sender, CancellationToken ct)
        => TypedResults.Ok(await sender.Send(new AgingReportQuery(), ct));

    private static async Task<IResult> CustomerStatementAsync(
        ISender sender,
        Guid customerId,
        DateOnly? from,
        DateOnly? to,
        CancellationToken ct)
        => TypedResults.Ok(await sender.Send(new CustomerStatementQuery(customerId, from, to), ct));
}
