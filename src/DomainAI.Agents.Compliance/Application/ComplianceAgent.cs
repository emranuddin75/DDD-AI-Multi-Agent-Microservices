using DomainAI.Agents.Compliance.Application.Commands;
using DomainAI.Shared.Application;
using DomainAI.Shared.Contracts;
using MediatR;
using Microsoft.Extensions.Logging;

namespace DomainAI.Agents.Compliance.Application;

/// <summary>
/// Refactored Compliance agent using CQRS via MediatR.
/// Acts as a thin wrapper that dispatches commands through IMediator.
/// All domain logic lives in <see cref="ExecuteAgentCommandHandler"/>.
/// </summary>
public sealed class ComplianceAgent : AgentBase
{
    private readonly IMediator _mediator;

    public override string AgentId => "compliance-agent";
    public override string AgentName => "Compliance Agent";
    public override string Domain => "Compliance";

    public ComplianceAgent(IMediator mediator, ILogger<ComplianceAgent> logger) : base(logger)
    {
        _mediator = mediator;
    }

    protected override Task<AgentResponse> ExecuteCoreAsync(AgentRequest request, CancellationToken cancellationToken)
        => _mediator.Send(new ExecuteAgentCommand(request), cancellationToken);
}
