namespace DomainAI.Orchestrator.Domain;

public interface IWorkflowStateStore
{
    Task SavePlanAsync(ExecutionPlan plan, CancellationToken ct = default);
    Task<ExecutionPlan?> LoadPlanAsync(Guid workflowId, CancellationToken ct = default);
    Task RemovePlanAsync(Guid workflowId, CancellationToken ct = default);
}
