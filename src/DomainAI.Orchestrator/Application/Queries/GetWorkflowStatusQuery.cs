using MediatR;
using DomainAI.Orchestrator.Domain;

namespace DomainAI.Orchestrator.Application.Queries;

public record GetWorkflowStatusQuery(Guid WorkflowId) : IRequest<ExecutionPlan?>;
