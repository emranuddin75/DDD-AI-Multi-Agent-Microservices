namespace DomainAI.Agents.Reporting.Domain;

/// <summary>
/// Value object: a single section within a report.
/// Bounded context: Reporting.
/// </summary>
public sealed record ReportSection
{
    public int Order { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Content { get; init; } = string.Empty;
    public string SourceAgent { get; init; } = string.Empty;
}
