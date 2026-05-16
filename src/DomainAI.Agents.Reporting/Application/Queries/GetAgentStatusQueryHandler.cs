using MediatR;
using System.Text.Json;

namespace DomainAI.Agents.Reporting.Application.Queries;

public class GetAgentStatusQueryHandler : IRequestHandler<GetAgentStatusQuery, JsonElement?>
{
    public Task<JsonElement?> Handle(GetAgentStatusQuery query, CancellationToken cancellationToken)
    {
        var mockStatus = new
        {
            agentId = "reporting-agent",
            topic = query.Topic,
            lastStatus = "Processed",
            sectionCount = 4,
            processedAt = DateTimeOffset.UtcNow
        };
        return Task.FromResult<JsonElement?>(JsonSerializer.SerializeToElement(mockStatus));
    }
}
