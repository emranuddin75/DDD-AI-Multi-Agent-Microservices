using System.Threading;
using System.Threading.Tasks;

namespace DomainAI.Agents.MarketTrends.Domain;

/// <summary>
/// Domain service interface for market analysis.
/// DDD: This interface belongs to the Domain layer, while its implementation
///      (using a real LLM) belongs to the Infrastructure layer.
/// </summary>
public interface IMarketAnalysisService
{
    Task<TrendAnalysis> AnalyzeMarketAsync(string topic, string industry, string region, CancellationToken ct = default);
}
