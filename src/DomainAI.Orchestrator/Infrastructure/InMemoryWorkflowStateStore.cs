using System.Collections.Concurrent;
using DomainAI.Orchestrator.Domain;

namespace DomainAI.Orchestrator.Infrastructure;

public sealed class InMemoryWorkflowStateStore : IWorkflowStateStore
{
    private readonly ConcurrentDictionary<Guid, ExecutionPlan> _store = new();

    public Task SavePlanAsync(ExecutionPlan plan, CancellationToken ct = default)
    {
        _store[plan.WorkflowId] = plan;
        return Task.CompletedTask;
    }

    public Task<ExecutionPlan?> LoadPlanAsync(Guid workflowId, CancellationToken ct = default)
    {
        _store.TryGetValue(workflowId, out var plan);
        return Task.FromResult(plan);
    }

    public Task RemovePlanAsync(Guid workflowId, CancellationToken ct = default)
    {
        _store.TryRemove(workflowId, out _);
        return Task.CompletedTask;
    }
}
