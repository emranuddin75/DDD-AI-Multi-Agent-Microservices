namespace DomainAI.Orchestrator.Domain;

public sealed record WorkflowStep
{
    public int Order { get; init; }
    public string AgentId { get; init; } = string.Empty;
    public string AgentName { get; init; } = string.Empty;
    public string Intent { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public bool DependsOnPreviousResults { get; init; }
    public StepStatus Status { get; init; } = StepStatus.Pending;
}

public enum StepStatus
{
    Pending,
    InProgress,
    Completed,
    Failed
}
