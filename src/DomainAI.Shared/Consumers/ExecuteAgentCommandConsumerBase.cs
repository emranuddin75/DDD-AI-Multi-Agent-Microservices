using DomainAI.Shared.Contracts;
using DomainAI.Shared.Contracts.Messages;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace DomainAI.Shared.Consumers;

public abstract class ExecuteAgentCommandConsumerBase : IConsumer<ExecuteAgentCommand>
{
    private readonly ILogger _logger;

    protected ExecuteAgentCommandConsumerBase(ILogger logger)
    {
        _logger = logger;
    }

    protected abstract string TargetAgentId { get; }

    protected abstract Task<AgentResponse> ExecuteAgentLogicAsync(
        ExecuteAgentCommand command, CancellationToken ct);

    public async Task Consume(ConsumeContext<ExecuteAgentCommand> context)
    {
        var command = context.Message;

        if (!string.Equals(command.TargetAgentId, TargetAgentId, StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogDebug("Ignoring command for agent {Target} — this consumer handles {MyAgent}",
                command.TargetAgentId, TargetAgentId);
            return;
        }

        _logger.LogInformation(
            "ExecuteAgentCommandConsumer [{AgentId}]: Processing command {CommandId} for workflow {WorkflowId}, step {Step}",
            TargetAgentId, command.CommandId, command.WorkflowId, command.StepOrder);

        AgentResponse response;
        bool success;

        try
        {
            response = await ExecuteAgentLogicAsync(command, context.CancellationToken);
            success = true;
            _logger.LogInformation("Agent [{AgentId}] completed step {Step} successfully", TargetAgentId, command.StepOrder);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Agent [{AgentId}] failed for step {Step}", TargetAgentId, command.StepOrder);
            response = new AgentResponse
            {
                SourceAgentId = TargetAgentId,
                SourceAgentName = TargetAgentId,
                Domain = "Error",
                Success = false,
                Summary = $"Agent execution failed: {ex.Message}"
            };
            success = false;
        }

        var completedEvent = new AgentTaskCompletedEvent
        {
            WorkflowId = command.WorkflowId,
            CorrelationId = command.CorrelationId,
            SourceAgentId = TargetAgentId,
            SourceAgentName = TargetAgentId,
            StepOrder = command.StepOrder,
            Success = success,
            AgentResponse = response
        };

        await context.Publish(completedEvent);

        _logger.LogInformation(
            "Published AgentTaskCompletedEvent for workflow {WorkflowId}, step {Step}",
            command.WorkflowId, command.StepOrder);
    }
}
