using MediatR;
using System.Text.Json;

namespace DomainAI.Agents.Reporting.Application.Queries;

/// <summary>
/// CQRS Query: Returns the current status/last generated report summary for the reporting agent.
/// </summary>
public record GetAgentStatusQuery(string Topic) : IRequest<JsonElement?>;
