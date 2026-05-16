using System.Threading;
using System.Threading.Tasks;

namespace DomainAI.Agents.Costing.Domain;

/// <summary>
/// Domain service interface for cost estimation.
/// DDD: This interface belongs to the Domain layer; its implementation
///      (using a real LLM) belongs to the Infrastructure layer.
/// </summary>
public interface ICostingService
{
    Task<CostEstimate> EstimateCostsAsync(string topic, string industry, bool hasHighComplianceRisk, CancellationToken ct = default);
}
