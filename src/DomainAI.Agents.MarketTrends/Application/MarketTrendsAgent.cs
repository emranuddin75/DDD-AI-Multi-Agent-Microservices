using DomainAI.Agents.MarketTrends.Application.Commands;
using DomainAI.Shared.Application;
using DomainAI.Shared.Contracts;
using MediatR;
using Microsoft.Extensions.Logging;

namespace DomainAI.Agents.MarketTrends.Application;

/// <summary>
/// Refactored Agent using CQRS.
/// It now acts as a thin wrapper that dispatches commands via MediatR.
/// </summary>
public sealed class MarketTrendsAgent : AgentBase
{
    private readonly IMediator _mediator;

    public override string AgentId => "market-trends-agent";
    public override string AgentName => "MarketTrends Agent (CQRS)";
    public override string Domain => "MarketTrends";

    public MarketTrendsAgent(IMediator mediator, ILogger<MarketTrendsAgent> logger) : base(logger) 
    {
        _mediator = mediator;
    }

    protected override async Task<AgentResponse> ExecuteCoreAsync(AgentRequest request, CancellationToken cancellationToken)
    {
        // Dispatching the Command
        return await _mediator.Send(new ExecuteMarketAnalysisCommand(request), cancellationToken);
    }
}
