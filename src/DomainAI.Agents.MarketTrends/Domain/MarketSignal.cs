namespace DomainAI.Agents.MarketTrends.Domain;

/// <summary>
/// Domain entity representing a detected market signal or trend data point.
/// Lives exclusively within the MarketTrends bounded context.
/// </summary>
public sealed record MarketSignal
{
    public string SignalType { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public double Confidence { get; init; }
    public string Direction { get; init; } = string.Empty; // "bullish" | "bearish" | "neutral"
    public DateTimeOffset DetectedAt { get; init; } = DateTimeOffset.UtcNow;
}
