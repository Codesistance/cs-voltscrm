using MediatR;
using VoltsCRM.API.Auth;
using VoltsCRM.Application.Authorization;
using VoltsCRM.Application.Features.Settings;

namespace VoltsCRM.API.Endpoints;

public static class SettingsEndpoints
{
    public static IEndpointRouteBuilder MapSettingsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/settings").WithTags("Settings");

        // Payment Gateway registry (one row per gateway, keyed by unique keyName)
        group.MapGet("/payment-gateways", ListPaymentGatewaysAsync).RequirePermission(Permissions.SettingsManage);
        group.MapPut("/payment-gateways/{keyName}", UpsertPaymentGatewayAsync).RequirePermission(Permissions.SettingsManage);
        group.MapPut("/payment-gateways/{keyName}/visibility", SetPaymentGatewayVisibilityAsync).RequirePermission(Permissions.SettingsManage);

        // Auto-Debit
        group.MapGet("/auto-debit", GetAutoDebitAsync).RequirePermission(Permissions.SettingsManage);
        group.MapPut("/auto-debit", UpdateAutoDebitAsync).RequirePermission(Permissions.SettingsManage);

        // Token Vending
        group.MapGet("/token-vending", GetTokenVendingAsync).RequirePermission(Permissions.SettingsManage);
        group.MapPut("/token-vending", UpdateTokenVendingAsync).RequirePermission(Permissions.SettingsManage);

        return app;
    }

    private static async Task<IResult> ListPaymentGatewaysAsync(ISender sender, CancellationToken ct)
        => TypedResults.Ok(await sender.Send(new ListPaymentGatewayConfigsQuery(), ct));

    private static async Task<IResult> UpsertPaymentGatewayAsync(
        ISender sender, string keyName, UpsertPaymentGatewayConfigInput input, CancellationToken ct)
        => TypedResults.Ok(await sender.Send(new UpsertPaymentGatewayConfigCommand(keyName, input), ct));

    private static async Task<IResult> SetPaymentGatewayVisibilityAsync(
        ISender sender, string keyName, SetVisibilityInput input, CancellationToken ct)
        => TypedResults.Ok(await sender.Send(new SetPaymentGatewayVisibilityCommand(keyName, input.Visible), ct));

    private static async Task<IResult> GetAutoDebitAsync(ISender sender, CancellationToken ct)
    {
        var result = await sender.Send(new GetAutoDebitSettingsQuery(), ct);
        return result is not null ? TypedResults.Ok(result) : TypedResults.Ok(new AutoDebitSettingsDto("", 3, false));
    }

    private static async Task<IResult> UpdateAutoDebitAsync(
        ISender sender, UpdateAutoDebitSettingsInput input, CancellationToken ct)
        => TypedResults.Ok(await sender.Send(new UpdateAutoDebitSettingsCommand(input), ct));

    private static async Task<IResult> GetTokenVendingAsync(ISender sender, CancellationToken ct)
    {
        var result = await sender.Send(new GetTokenVendingSettingsQuery(), ct);
        return result is not null ? TypedResults.Ok(result) : TypedResults.Ok(new TokenVendingSettingsDto("", "", false));
    }

    private static async Task<IResult> UpdateTokenVendingAsync(
        ISender sender, UpdateTokenVendingSettingsInput input, CancellationToken ct)
        => TypedResults.Ok(await sender.Send(new UpdateTokenVendingSettingsCommand(input), ct));

    public sealed record SetVisibilityInput(bool Visible);
}
