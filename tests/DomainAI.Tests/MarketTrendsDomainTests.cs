using DomainAI.Agents.MarketTrends.Domain;
using Xunit;

namespace DomainAI.Tests;

public class MarketTrendsDomainTests
{
    [Fact]
    public void TrendAnalysis_Create_IsValid()
    {
        var analysis = TrendAnalysis.Create("AI Automation", "Finance", "UK");
        Assert.Equal("AI Automation", analysis.Topic);
        Assert.Empty(analysis.Signals);
        Assert.Equal(0.0, analysis.OverallSentiment);
    }

    [Fact]
    public void TrendAnalysis_BullishSignals_YieldPositiveSentiment()
    {
        var analysis = TrendAnalysis.Create("Topic", "Industry", "Region");
        analysis.AddSignal(new MarketSignal { SignalType = "Test", Description = "Growing", Confidence = 0.9, Direction = "bullish" });
        analysis.AddSignal(new MarketSignal { SignalType = "Test2", Description = "Growing2", Confidence = 0.8, Direction = "bullish" });

        Assert.True(analysis.OverallSentiment > 0);
        Assert.Equal("bullish", analysis.TrendDirection);
    }

    [Fact]
    public void TrendAnalysis_BearishSignals_YieldNegativeSentiment()
    {
        var analysis = TrendAnalysis.Create("Topic", "Industry", "Region");
        analysis.AddSignal(new MarketSignal { SignalType = "Test", Description = "Declining", Confidence = 0.9, Direction = "bearish" });
        analysis.AddSignal(new MarketSignal { SignalType = "Test2", Description = "Declining2", Confidence = 0.8, Direction = "bearish" });

        Assert.True(analysis.OverallSentiment < 0);
        Assert.Equal("bearish", analysis.TrendDirection);
    }

    [Fact]
    public void TrendAnalysis_MixedSignals_YieldNeutralSentiment()
    {
        var analysis = TrendAnalysis.Create("Topic", "Industry", "Region");
        analysis.AddSignal(new MarketSignal { SignalType = "T1", Description = "Up", Confidence = 0.5, Direction = "bullish" });
        analysis.AddSignal(new MarketSignal { SignalType = "T2", Description = "Down", Confidence = 0.5, Direction = "bearish" });

        Assert.Equal("neutral", analysis.TrendDirection);
    }
}
