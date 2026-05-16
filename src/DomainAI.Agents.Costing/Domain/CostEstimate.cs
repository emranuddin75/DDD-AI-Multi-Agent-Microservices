namespace DomainAI.Agents.Costing.Domain;

/// <summary>
/// Aggregate root for project cost estimation.
/// Enforces domain rules (e.g. contingency must be applied to subtotals).
/// </summary>
public sealed class CostEstimate
{
    private readonly List<CostItem> _items = new();
    private const decimal ContingencyRate = 0.15m;

    public string Topic { get; private set; }
    public string Currency { get; private set; }
    public IReadOnlyList<CostItem> Items => _items.AsReadOnly();

    public decimal Subtotal => _items.Sum(i => i.TotalCost);
    public decimal Contingency => Math.Round(Subtotal * ContingencyRate, 2);
    public decimal TotalCost => Subtotal + Contingency;

    private CostEstimate(string topic, string currency)
    {
        Topic = topic;
        Currency = currency;
    }

    public static CostEstimate Create(string topic, string currency = "GBP") =>
        new(topic, currency);

    public void AddItem(CostItem item)
    {
        if (item.UnitCost < 0) throw new InvalidOperationException("Unit cost cannot be negative.");
        _items.Add(item);
    }

    public Dictionary<CostCategory, decimal> GetBreakdownByCategory() =>
        _items.GroupBy(i => i.Category)
              .ToDictionary(g => g.Key, g => g.Sum(i => i.TotalCost));
}
