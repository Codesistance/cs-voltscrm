using MediatR;
using System.Security.Claims;
using VoltsCRM.API.Auth;
using VoltsCRM.Application.Authorization;
using VoltsCRM.Application.Common.Models;
using VoltsCRM.Application.Features.Subscriptions;

namespace VoltsCRM.API.Endpoints;

public static class SubscriptionEndpoints
{
    public static IEndpointRouteBuilder MapSubscriptionEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/subscriptions").WithTags("Subscriptions");

        group.MapGet("", ListAsync).RequirePermission(Permissions.SubscriptionsView);
        group.MapGet("/{id:guid}", GetAsync).RequirePermission(Permissions.SubscriptionsView);
        group.MapPost("", CreateAsync).RequirePermission(Permissions.SubscriptionsManage);
        group.MapPut("/{id:guid}", UpdateAsync).RequirePermission(Permissions.SubscriptionsManage);
        group.MapPost("/{id:guid}/activate", ActivateAsync).RequirePermission(Permissions.SubscriptionsManage);
        group.MapPost("/{id:guid}/suspend", SuspendAsync).RequirePermission(Permissions.SubscriptionsManage);
        group.MapPost("/{id:guid}/resume", ResumeAsync).RequirePermission(Permissions.SubscriptionsManage);
        group.MapPost("/{id:guid}/terminate", TerminateAsync).RequirePermission(Permissions.SubscriptionsManage);
        group.MapPost("/{id:guid}/deployed-items", RecordDeployedItemAsync).RequirePermission(Permissions.SubscriptionsManage);

        return app;
    }

    private static async Task<IResult> ListAsync(
        ISender sender, int? page, int? pageSize, string? status, Guid? customerId, Guid? servicePlanId, CancellationToken ct)
        => TypedResults.Ok(await sender.Send(
            new ListSubscriptionsQuery(page ?? 1, pageSize ?? 20, status, customerId, servicePlanId), ct));

    private static async Task<IResult> GetAsync(ISender sender, Guid id, CancellationToken ct)
        => TypedResults.Ok(await sender.Send(new GetSubscriptionQuery(id), ct));

    private static async Task<IResult> CreateAsync(ISender sender, CreateSubscriptionCommand command, CancellationToken ct)
    {
        var dto = await sender.Send(command, ct);
        return TypedResults.Created($"/api/subscriptions/{dto.Id}", dto);
    }

    private static async Task<IResult> UpdateAsync(
        ISender sender, Guid id, UpdateSubscriptionRequest request, CancellationToken ct)
    {
        var dto = await sender.Send(
            new UpdateSubscriptionCommand(id, request.StartDate, request.NegotiatedPrice, request.ServiceLocationId), ct);
        return TypedResults.Ok(dto);
    }

    private static async Task<IResult> ActivateAsync(ISender sender, Guid id, CancellationToken ct)
    {
        await sender.Send(new ActivateSubscriptionCommand(id), ct);
        return TypedResults.NoContent();
    }

    private static async Task<IResult> SuspendAsync(ISender sender, Guid id, CancellationToken ct)
    {
        await sender.Send(new SuspendSubscriptionCommand(id), ct);
        return TypedResults.NoContent();
    }

    private static async Task<IResult> ResumeAsync(ISender sender, Guid id, CancellationToken ct)
    {
        await sender.Send(new ResumeSubscriptionCommand(id), ct);
        return TypedResults.NoContent();
    }

    private static async Task<IResult> TerminateAsync(ISender sender, Guid id, CancellationToken ct)
    {
        await sender.Send(new TerminateSubscriptionCommand(id), ct);
        return TypedResults.NoContent();
    }

    private static async Task<IResult> RecordDeployedItemAsync(
        ISender sender, Guid id, RecordDeployedItemRequest req, ClaimsPrincipal user, CancellationToken ct)
    {
        var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
        var dto = await sender.Send(
            new RecordDeployedItemCommand(id, req.InventoryItemId, req.Quantity, req.SerialNumber, userId), ct);
        return TypedResults.Ok(dto);
    }
}

public sealed record RecordDeployedItemRequest(Guid InventoryItemId, int Quantity, string? SerialNumber);

public sealed record UpdateSubscriptionRequest(
    DateTimeOffset StartDate,
    MoneyDto? NegotiatedPrice,
    Guid? ServiceLocationId);
