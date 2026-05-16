namespace DomainAI.Shared.Domain;

/// <summary>
/// Orchestrator (Magentic/Manager-Planner pattern) that coordinates
/// all specialized agents across bounded contexts.
/// </summary>
public interface IOrchestrator
{
    Task<OrchestratorResult> RunWorkflowAsync(WorkflowRequest request, CancellationToken cancellationToken = default);
    void RegisterAgent(IAgent agent);
}
