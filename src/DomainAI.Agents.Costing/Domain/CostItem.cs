namespace DomainAI.Agents.Costing.Domain;

public enum CostCategory { Infrastructure, Labour, Licensing, Compliance, Contingency, Other }

/// <summary>
/// Value object representing a single cost line item.
/// Domain model within the Costing bounded context.
/// </summary>
public sealed record CostItem
{
    public CostCategory Category { get; init; }
    public string Description { get; init; } = string.Empty;
    public decimal UnitCost { get; init; }
    public int Quantity { get; init; }
    public string Unit { get; init; } = string.Empty;
    public decimal TotalCost => UnitCost * Quantity;
}
