using MediatR;
using VoltsCRM.API.Auth;
using VoltsCRM.Application.Authorization;
using VoltsCRM.Application.Common.Models;
using VoltsCRM.Application.Features.Customers;

namespace VoltsCRM.API.Endpoints;

public static class CustomerEndpoints
{
    public static IEndpointRouteBuilder MapCustomerEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/customers").WithTags("Customers");

        group.MapGet("", ListAsync).RequirePermission(Permissions.CustomersView);
        group.MapGet("/geo", GetGeoAsync).RequirePermission(Permissions.CustomersView);
        group.MapGet("/{id:guid}", GetAsync).RequirePermission(Permissions.CustomersView);
        group.MapPost("", CreateAsync).RequirePermission(Permissions.CustomersManage);
        group.MapPut("/{id:guid}", UpdateAsync).RequirePermission(Permissions.CustomersManage);
        group.MapPost("/{id:guid}/suspend", SuspendAsync).RequirePermission(Permissions.CustomersManage);
        group.MapPost("/{id:guid}/reactivate", ReactivateAsync).RequirePermission(Permissions.CustomersManage);
        group.MapPost("/{id:guid}/disconnect", DisconnectAsync).RequirePermission(Permissions.CustomersManage);
        group.MapPost("/{id:guid}/service-locations", AddServiceLocationAsync).RequirePermission(Permissions.CustomersManage);
        group.MapPost("/{id:guid}/service-locations/{locationId:guid}/deactivate", DeactivateServiceLocationAsync)
            .RequirePermission(Permissions.CustomersManage);

        return app;
    }

    private static async Task<IResult> ListAsync(
        ISender sender, int? page, int? pageSize, string? q, string? status, CancellationToken ct)
        => TypedResults.Ok(await sender.Send(new ListCustomersQuery(page ?? 1, pageSize ?? 20, q, status), ct));

    private static async Task<IResult> GetGeoAsync(
        ISender sender,
        double? minLng,
        double? minLat,
        double? maxLng,
        double? maxLat,
        CancellationToken ct)
        => TypedResults.Ok(await sender.Send(new GetCustomersGeoQuery(minLng, minLat, maxLng, maxLat), ct));

    private static async Task<IResult> GetAsync(ISender sender, Guid id, CancellationToken ct)
        => TypedResults.Ok(await sender.Send(new GetCustomerQuery(id), ct));

    private static async Task<IResult> CreateAsync(ISender sender, CreateCustomerCommand command, CancellationToken ct)
    {
        var dto = await sender.Send(command, ct);
        return TypedResults.Created($"/api/customers/{dto.Id}", dto);
    }

    private static async Task<IResult> UpdateAsync(ISender sender, Guid id, UpdateCustomerRequest req, CancellationToken ct)
        => TypedResults.Ok(await sender.Send(new UpdateCustomerCommand(id, req.PersonalInfo, req.Location), ct));

    private static async Task<IResult> SuspendAsync(ISender sender, Guid id, CancellationToken ct)
    {
        await sender.Send(new SuspendCustomerCommand(id), ct);
        return TypedResults.NoContent();
    }

    private static async Task<IResult> ReactivateAsync(ISender sender, Guid id, CancellationToken ct)
    {
        await sender.Send(new ReactivateCustomerCommand(id), ct);
        return TypedResults.NoContent();
    }

    private static async Task<IResult> DisconnectAsync(ISender sender, Guid id, CancellationToken ct)
    {
        await sender.Send(new DisconnectCustomerCommand(id), ct);
        return TypedResults.NoContent();
    }

    private static async Task<IResult> AddServiceLocationAsync(
        ISender sender, Guid id, AddServiceLocationRequest req, CancellationToken ct)
    {
        var dto = await sender.Send(new AddServiceLocationCommand(id, req.Label, req.Location), ct);
        return TypedResults.Created($"/api/customers/{id}/service-locations/{dto.Id}", dto);
    }

    private static async Task<IResult> DeactivateServiceLocationAsync(
        ISender sender, Guid id, Guid locationId, CancellationToken ct)
    {
        await sender.Send(new DeactivateServiceLocationCommand(id, locationId), ct);
        return TypedResults.NoContent();
    }
}

public sealed record UpdateCustomerRequest(PersonalInfoInput PersonalInfo, LocationInput Location);

public sealed record AddServiceLocationRequest(string Label, LocationInput Location);
