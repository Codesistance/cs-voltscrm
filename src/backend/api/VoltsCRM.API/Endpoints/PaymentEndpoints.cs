using MediatR;
using VoltsCRM.API.Auth;
using VoltsCRM.Application.Authorization;
using VoltsCRM.Application.Features.Payments;

namespace VoltsCRM.API.Endpoints;

public static class PaymentEndpoints
{
    public static IEndpointRouteBuilder MapPaymentEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/payments").WithTags("Payments");

        group.MapGet("", ListAsync).RequirePermission(Permissions.PaymentsView);
        group.MapGet("/{id:guid}", GetAsync).RequirePermission(Permissions.PaymentsView);
        group.MapPost("", RecordAsync).RequirePermission(Permissions.PaymentsRecord);
        group.MapPost("/{id:guid}/complete", CompleteAsync).RequirePermission(Permissions.PaymentsRecord);
        group.MapPost("/{id:guid}/fail", FailAsync).RequirePermission(Permissions.PaymentsRecord);
        group.MapPost("/{id:guid}/reverse", ReverseAsync).RequirePermission(Permissions.PaymentsRecord);

        return app;
    }

    private static async Task<IResult> ListAsync(
        ISender sender, int? page, int? pageSize, Guid? customerId, string? status, string? method,
        DateTimeOffset? from, DateTimeOffset? to, CancellationToken ct)
        => TypedResults.Ok(await sender.Send(
            new ListPaymentsQuery(page ?? 1, pageSize ?? 20, customerId, status, method, from, to), ct));

    private static async Task<IResult> GetAsync(ISender sender, Guid id, CancellationToken ct)
        => TypedResults.Ok(await sender.Send(new GetPaymentQuery(id), ct));

    private static async Task<IResult> RecordAsync(ISender sender, RecordPaymentCommand command, CancellationToken ct)
    {
        var dto = await sender.Send(command, ct);
        return TypedResults.Created($"/api/payments/{dto.Id}", dto);
    }

    private static async Task<IResult> CompleteAsync(ISender sender, Guid id, CancellationToken ct)
    {
        await sender.Send(new CompletePaymentCommand(id), ct);
        return TypedResults.NoContent();
    }

    private static async Task<IResult> FailAsync(ISender sender, Guid id, CancellationToken ct)
    {
        await sender.Send(new FailPaymentCommand(id), ct);
        return TypedResults.NoContent();
    }

    private static async Task<IResult> ReverseAsync(ISender sender, Guid id, CancellationToken ct)
    {
        await sender.Send(new ReversePaymentCommand(id), ct);
        return TypedResults.NoContent();
    }
}
