using System.Text.Json;
using DomainAI.Orchestrator.Application.Commands;
using DomainAI.Orchestrator.Domain;
using DomainAI.Shared.Contracts;
using DomainAI.Shared.Contracts.Messages;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace DomainAI.Orchestrator.Consumers;

public sealed class AgentTaskCompletedConsumer : IConsumer<AgentTaskCompletedEvent>
{
    private readonly IWorkflowStateStore _stateStore;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly ILogger<AgentTaskCompletedConsumer> _logger;

    public AgentTaskCompletedConsumer(
        IWorkflowStateStore stateStore,
        IPublishEndpoint publishEndpoint,
        ILogger<AgentTaskCompletedConsumer> logger)
    {
        _stateStore = stateStore;
        _publishEndpoint = publishEndpoint;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<AgentTaskCompletedEvent> context)
    {
        var message = context.Message;

        _logger.LogInformation(
            "AgentTaskCompletedConsumer: Received completion for workflow {WorkflowId}, step {Step}, agent [{Agent}], success={Success}",
            message.WorkflowId, message.StepOrder, message.SourceAgentName, message.Success);

        var plan = await _stateStore.LoadPlanAsync(message.WorkflowId, context.CancellationToken);
        if (plan == null)
        {
            _logger.LogError("Execution plan not found for workflow {WorkflowId}. Dropping message.", message.WorkflowId);
            return;
        }

        if (message.Success)
            plan.MarkStepCompleted(message.StepOrder, message.AgentResponse);
        else
            plan.MarkStepFailed(message.StepOrder, message.AgentResponse);

        await _stateStore.SavePlanAsync(plan, context.CancellationToken);

        if (plan.AllStepsCompleted)
        {
            _logger.LogInformation("All steps completed for workflow {WorkflowId}. Publishing WorkflowCompletedNotification.", message.WorkflowId);

            await _publishEndpoint.Publish(new Shared.Contracts.Messages.WorkflowCompletedNotification
            {
                WorkflowId = message.WorkflowId,
                CorrelationId = message.CorrelationId,
                Success = !plan.HasFailures,
                TotalSteps = plan.Steps.Count
            }, context.CancellationToken);

            await _stateStore.RemovePlanAsync(message.WorkflowId, context.CancellationToken);
            return;
        }

        var nextStep = plan.GetNextPendingStep();
        if (nextStep != null)
        {
            plan.MarkStepInProgress(nextStep.Order);
            await _stateStore.SavePlanAsync(plan, context.CancellationToken);

            var workflowRequest = new WorkflowRequest
            {
                WorkflowId = plan.WorkflowId,
                CorrelationId = plan.CorrelationId,
                Topic = plan.Topic,
                Industry = string.Empty,
                Region = string.Empty,
                Parameters = new Dictionary<string, string>()
            };

            var agentCommand = StartWorkflowCommandHandler.BuildAgentCommand(
                workflowRequest, nextStep, plan.CompletedResults);

            await _publishEndpoint.Publish(agentCommand, context.CancellationToken);

            _logger.LogInformation(
                "Published ExecuteAgentCommand for [{AgentName}] (Step {Order})", nextStep.AgentName, nextStep.Order);
        }
    }
}
