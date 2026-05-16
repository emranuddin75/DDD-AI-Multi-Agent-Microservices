using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DomainAI.Shared.Contracts;

namespace DomainAI.Agents.Reporting.Domain;

/// <summary>
/// Domain service interface for report generation.
/// DDD: This interface belongs to the Domain layer; its implementation
///      (using a real LLM) belongs to the Infrastructure layer.
/// </summary>
public interface IReportingService
{
    Task<Report> GenerateReportAsync(string topic, string industry, string region, IReadOnlyList<AgentResponse> previousResults, CancellationToken ct = default);
}
