using DomainAI.Shared.Contracts;
using MediatR;

namespace DomainAI.Agents.Compliance.Application.Commands;

/// <summary>
/// CQRS Command: Represents the intent to perform a compliance assessment.
/// </summary>
public record ExecuteAgentCommand(AgentRequest Request) : IRequest<AgentResponse>;
