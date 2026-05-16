namespace DomainAI.Agents.MarketTrends.Domain;

/// <summary>
/// Aggregate root for a complete market trend analysis.
/// Encapsulates domain rules about what constitutes a valid analysis.
/// </summary>
public sealed class TrendAnalysis
{
    private readonly List<MarketSignal> _signals = new();

    public string Topic { get; private set; }
    public string Industry { get; private set; }
    public string Region { get; private set; }
    public IReadOnlyList<MarketSignal> Signals => _signals.AsReadOnly();
    public double OverallSentiment { get; private set; }
    public string TrendDirection { get; private set; } = "neutral";

    private TrendAnalysis(string topic, string industry, string region)
    {
        Topic = topic;
        Industry = industry;
        Region = region;
    }

    public static TrendAnalysis Create(string topic, string industry, string region) =>
        new(topic, industry, region);

    public void AddSignal(MarketSignal signal)
    {
        _signals.Add(signal);
        RecalculateSentiment();
    }

    private void RecalculateSentiment()
    {
        if (_signals.Count == 0) return;

        var avg = _signals.Average(s => s.Direction switch
        {
            "bullish" => s.Confidence,
            "bearish" => -s.Confidence,
            _ => 0.0
        });

        OverallSentiment = Math.Round(avg, 2);
        TrendDirection = avg switch
        {
            > 0.3 => "bullish",
            < -0.3 => "bearish",
            _ => "neutral"
        };
    }
}
