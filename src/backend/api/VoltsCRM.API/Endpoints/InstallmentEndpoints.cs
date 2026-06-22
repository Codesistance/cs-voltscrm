using MediatR;
using VoltsCRM.API.Auth;
using VoltsCRM.Application.Authorization;
using VoltsCRM.Application.Features.Installments;

namespace VoltsCRM.API.Endpoints;

public static class InstallmentEndpoints
{
    public static IEndpointRouteBuilder MapInstallmentEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/installment-plans").WithTags("Installment Plans");

        group.MapGet("", ListAsync).RequirePermission(Permissions.InvoicesView);
        group.MapGet("/{id:guid}", GetAsync).RequirePermission(Permissions.InvoicesView);
        group.MapPost("", CreateAsync).RequirePermission(Permissions.InvoicesManage);
        group.MapPost("/installments/{id:guid}/mark-paid", MarkInstallmentPaidAsync)
            .RequirePermission(Permissions.InvoicesManage);
        group.MapPost("/mark-overdue", MarkOverdueAsync).RequirePermission(Permissions.InvoicesManage);

        return app;
    }

    private static async Task<IResult> ListAsync(
        ISender sender,
        int? page,
        int? pageSize,
        Guid? customerId,
        CancellationToken ct)
        => TypedResults.Ok(await sender.Send(
            new ListInstallmentPlansQuery(page ?? 1, pageSize ?? 20, customerId), ct));

    private static async Task<IResult> GetAsync(ISender sender, Guid id, CancellationToken ct)
        => TypedResults.Ok(await sender.Send(new GetInstallmentPlanQuery(id), ct));

    private static async Task<IResult> CreateAsync(
        ISender sender,
        CreateInstallmentPlanRequest request,
        CancellationToken ct)
    {
        var dto = await sender.Send(
            new CreateInstallmentPlanCommand(
                request.SubscriptionId,
                request.TotalAmount,
                request.DepositAmount,
                request.InstallmentCount,
                request.StartDate), ct);
        return TypedResults.Created($"/api/installment-plans/{dto.Id}", dto);
    }

    private static async Task<IResult> MarkInstallmentPaidAsync(ISender sender, Guid id, CancellationToken ct)
    {
        await sender.Send(new MarkInstallmentPaidCommand(id), ct);
        return TypedResults.NoContent();
    }

    private static async Task<IResult> MarkOverdueAsync(ISender sender, CancellationToken ct)
        => TypedResults.Ok(await sender.Send(new MarkInstallmentsOverdueCommand(), ct));
}

public sealed record CreateInstallmentPlanRequest(
    Guid SubscriptionId,
    decimal TotalAmount,
    decimal DepositAmount,
    int InstallmentCount,
    DateTimeOffset StartDate);
