using DomainAI.Shared.Contracts;
using MediatR;

namespace DomainAI.Orchestrator.Application.Commands;

public record StartWorkflowCommand(WorkflowRequest Request) : IRequest<OrchestratorResult>;
