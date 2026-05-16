using DomainAI.Shared.Contracts;
using MediatR;

namespace DomainAI.Agents.Reporting.Application.Commands;

/// <summary>
/// CQRS Command: Represents the intent to generate a final synthesised report.
/// </summary>
public record ExecuteAgentCommand(AgentRequest Request) : IRequest<AgentResponse>;
