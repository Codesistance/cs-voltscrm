using MediatR;
using System.Security.Claims;
using VoltsCRM.API.Auth;
using VoltsCRM.Application.Authorization;
using VoltsCRM.Application.Features.Discounts;

namespace VoltsCRM.API.Endpoints;

public static class DiscountEndpoints
{
    public static IEndpointRouteBuilder MapDiscountEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/discounts").WithTags("Discounts");

        group.MapGet("", ListAsync).RequirePermission(Permissions.DiscountsView);
        group.MapPost("", GrantAsync).RequirePermission(Permissions.DiscountsManage);
        group.MapPost("/{id:guid}/revoke", RevokeAsync).RequirePermission(Permissions.DiscountsManage);

        return app;
    }

    private static async Task<IResult> ListAsync(
        ISender sender,
        int? page,
        int? pageSize,
        Guid? customerId,
        string? status,
        CancellationToken ct)
        => TypedResults.Ok(await sender.Send(
            new ListDiscountGrantsQuery(page ?? 1, pageSize ?? 20, customerId, status), ct));

    private static async Task<IResult> GrantAsync(
        ISender sender,
        GrantDiscountRequest request,
        ClaimsPrincipal user,
        CancellationToken ct)
    {
        var grantedByUserId = user.FindFirstValue(ClaimTypes.NameIdentifier);
        var dto = await sender.Send(new GrantDiscountCommand(
            request.CustomerId,
            request.DiscountType,
            request.Value,
            request.Scope,
            request.TargetId,
            request.IsRecurring,
            request.ValidFrom,
            request.ValidUntil,
            grantedByUserId ?? string.Empty,
            request.Reason), ct);

        return TypedResults.Created($"/api/discounts/{dto.Id}", dto);
    }

    private static async Task<IResult> RevokeAsync(ISender sender, Guid id, CancellationToken ct)
    {
        await sender.Send(new RevokeDiscountCommand(id), ct);
        return TypedResults.NoContent();
    }
}

public sealed record GrantDiscountRequest(
    Guid CustomerId,
    string DiscountType,
    decimal Value,
    string Scope,
    Guid? TargetId,
    bool IsRecurring,
    DateTimeOffset? ValidFrom,
    DateTimeOffset? ValidUntil,
    string? Reason);
