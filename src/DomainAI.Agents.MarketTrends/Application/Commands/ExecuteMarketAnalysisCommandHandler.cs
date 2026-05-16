using DomainAI.Agents.MarketTrends.Domain;
using DomainAI.Agents.MarketTrends.Application.Notifications;
using DomainAI.Shared.Contracts;
using MediatR;
using System.Text.Json;

namespace DomainAI.Agents.MarketTrends.Application.Commands;

public class ExecuteMarketAnalysisCommandHandler : IRequestHandler<ExecuteMarketAnalysisCommand, AgentResponse>
{
    private readonly IMarketAnalysisService _analysisService;
    private readonly IMediator _mediator;

    public ExecuteMarketAnalysisCommandHandler(IMarketAnalysisService analysisService, IMediator mediator)
    {
        _analysisService = analysisService;
        _mediator = mediator;
    }

    public async Task<AgentResponse> Handle(ExecuteMarketAnalysisCommand command, CancellationToken cancellationToken)
    {
        var request = command.Request;
        var topic = request.Payload.TryGetProperty("topic", out var t) ? t.GetString() ?? "" : "";
        var industry = request.Payload.TryGetProperty("industry", out var i) ? i.GetString() ?? "" : "";
        var region = request.Payload.TryGetProperty("region", out var r) ? r.GetString() ?? "" : "";

        // Execute Domain Logic
        var analysis = await _analysisService.AnalyzeMarketAsync(topic, industry, region, cancellationToken);

        var resultPayload = new { 
            topic, industry, region, 
            sentiment = analysis.OverallSentiment,
            analyzedAt = DateTimeOffset.UtcNow 
        };

        var response = new AgentResponse
        {
            Success = true,
            Summary = $"Market analysis for {topic} completed.",
            Result = JsonSerializer.SerializeToElement(resultPayload)
        };

        // Publish Notification (Side Effect)
        await _mediator.Publish(new MarketAnalysisCompletedNotification(topic, analysis.OverallSentiment), cancellationToken);

        return response;
    }
}
