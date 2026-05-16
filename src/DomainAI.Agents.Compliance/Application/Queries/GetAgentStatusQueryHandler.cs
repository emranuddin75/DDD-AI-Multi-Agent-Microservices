using MediatR;
using System.Text.Json;

namespace DomainAI.Agents.Compliance.Application.Queries;

public class GetAgentStatusQueryHandler : IRequestHandler<GetAgentStatusQuery, JsonElement?>
{
    public Task<JsonElement?> Handle(GetAgentStatusQuery query, CancellationToken cancellationToken)
    {
        // In a real CQRS system this reads from a dedicated read-model (projection)
        var mockStatus = new
        {
            agentId = "compliance-agent",
            topic = query.Topic,
            lastStatus = "Processed",
            lastRiskLevel = "Medium",
            processedAt = DateTimeOffset.UtcNow
        };
        return Task.FromResult<JsonElement?>(JsonSerializer.SerializeToElement(mockStatus));
    }
}
