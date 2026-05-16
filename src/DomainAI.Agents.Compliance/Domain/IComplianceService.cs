using System.Threading;
using System.Threading.Tasks;

namespace DomainAI.Agents.Compliance.Domain;

/// <summary>
/// Domain service interface for compliance assessment.
/// DDD: This interface belongs to the Domain layer; its implementation
///      (using a real LLM) belongs to the Infrastructure layer.
/// </summary>
public interface IComplianceService
{
    Task<ComplianceAssessment> AssessComplianceAsync(string topic, string region, CancellationToken ct = default);
}
