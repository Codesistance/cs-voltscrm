using MediatR;
using VoltsCRM.API.Auth;
using VoltsCRM.Application.Authorization;
using VoltsCRM.Application.Features.Invoices;

namespace VoltsCRM.API.Endpoints;

public static class InvoiceEndpoints
{
    public static IEndpointRouteBuilder MapInvoiceEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/invoices").WithTags("Invoices");

        group.MapGet("", ListAsync).RequirePermission(Permissions.InvoicesView);
        group.MapGet("/{id:guid}", GetAsync).RequirePermission(Permissions.InvoicesView);
        group.MapGet("/customers/{customerId:guid}/payment-account", GetCustomerPaymentAccountAsync)
            .RequirePermission(Permissions.InvoicesView);
        group.MapPost("/generate", GenerateAsync).RequirePermission(Permissions.InvoicesManage);
        group.MapPost("/mark-overdue", MarkOverdueAsync).RequirePermission(Permissions.InvoicesManage);

        return app;
    }

    private static async Task<IResult> ListAsync(
        ISender sender,
        int? page,
        int? pageSize,
        Guid? customerId,
        string? status,
        int? year,
        int? month,
        CancellationToken ct)
        => TypedResults.Ok(await sender.Send(
            new ListInvoicesQuery(page ?? 1, pageSize ?? 20, customerId, status, year, month), ct));

    private static async Task<IResult> GetAsync(ISender sender, Guid id, CancellationToken ct)
        => TypedResults.Ok(await sender.Send(new GetInvoiceQuery(id), ct));

    private static async Task<IResult> GetCustomerPaymentAccountAsync(
        ISender sender,
        Guid customerId,
        CancellationToken ct)
        => TypedResults.Ok(await sender.Send(new GetCustomerPaymentAccountQuery(customerId), ct));

    private static async Task<IResult> GenerateAsync(
        ISender sender,
        GenerateInvoicesRequest request,
        CancellationToken ct)
        => TypedResults.Ok(await sender.Send(new GenerateInvoicesCommand(request.Year, request.Month), ct));

    private static async Task<IResult> MarkOverdueAsync(ISender sender, CancellationToken ct)
        => TypedResults.Ok(await sender.Send(new MarkInvoicesOverdueCommand(), ct));
}

public sealed record GenerateInvoicesRequest(int? Year, int? Month);
