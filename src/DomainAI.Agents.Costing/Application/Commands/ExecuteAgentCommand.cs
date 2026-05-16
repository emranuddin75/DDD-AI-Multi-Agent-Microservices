using DomainAI.Shared.Contracts;
using MediatR;

namespace DomainAI.Agents.Costing.Application.Commands;

/// <summary>
/// CQRS Command: Represents the intent to produce a cost estimate.
/// </summary>
public record ExecuteAgentCommand(AgentRequest Request) : IRequest<AgentResponse>;
