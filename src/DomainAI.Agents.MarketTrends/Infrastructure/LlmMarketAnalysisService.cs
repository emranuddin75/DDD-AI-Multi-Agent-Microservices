using System.Text.Json;
using DomainAI.Agents.MarketTrends.Domain;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

namespace DomainAI.Agents.MarketTrends.Infrastructure;

/// <summary>
/// Infrastructure implementation of the market analysis service using an LLM.
/// </summary>
public sealed class LlmMarketAnalysisService : IMarketAnalysisService
{
    private readonly IChatClient _chatClient;
    private readonly ILogger<LlmMarketAnalysisService> _logger;

    public LlmMarketAnalysisService(IChatClient chatClient, ILogger<LlmMarketAnalysisService> logger)
    {
        _chatClient = chatClient;
        _logger = logger;
    }

    public async Task<TrendAnalysis> AnalyzeMarketAsync(string topic, string industry, string region, CancellationToken ct = default)
    {
        _logger.LogInformation("Analyzing market for {Topic} in {Industry} ({Region}) using LLM", topic, industry, region);

        var prompt = $"""
            Act as a market research expert. Analyze the market for '{topic}' in the '{industry}' industry within the '{region}' region.
            Return a JSON object with:
            - OverallSentiment (double, -1 to 1)
            - TrendDirection (string: "bullish", "bearish", or "neutral")
            - Signals (array of objects with SignalType, Description, Confidence (0-1), and Direction)
            """;

        var response = await _chatClient.GetResponseAsync(prompt, cancellationToken: ct);
        var responseText = response.Text ?? "{}";

        var analysis = TrendAnalysis.Create(topic, industry, region);

        try
        {
            var parsed = JsonSerializer.Deserialize<TrendAnalysisResponse>(responseText, 
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            
            if (parsed?.Signals != null)
            {
                foreach (var s in parsed.Signals)
                {
                    analysis.AddSignal(new MarketSignal 
                    { 
                        SignalType = s.SignalType,
                        Description = s.Description,
                        Confidence = s.Confidence,
                        Direction = s.Direction
                    });
                }
            }
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Failed to parse LLM response as structured data");
        }

        return analysis;
    }

    private record TrendAnalysisResponse(
        double OverallSentiment, 
        string TrendDirection, 
        List<SignalDto> Signals);

    private record SignalDto(
        string SignalType, 
        string Description, 
        double Confidence, 
        string Direction);
}
