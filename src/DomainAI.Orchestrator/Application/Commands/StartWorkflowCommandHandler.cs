using System.Diagnostics;
using System.Text.Json;
using DomainAI.Orchestrator.Domain;
using DomainAI.Shared.Contracts;
using DomainAI.Shared.Contracts.Messages;
using MassTransit;
using MediatR;
using Microsoft.Extensions.Logging;

namespace DomainAI.Orchestrator.Application.Commands;

public class StartWorkflowCommandHandler : IRequestHandler<StartWorkflowCommand, OrchestratorResult>
{
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly IWorkflowStateStore _stateStore;
    private readonly ILogger<StartWorkflowCommandHandler> _logger;

    public StartWorkflowCommandHandler(
        IPublishEndpoint publishEndpoint,
        IWorkflowStateStore stateStore,
        ILogger<StartWorkflowCommandHandler> logger)
    {
        _publishEndpoint = publishEndpoint;
        _stateStore = stateStore;
        _logger = logger;
    }

    public async Task<OrchestratorResult> Handle(StartWorkflowCommand command, CancellationToken cancellationToken)
    {
        var request = command.Request;
        var sw = Stopwatch.StartNew();

        _logger.LogInformation("=== WORKFLOW HANDLER: Starting async workflow {WorkflowId} | Topic: '{Topic}' ===",
            request.WorkflowId, request.Topic);

        var plan = BuildExecutionPlan(request);
        var planDescriptions = plan.Describe().ToList();

        _logger.LogInformation("Execution plan created with {StepCount} steps:", plan.Steps.Count);
        foreach (var step in planDescriptions)
            _logger.LogInformation("  {Step}", step);

        await _stateStore.SavePlanAsync(plan, cancellationToken);

        var firstStep = plan.GetNextPendingStep();
        if (firstStep != null)
        {
            plan.MarkStepInProgress(firstStep.Order);
            await _stateStore.SavePlanAsync(plan, cancellationToken);

            var agentCommand = BuildAgentCommand(request, firstStep, new List<AgentResponse>());
            await _publishEndpoint.Publish(agentCommand, cancellationToken);

            _logger.LogInformation("Published ExecuteAgentCommand for [{AgentName}] (Step {Order})",
                firstStep.AgentName, firstStep.Order);
        }

        sw.Stop();

        return new OrchestratorResult
        {
            WorkflowId = request.WorkflowId,
            CorrelationId = request.CorrelationId,
            Success = true,
            AgentResults = Array.Empty<AgentResponse>(),
            FinalReport = "Workflow accepted — processing asynchronously via message bus.",
            ExecutionPlan = planDescriptions,
            TotalExecutionTimeMs = sw.ElapsedMilliseconds
        };
    }

    internal static ExecutionPlan BuildExecutionPlan(WorkflowRequest request)
    {
        var plan = ExecutionPlan.Create(request.CorrelationId, request.Topic, request.WorkflowId);

        plan.AddStep(new WorkflowStep
        {
            Order = 1,
            AgentId = "market-trends-agent",
            AgentName = "MarketTrends",
            Intent = "AnalyseMarketTrends",
            Description = $"Analyse market trends for '{request.Topic}' in {request.Industry} ({request.Region})",
            DependsOnPreviousResults = false
        });
        plan.AddStep(new WorkflowStep
        {
            Order = 2,
            AgentId = "compliance-agent",
            AgentName = "Compliance",
            Intent = "AssessCompliance",
            Description = $"Assess regulatory compliance risks for '{request.Topic}' in {request.Region}",
            DependsOnPreviousResults = true
        });
        plan.AddStep(new WorkflowStep
        {
            Order = 3,
            AgentId = "costing-agent",
            AgentName = "Costing",
            Intent = "EstimateCosts",
            Description = $"Estimate costs and financial projections for '{request.Topic}'",
            DependsOnPreviousResults = true
        });
        plan.AddStep(new WorkflowStep
        {
            Order = 4,
            AgentId = "reporting-agent",
            AgentName = "ReportWriter",
            Intent = "GenerateReport",
            Description = "Synthesise all findings into a comprehensive final report",
            DependsOnPreviousResults = true
        });

        return plan;
    }

    internal static Shared.Contracts.Messages.ExecuteAgentCommand BuildAgentCommand(
        WorkflowRequest workflow, WorkflowStep step, IReadOnlyList<AgentResponse> previousResults)
    {
        var payloadJson = JsonSerializer.Serialize(new
        {
            topic = workflow.Topic,
            industry = workflow.Industry,
            region = workflow.Region,
            parameters = workflow.Parameters
        });

        using var doc = JsonDocument.Parse(payloadJson);

        return new Shared.Contracts.Messages.ExecuteAgentCommand
        {
            WorkflowId = workflow.WorkflowId,
            CorrelationId = workflow.CorrelationId,
            TargetAgentId = step.AgentId,
            Intent = step.Intent,
            StepOrder = step.Order,
            Payload = doc.RootElement.Clone(),
            PreviousResults = step.DependsOnPreviousResults
                ? new List<AgentResponse>(previousResults)
                : new List<AgentResponse>(),
            Metadata = new Dictionary<string, string>
            {
                ["workflowId"] = workflow.WorkflowId.ToString(),
                ["stepOrder"] = step.Order.ToString(),
                ["stepDescription"] = step.Description
            }
        };
    }
}
