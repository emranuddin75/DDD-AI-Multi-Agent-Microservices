using System.Text.Json.Serialization;
using DomainAI.Shared.Contracts;

namespace DomainAI.Orchestrator.Domain;

public sealed class ExecutionPlan
{
    private readonly List<WorkflowStep> _steps = new();
    private readonly List<AgentResponse> _completedResults = new();

    [JsonInclude]
    public IReadOnlyList<WorkflowStep> Steps => _steps.AsReadOnly();

    [JsonInclude]
    public IReadOnlyList<AgentResponse> CompletedResults => _completedResults.AsReadOnly();

    public string CorrelationId { get; private init; }
    public string Topic { get; private init; }
    public Guid WorkflowId { get; private init; }

    [JsonConstructor]
    private ExecutionPlan(string correlationId, string topic, Guid workflowId)
    {
        CorrelationId = correlationId;
        Topic = topic;
        WorkflowId = workflowId;
    }

    public static ExecutionPlan Create(string correlationId, string topic, Guid workflowId = default) =>
        new(correlationId, topic, workflowId == default ? Guid.NewGuid() : workflowId);

    public void AddStep(WorkflowStep step) => _steps.Add(step);

    public void MarkStepCompleted(int stepOrder, AgentResponse response)
    {
        var idx = _steps.FindIndex(s => s.Order == stepOrder);
        if (idx >= 0)
        {
            _steps[idx] = _steps[idx] with { Status = StepStatus.Completed };
            _completedResults.Add(response);
        }
    }

    public void MarkStepFailed(int stepOrder, AgentResponse response)
    {
        var idx = _steps.FindIndex(s => s.Order == stepOrder);
        if (idx >= 0)
        {
            _steps[idx] = _steps[idx] with { Status = StepStatus.Failed };
            _completedResults.Add(response);
        }
    }

    public void MarkStepInProgress(int stepOrder)
    {
        var idx = _steps.FindIndex(s => s.Order == stepOrder);
        if (idx >= 0)
            _steps[idx] = _steps[idx] with { Status = StepStatus.InProgress };
    }

    public WorkflowStep? GetNextPendingStep() =>
        _steps.FirstOrDefault(s => s.Status == StepStatus.Pending);

    public bool AllStepsCompleted =>
        _steps.All(s => s.Status is StepStatus.Completed or StepStatus.Failed);

    public bool HasFailures =>
        _steps.Any(s => s.Status == StepStatus.Failed);

    public IEnumerable<string> Describe() =>
        _steps.Select(s => $"Step {s.Order}: [{s.AgentName}] — {s.Description} ({s.Status})");

    internal void RestoreSteps(IEnumerable<WorkflowStep> steps)
    {
        _steps.Clear();
        _steps.AddRange(steps);
    }

    internal void RestoreResults(IEnumerable<AgentResponse> results)
    {
        _completedResults.Clear();
        _completedResults.AddRange(results);
    }
}
