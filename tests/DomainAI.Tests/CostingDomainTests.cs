using DomainAI.Agents.Costing.Domain;
using Xunit;

namespace DomainAI.Tests;

public class CostingDomainTests
{
    [Fact]
    public void CostEstimate_EmptyItems_HasZeroTotal()
    {
        var estimate = CostEstimate.Create("Topic");
        Assert.Equal(0m, estimate.Subtotal);
        Assert.Equal(0m, estimate.Contingency);
        Assert.Equal(0m, estimate.TotalCost);
    }

    [Fact]
    public void CostEstimate_Contingency_Is15Percent()
    {
        var estimate = CostEstimate.Create("Topic");
        estimate.AddItem(new CostItem
        {
            Category = CostCategory.Labour,
            Description = "Dev",
            UnitCost = 1000m,
            Quantity = 10,
            Unit = "day"
        });

        Assert.Equal(10000m, estimate.Subtotal);
        Assert.Equal(1500m, estimate.Contingency);
        Assert.Equal(11500m, estimate.TotalCost);
    }

    [Fact]
    public void CostEstimate_NegativeUnitCost_ThrowsException()
    {
        var estimate = CostEstimate.Create("Topic");
        var badItem = new CostItem
        {
            Category = CostCategory.Infrastructure,
            Description = "Bad",
            UnitCost = -100m,
            Quantity = 1,
            Unit = "unit"
        };

        Assert.Throws<InvalidOperationException>(() => estimate.AddItem(badItem));
    }

    [Fact]
    public void CostEstimate_BreakdownByCategory_IsCorrect()
    {
        var estimate = CostEstimate.Create("Topic");
        estimate.AddItem(new CostItem { Category = CostCategory.Labour, Description = "Dev", UnitCost = 500m, Quantity = 2, Unit = "day" });
        estimate.AddItem(new CostItem { Category = CostCategory.Labour, Description = "QA", UnitCost = 300m, Quantity = 2, Unit = "day" });
        estimate.AddItem(new CostItem { Category = CostCategory.Infrastructure, Description = "Cloud", UnitCost = 200m, Quantity = 1, Unit = "month" });

        var breakdown = estimate.GetBreakdownByCategory();
        Assert.Equal(1600m, breakdown[CostCategory.Labour]);
        Assert.Equal(200m, breakdown[CostCategory.Infrastructure]);
    }

    [Fact]
    public void CostItem_TotalCost_IsUnitCostTimesQuantity()
    {
        var item = new CostItem { UnitCost = 250m, Quantity = 8, Unit = "day", Category = CostCategory.Labour, Description = "Work" };
        Assert.Equal(2000m, item.TotalCost);
    }
}
