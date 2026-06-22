using MediatR;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;
using VoltsCRM.API.Auth;
using VoltsCRM.Application.Features.Payments;
using VoltsCRM.Application.Features.Portal;
using VoltsCRM.Domain.Enums;
using VoltsCRM.Infrastructure.Identity;

namespace VoltsCRM.API.Endpoints;

public static class PortalEndpoints
{
    public static IEndpointRouteBuilder MapPortalEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/portal/me")
            .WithTags("Portal")
            .RequireAuthorization()
            .RequireUserType(UserType.Customer);

        group.MapGet("/summary", SummaryAsync);
        group.MapGet("/invoices", InvoicesAsync);
        group.MapGet("/subscriptions", SubscriptionsAsync);
        group.MapGet("/payments", PaymentsAsync);
        group.MapGet("/profile", ProfileAsync);

        // Self-service payment (Phase 18b)
        group.MapGet("/gateways", GatewaysAsync);
        group.MapPost("/payments", InitiatePaymentAsync);

        return app;
    }

    private static async Task<IResult> GatewaysAsync(
        ISender sender,
        ClaimsPrincipal user,
        UserManager<AppUser> userManager,
        CancellationToken ct)
    {
        var customerId = await ResolveCustomerIdAsync(user, userManager);
        if (customerId is null)
            return TypedResults.Forbid();

        return TypedResults.Ok(await sender.Send(new ListAvailableGatewaysQuery(), ct));
    }

    private static async Task<IResult> InitiatePaymentAsync(
        ISender sender,
        ClaimsPrincipal user,
        UserManager<AppUser> userManager,
        InitiatePortalPaymentRequest request,
        CancellationToken ct)
    {
        var customerId = await ResolveCustomerIdAsync(user, userManager);
        if (customerId is null)
            return TypedResults.Forbid();

        var result = await sender.Send(
            new InitiatePortalPaymentCommand(customerId.Value, request.InvoiceId, request.Amount, request.GatewayKey), ct);
        return TypedResults.Ok(result);
    }

    public sealed record InitiatePortalPaymentRequest(Guid? InvoiceId, decimal? Amount, string GatewayKey);

    private static async Task<IResult> SummaryAsync(
        ISender sender,
        ClaimsPrincipal user,
        UserManager<AppUser> userManager,
        CancellationToken ct)
    {
        var customerId = await ResolveCustomerIdAsync(user, userManager);
        if (customerId is null)
            return TypedResults.Forbid();

        return TypedResults.Ok(await sender.Send(new PortalSummaryQuery(customerId.Value), ct));
    }

    private static async Task<IResult> InvoicesAsync(
        ISender sender,
        ClaimsPrincipal user,
        UserManager<AppUser> userManager,
        int? page,
        int? pageSize,
        CancellationToken ct)
    {
        var customerId = await ResolveCustomerIdAsync(user, userManager);
        if (customerId is null)
            return TypedResults.Forbid();

        return TypedResults.Ok(await sender.Send(
            new PortalInvoicesQuery(customerId.Value, page ?? 1, pageSize ?? 20), ct));
    }

    private static async Task<IResult> SubscriptionsAsync(
        ISender sender,
        ClaimsPrincipal user,
        UserManager<AppUser> userManager,
        int? page,
        int? pageSize,
        CancellationToken ct)
    {
        var customerId = await ResolveCustomerIdAsync(user, userManager);
        if (customerId is null)
            return TypedResults.Forbid();

        return TypedResults.Ok(await sender.Send(
            new PortalSubscriptionsQuery(customerId.Value, page ?? 1, pageSize ?? 20), ct));
    }

    private static async Task<IResult> PaymentsAsync(
        ISender sender,
        ClaimsPrincipal user,
        UserManager<AppUser> userManager,
        int? page,
        int? pageSize,
        CancellationToken ct)
    {
        var customerId = await ResolveCustomerIdAsync(user, userManager);
        if (customerId is null)
            return TypedResults.Forbid();

        return TypedResults.Ok(await sender.Send(
            new PortalPaymentsQuery(customerId.Value, page ?? 1, pageSize ?? 20), ct));
    }

    private static async Task<IResult> ProfileAsync(
        ISender sender,
        ClaimsPrincipal user,
        UserManager<AppUser> userManager,
        CancellationToken ct)
    {
        var customerId = await ResolveCustomerIdAsync(user, userManager);
        if (customerId is null)
            return TypedResults.Forbid();

        return TypedResults.Ok(await sender.Send(new PortalProfileQuery(customerId.Value), ct));
    }

    private static async Task<Guid?> ResolveCustomerIdAsync(ClaimsPrincipal principal, UserManager<AppUser> userManager)
    {
        var userId = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
            return null;

        var user = await userManager.FindByIdAsync(userId);
        return user?.CustomerId;
    }
}
