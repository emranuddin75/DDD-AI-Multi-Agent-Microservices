using DomainAI.Shared.Consumers;
using DomainAI.Shared.Contracts;
using MediatR;
using Microsoft.Extensions.Logging;

namespace DomainAI.Agents.Compliance.Api.Consumers;

public sealed class ExecuteAgentCommandConsumer : ExecuteAgentCommandConsumerBase
{
    private readonly IMediator _mediator;

    public ExecuteAgentCommandConsumer(IMediator mediator, ILogger<ExecuteAgentCommandConsumer> logger)
        : base(logger)
    {
        _mediator = mediator;
    }

    protected override string TargetAgentId => "compliance-agent";

    protected override async Task<AgentResponse> ExecuteAgentLogicAsync(
        DomainAI.Shared.Contracts.Messages.ExecuteAgentCommand command, CancellationToken ct)
    {
        var request = new AgentRequest
        {
            TargetAgentId = command.TargetAgentId,
            Intent = command.Intent,
            Payload = command.Payload,
            Metadata = command.Metadata,
            PreviousResults = command.PreviousResults
        };

        return await _mediator.Send(
            new DomainAI.Agents.Compliance.Application.Commands.ExecuteAgentCommand(request), ct);
    }
}
