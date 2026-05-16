namespace DomainAI.Agents.Compliance.Domain;

public enum RiskLevel { Low, Medium, High, Critical }

/// <summary>
/// Value object representing a specific compliance risk.
/// Domain knowledge within the Compliance bounded context.
/// </summary>
public sealed record ComplianceRisk
{
    public string RiskId { get; init; } = Guid.NewGuid().ToString("N")[..8];
    public string Category { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public RiskLevel Level { get; init; }
    public string Regulation { get; init; } = string.Empty;
    public string Mitigation { get; init; } = string.Empty;
}
