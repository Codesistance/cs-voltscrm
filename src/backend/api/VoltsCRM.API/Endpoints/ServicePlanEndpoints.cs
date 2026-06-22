using MediatR;
using VoltsCRM.API.Auth;
using VoltsCRM.Application.Authorization;
using VoltsCRM.Application.Common.Models;
using VoltsCRM.Application.Features.ServicePlans;

namespace VoltsCRM.API.Endpoints;

public static class ServicePlanEndpoints
{
    public static IEndpointRouteBuilder MapServicePlanEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/service-plans").WithTags("ServicePlans");

        group.MapGet("", ListAsync).RequirePermission(Permissions.ServicePlansView);
        group.MapGet("/{id:guid}", GetAsync).RequirePermission(Permissions.ServicePlansView);
        group.MapPost("", CreateAsync).RequirePermission(Permissions.ServicePlansManage);
        group.MapPut("/{id:guid}", UpdateAsync).RequirePermission(Permissions.ServicePlansManage);
        group.MapPost("/{id:guid}/archive", ArchiveAsync).RequirePermission(Permissions.ServicePlansManage);
        group.MapPost("/{id:guid}/restore", RestoreAsync).RequirePermission(Permissions.ServicePlansManage);

        return app;
    }

    private static async Task<IResult> ListAsync(
        ISender sender, int? page, int? pageSize, string? q, string? status, CancellationToken ct)
        => TypedResults.Ok(await sender.Send(new ListServicePlansQuery(page ?? 1, pageSize ?? 20, q, status), ct));

    private static async Task<IResult> GetAsync(ISender sender, Guid id, CancellationToken ct)
        => TypedResults.Ok(await sender.Send(new GetServicePlanQuery(id), ct));

    private static async Task<IResult> CreateAsync(
        ISender sender, CreateServicePlanCommand command, CancellationToken ct)
    {
        var dto = await sender.Send(command, ct);
        return TypedResults.Created($"/api/service-plans/{dto.Id}", dto);
    }

    private static async Task<IResult> UpdateAsync(
        ISender sender, Guid id, UpdateServicePlanRequest req, CancellationToken ct)
        => TypedResults.Ok(await sender.Send(
            new UpdateServicePlanCommand(id, req.Name, req.Description, req.BasePrice, req.LineItems), ct));

    private static async Task<IResult> ArchiveAsync(ISender sender, Guid id, CancellationToken ct)
    {
        await sender.Send(new ArchiveServicePlanCommand(id), ct);
        return TypedResults.NoContent();
    }

    private static async Task<IResult> RestoreAsync(ISender sender, Guid id, CancellationToken ct)
    {
        await sender.Send(new RestoreServicePlanCommand(id), ct);
        return TypedResults.NoContent();
    }
}

public sealed record UpdateServicePlanRequest(
    string Name,
    string? Description,
    MoneyDto BasePrice,
    IReadOnlyList<ServicePlanLineItemInput> LineItems);
