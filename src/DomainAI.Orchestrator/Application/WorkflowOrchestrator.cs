using DomainAI.Orchestrator.Domain;
using DomainAI.Shared.Contracts;
using MassTransit;
using MediatR;
using Microsoft.Extensions.Logging;

namespace DomainAI.Orchestrator.Application;

public class WorkflowOrchestrator : DomainAI.Shared.Domain.IOrchestrator
{
    private readonly IMediator _mediator;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly IWorkflowStateStore _stateStore;
    private readonly ILogger<WorkflowOrchestrator> _logger;
    private readonly List<DomainAI.Shared.Domain.IAgent> _agents = new();

    public WorkflowOrchestrator(
        IMediator mediator,
        IPublishEndpoint publishEndpoint,
        IWorkflowStateStore stateStore,
        ILogger<WorkflowOrchestrator> logger)
    {
        _mediator = mediator;
        _publishEndpoint = publishEndpoint;
        _stateStore = stateStore;
        _logger = logger;
    }

    public void RegisterAgent(DomainAI.Shared.Domain.IAgent agent)
    {
        _agents.Add(agent);
        _logger.LogInformation("Registered agent: {AgentName} (message-driven mode)", agent.AgentName);
    }

    public IReadOnlyList<DomainAI.Shared.Domain.IAgent> RegisteredAgents => _agents.AsReadOnly();

    public async Task<OrchestratorResult> RunWorkflowAsync(WorkflowRequest request, CancellationToken ct = default)
    {
        return await ExecuteWorkflowAsync(request, ct);
    }

    public async Task<OrchestratorResult> ExecuteWorkflowAsync(WorkflowRequest request, CancellationToken ct = default)
    {
        _logger.LogInformation("WorkflowOrchestrator: Dispatching workflow {WorkflowId} to MediatR command handler", request.WorkflowId);
        return await _mediator.Send(new Commands.StartWorkflowCommand(request), ct);
    }
}
