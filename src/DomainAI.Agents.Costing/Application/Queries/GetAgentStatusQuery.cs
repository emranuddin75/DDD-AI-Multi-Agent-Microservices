using MediatR;
using System.Text.Json;

namespace DomainAI.Agents.Costing.Application.Queries;

/// <summary>
/// CQRS Query: Returns the current status/last known estimate for the costing agent.
/// </summary>
public record GetAgentStatusQuery(string Topic) : IRequest<JsonElement?>;
