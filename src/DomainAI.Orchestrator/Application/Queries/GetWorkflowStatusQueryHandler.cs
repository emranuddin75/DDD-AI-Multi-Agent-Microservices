using MediatR;
using DomainAI.Orchestrator.Domain;

namespace DomainAI.Orchestrator.Application.Queries;

public class GetWorkflowStatusQueryHandler : IRequestHandler<GetWorkflowStatusQuery, ExecutionPlan?>
{
    private readonly IWorkflowStateStore _stateStore;

    public GetWorkflowStatusQueryHandler(IWorkflowStateStore stateStore)
    {
        _stateStore = stateStore;
    }

    public async Task<ExecutionPlan?> Handle(GetWorkflowStatusQuery query, CancellationToken cancellationToken)
    {
        return await _stateStore.LoadPlanAsync(query.WorkflowId, cancellationToken);
    }
}
