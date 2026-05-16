namespace DomainAI.Agents.Compliance.Domain;

/// <summary>
/// Aggregate root for a compliance assessment.
/// Enforces domain invariants around risk classification.
/// </summary>
public sealed class ComplianceAssessment
{
    private readonly List<ComplianceRisk> _risks = new();

    public string Topic { get; private set; }
    public string Region { get; private set; }
    public IReadOnlyList<ComplianceRisk> Risks => _risks.AsReadOnly();

    public RiskLevel OverallRiskLevel => _risks.Count == 0
        ? RiskLevel.Low
        : _risks.Max(r => r.Level);

    public bool IsCompliant => OverallRiskLevel <= RiskLevel.Medium;

    private ComplianceAssessment(string topic, string region)
    {
        Topic = topic;
        Region = region;
    }

    public static ComplianceAssessment Create(string topic, string region) =>
        new(topic, region);

    public void AddRisk(ComplianceRisk risk)
    {
        if (string.IsNullOrWhiteSpace(risk.Description))
            throw new InvalidOperationException("Risk description cannot be empty.");
        _risks.Add(risk);
    }

    public IEnumerable<ComplianceRisk> GetRisksAbove(RiskLevel threshold) =>
        _risks.Where(r => r.Level >= threshold);
}
