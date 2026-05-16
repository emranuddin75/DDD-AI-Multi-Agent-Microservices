using DomainAI.Agents.Costing.Application.Commands;
using DomainAI.Shared.Application;
using DomainAI.Shared.Contracts;
using MediatR;
using Microsoft.Extensions.Logging;

namespace DomainAI.Agents.Costing.Application;

/// <summary>
/// Refactored Costing agent using CQRS via MediatR.
/// Acts as a thin wrapper that dispatches commands through IMediator.
/// All domain logic lives in <see cref="ExecuteAgentCommandHandler"/>.
/// </summary>
public sealed class CostingAgent : AgentBase
{
    private readonly IMediator _mediator;

    public override string AgentId => "costing-agent";
    public override string AgentName => "Costing Agent";
    public override string Domain => "Costing";

    public CostingAgent(IMediator mediator, ILogger<CostingAgent> logger) : base(logger)
    {
        _mediator = mediator;
    }

    protected override Task<AgentResponse> ExecuteCoreAsync(AgentRequest request, CancellationToken cancellationToken)
        => _mediator.Send(new ExecuteAgentCommand(request), cancellationToken);
}
