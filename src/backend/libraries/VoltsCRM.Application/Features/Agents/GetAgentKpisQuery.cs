using MediatR;
using Microsoft.EntityFrameworkCore;
using VoltsCRM.Application.Common.Interfaces;
using VoltsCRM.Domain.Enums;

namespace VoltsCRM.Application.Features.Agents;

/// <summary>KPI summary for an agent's dashboard.</summary>
public sealed record AgentKpisDto(
    int AssignedCustomers,
    int VisitsToday,
    decimal PaymentsCollected,
    string PaymentsCurrency,
    int OpenTasks);

/// <summary>Query for agent KPIs.</summary>
public sealed record GetAgentKpisQuery(Guid AgentId) : IRequest<AgentKpisDto>;

public sealed class GetAgentKpisHandler(IAppDbContext db) : IRequestHandler<GetAgentKpisQuery, AgentKpisDto>
{
    public async Task<AgentKpisDto> Handle(GetAgentKpisQuery query, CancellationToken ct)
    {
        var agentId = query.AgentId.ToString();
        var today = DateTimeOffset.UtcNow.Date;
        var todayStart = new DateTimeOffset(today, TimeSpan.Zero);
        var todayEnd = todayStart.AddDays(1);

        // Count customers assigned to this agent (via subscriptions collected by this agent)
        // For now, count distinct customers from payments collected by this agent
        var assignedCustomers = await db.Payments
            .Where(p => p.CollectedByAgentId == agentId)
            .Select(p => p.CustomerId)
            .Distinct()
            .CountAsync(ct);

        // Visits today - count payments collected today by this agent
        var visitsToday = await db.Payments
            .Where(p => p.CollectedByAgentId == agentId
                     && p.PaymentDate >= todayStart
                     && p.PaymentDate < todayEnd)
            .CountAsync(ct);

        // Payments collected today
        var paymentsToday = await db.Payments
            .Where(p => p.CollectedByAgentId == agentId
                     && p.PaymentDate >= todayStart
                     && p.PaymentDate < todayEnd
                     && p.Status == PaymentStatus.Completed)
            .SumAsync(p => p.Amount - p.DiscountApplied, ct);

        // Open tasks - for now, count pending installments for customers this agent handles
        var openTasks = await db.Payments
            .Where(p => p.CollectedByAgentId == agentId)
            .Select(p => p.CustomerId)
            .Distinct()
            .Join(db.InstallmentPlans, cid => cid, ip => ip.CustomerId, (cid, ip) => ip.Id)
            .Join(db.Installments, planId => planId, i => i.InstallmentPlanId, (planId, i) => i)
            .Where(i => i.Status == InstallmentStatus.Pending || i.Status == InstallmentStatus.Overdue)
            .CountAsync(ct);

        return new AgentKpisDto(
            assignedCustomers,
            visitsToday,
            paymentsToday,
            "KES",
            openTasks);
    }
}
