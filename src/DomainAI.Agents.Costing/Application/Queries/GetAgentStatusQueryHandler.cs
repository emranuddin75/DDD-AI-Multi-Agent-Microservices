using MediatR;
using System.Text.Json;

namespace DomainAI.Agents.Costing.Application.Queries;

public class GetAgentStatusQueryHandler : IRequestHandler<GetAgentStatusQuery, JsonElement?>
{
    public Task<JsonElement?> Handle(GetAgentStatusQuery query, CancellationToken cancellationToken)
    {
        var mockStatus = new
        {
            agentId = "costing-agent",
            topic = query.Topic,
            lastStatus = "Processed",
            lastTotalCost = 287425.0m,
            currency = "GBP",
            processedAt = DateTimeOffset.UtcNow
        };
        return Task.FromResult<JsonElement?>(JsonSerializer.SerializeToElement(mockStatus));
    }
}
