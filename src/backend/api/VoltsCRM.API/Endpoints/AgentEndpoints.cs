using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using VoltsCRM.API.Auth;
using VoltsCRM.Application.Authorization;
using VoltsCRM.Application.Common.Interfaces;
using VoltsCRM.Application.Common.Mappings;
using VoltsCRM.Application.Common.Models;
using VoltsCRM.Application.Common.Options;
using VoltsCRM.Application.Features.Agents;
using VoltsCRM.Domain.Entities.Organisation;
using VoltsCRM.Domain.Enums;
using VoltsCRM.Infrastructure.Identity;
using VoltsCRM.Infrastructure.Persistence;
using static VoltsCRM.Application.Common.Mappings.LocationMappings;

namespace VoltsCRM.API.Endpoints;

/// <summary>
/// Admin management of Agent users. Creating an agent provisions an <c>AspNetUsers</c> login
/// (UserType=Agent), assigns the Agent role, creates the profile, and emails a set-password invite.
/// Identity operations live here (API layer), mirroring AuthEndpoints/AccessEndpoints.
/// </summary>
public static class AgentEndpoints
{
    public static IEndpointRouteBuilder MapAgentEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/agents").WithTags("Agents");

        group.MapGet("/me/kpis", GetMyKpisAsync).RequireUserType(UserType.Agent);
        group.MapGet("", ListAsync).RequirePermission(Permissions.AgentsView);
        group.MapGet("/{id:guid}", GetAsync).RequirePermission(Permissions.AgentsView);
        group.MapGet("/{id:guid}/kpis", GetKpisAsync).RequirePermission(Permissions.AgentsView);
        group.MapPost("", CreateAsync).RequirePermission(Permissions.AgentsManage);
        group.MapPut("/{id:guid}", UpdateAsync).RequirePermission(Permissions.AgentsManage);
        group.MapPost("/{id:guid}/deactivate", DeactivateAsync).RequirePermission(Permissions.AgentsManage);
        group.MapPost("/{id:guid}/resend-invite", ResendInviteAsync).RequirePermission(Permissions.AgentsManage);

        return app;
    }

    private static async Task<IResult> GetKpisAsync(ISender sender, Guid id, CancellationToken ct)
        => TypedResults.Ok(await sender.Send(new GetAgentKpisQuery(id), ct));

    private static async Task<IResult> GetMyKpisAsync(
        ISender sender, ClaimsPrincipal user, AppDbContext db, CancellationToken ct)
    {
        var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null) return Results.Unauthorized();

        var agent = await db.Agents.AsNoTracking().FirstOrDefaultAsync(a => a.UserId == userId, ct);
        if (agent is null) return Results.NotFound();

        return TypedResults.Ok(await sender.Send(new GetAgentKpisQuery(agent.Id), ct));
    }

    private static async Task<IResult> ListAsync(AppDbContext db, CancellationToken ct)
    {
        var rows = await db.Agents.AsNoTracking()
            .Join(db.Users, a => a.UserId, u => u.Id, (a, u) => new { a, u })
            .OrderBy(x => x.u.Email)
            .ToListAsync(ct);

        var agents = rows.Select(x => new AgentDto(
            x.a.Id, x.a.UserId, x.u.Email ?? string.Empty, x.u.FirstName, x.u.LastName,
            x.u.PhoneNumber, x.a.Location.ToDto(), x.a.Territory, x.a.IsActive)).ToList();

        return TypedResults.Ok(agents);
    }

    private static async Task<IResult> GetAsync(AppDbContext db, Guid id, CancellationToken ct)
    {
        var row = await db.Agents.AsNoTracking()
            .Where(a => a.Id == id)
            .Join(db.Users, a => a.UserId, u => u.Id, (a, u) => new { a, u })
            .FirstOrDefaultAsync(ct);

        if (row is null) return Results.NotFound();

        var agent = new AgentDto(
            row.a.Id, row.a.UserId, row.u.Email ?? string.Empty, row.u.FirstName, row.u.LastName,
            row.u.PhoneNumber, row.a.Location.ToDto(), row.a.Territory, row.a.IsActive);

        return TypedResults.Ok(agent);
    }

    private static async Task<IResult> CreateAsync(
        CreateAgentRequest req,
        UserManager<AppUser> userManager,
        AppDbContext db,
        IEmailSender email,
        IOptions<EmailOptions> emailOptions,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req.Email))
            return Results.Problem(statusCode: StatusCodes.Status400BadRequest, title: "Email is required.");

        if (await userManager.FindByEmailAsync(req.Email) is not null)
            return Results.Problem(statusCode: StatusCodes.Status409Conflict, title: $"A user with email '{req.Email}' already exists.");

        var user = new AppUser
        {
            UserName = req.Email,
            Email = req.Email,
            EmailConfirmed = true,
            FirstName = req.FirstName,
            LastName = req.LastName,
            PhoneNumber = req.Phone,
            UserType = UserType.Agent,
            IsActive = true,
            MustChangePassword = true,
        };

        var result = await userManager.CreateAsync(user, GenerateTemporaryPassword());
        if (!result.Succeeded)
            return Results.Problem(statusCode: StatusCodes.Status400BadRequest,
                title: string.Join("; ", result.Errors.Select(e => e.Description)));

        await userManager.AddToRoleAsync(user, "Agent");

        var agent = new Agent(user.Id, ToLocation(req.Location), req.Territory);
        db.Agents.Add(agent);
        await db.SaveChangesAsync(ct);

        await AccountInviteService.SendSetPasswordInviteAsync(userManager, email, emailOptions.Value, user);

        var dto = new AgentDto(agent.Id, user.Id, user.Email!, user.FirstName, user.LastName,
            user.PhoneNumber, agent.Location.ToDto(), agent.Territory, agent.IsActive);

        return TypedResults.Created($"/api/agents/{agent.Id}", dto);
    }

    private static async Task<IResult> UpdateAsync(
        Guid id, UpdateAgentRequest req, UserManager<AppUser> userManager, AppDbContext db, CancellationToken ct)
    {
        var agent = await db.Agents.FirstOrDefaultAsync(a => a.Id == id, ct);
        if (agent is null) return Results.NotFound();

        var user = await userManager.FindByIdAsync(agent.UserId);
        if (user is null) return Results.NotFound();

        user.FirstName = req.FirstName;
        user.LastName = req.LastName;
        user.PhoneNumber = req.Phone;
        await userManager.UpdateAsync(user);

        agent.Update(ToLocation(req.Location), req.Territory);
        await db.SaveChangesAsync(ct);

        return Results.NoContent();
    }

    private static async Task<IResult> DeactivateAsync(
        Guid id, UserManager<AppUser> userManager, AppDbContext db, CancellationToken ct)
    {
        var agent = await db.Agents.FirstOrDefaultAsync(a => a.Id == id, ct);
        if (agent is null) return Results.NotFound();

        agent.Deactivate();
        await db.SaveChangesAsync(ct);

        var user = await userManager.FindByIdAsync(agent.UserId);
        if (user is not null)
        {
            user.IsActive = false;
            await userManager.UpdateAsync(user);
        }

        return Results.NoContent();
    }

    private static async Task<IResult> ResendInviteAsync(
        Guid id, UserManager<AppUser> userManager, AppDbContext db, IEmailSender email,
        IOptions<EmailOptions> emailOptions, CancellationToken ct)
    {
        var agent = await db.Agents.FirstOrDefaultAsync(a => a.Id == id, ct);
        if (agent is null) return Results.NotFound();

        var user = await userManager.FindByIdAsync(agent.UserId);
        if (user is null) return Results.NotFound();

        user.MustChangePassword = true;
        await userManager.UpdateAsync(user);

        await AccountInviteService.SendSetPasswordInviteAsync(userManager, email, emailOptions.Value, user);
        return Results.NoContent();
    }

    // Random password that satisfies the Identity policy (upper, lower, digit, length ≥ 8).
    private static string GenerateTemporaryPassword()
        => $"{Guid.NewGuid():N}".Substring(0, 12) + "Aa1!";
}

public sealed record AgentDto(
    Guid Id, string UserId, string Email, string FirstName, string LastName, string? Phone,
    LocationDto Location, string? Territory, bool IsActive);

public sealed record CreateAgentRequest(
    string Email, string FirstName, string LastName, string? Phone, LocationInput Location, string? Territory);

public sealed record UpdateAgentRequest(
    string FirstName, string LastName, string? Phone, LocationInput Location, string? Territory);
