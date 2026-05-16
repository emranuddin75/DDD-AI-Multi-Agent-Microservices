using MediatR;
using System.Text.Json;

namespace DomainAI.Agents.Compliance.Application.Queries;

/// <summary>
/// CQRS Query: Returns the current status/last known result for the compliance agent.
/// In a full CQRS implementation this would query a separate read-model store.
/// </summary>
public record GetAgentStatusQuery(string Topic) : IRequest<JsonElement?>;
