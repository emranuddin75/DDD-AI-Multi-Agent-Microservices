using DomainAI.Agents.Reporting.Application.Commands;
using DomainAI.Shared.Application;
using DomainAI.Shared.Contracts;
using MediatR;
using Microsoft.Extensions.Logging;

namespace DomainAI.Agents.Reporting.Application;

/// <summary>
/// Refactored Reporting agent using CQRS via MediatR.
/// Acts as a thin wrapper that dispatches commands through IMediator.
/// All domain logic lives in <see cref="ExecuteAgentCommandHandler"/>.
/// </summary>
public sealed class ReportingAgent : AgentBase
{
    private readonly IMediator _mediator;

    public override string AgentId => "reporting-agent";
    public override string AgentName => "Report Writer Agent";
    public override string Domain => "Reporting";

    public ReportingAgent(IMediator mediator, ILogger<ReportingAgent> logger) : base(logger)
    {
        _mediator = mediator;
    }

    protected override Task<AgentResponse> ExecuteCoreAsync(AgentRequest request, CancellationToken cancellationToken)
        => _mediator.Send(new ExecuteAgentCommand(request), cancellationToken);
}
