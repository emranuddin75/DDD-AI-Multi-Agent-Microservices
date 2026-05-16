using DomainAI.Agents.Compliance.Domain;
using Xunit;

namespace DomainAI.Tests;

public class ComplianceDomainTests
{
    [Fact]
    public void ComplianceAssessment_NoRisks_IsCompliantAndLow()
    {
        var assessment = ComplianceAssessment.Create("Topic", "UK");
        Assert.True(assessment.IsCompliant);
        Assert.Equal(RiskLevel.Low, assessment.OverallRiskLevel);
    }

    [Fact]
    public void ComplianceAssessment_HighRisk_IsNotCompliant()
    {
        var assessment = ComplianceAssessment.Create("Topic", "EU");
        assessment.AddRisk(new ComplianceRisk
        {
            Category = "DataPrivacy",
            Description = "GDPR breach risk",
            Level = RiskLevel.High,
            Regulation = "GDPR",
            Mitigation = "Encrypt data"
        });

        Assert.Equal(RiskLevel.High, assessment.OverallRiskLevel);
        Assert.False(assessment.IsCompliant);
    }

    [Fact]
    public void ComplianceAssessment_CriticalRisk_TakesHighestLevel()
    {
        var assessment = ComplianceAssessment.Create("Topic", "US");
        assessment.AddRisk(new ComplianceRisk { Description = "Risk 1", Level = RiskLevel.Low, Category = "C1", Regulation = "R1", Mitigation = "M1" });
        assessment.AddRisk(new ComplianceRisk { Description = "Risk 2", Level = RiskLevel.Critical, Category = "C2", Regulation = "R2", Mitigation = "M2" });

        Assert.Equal(RiskLevel.Critical, assessment.OverallRiskLevel);
    }

    [Fact]
    public void ComplianceAssessment_AddRisk_EmptyDescription_ThrowsException()
    {
        var assessment = ComplianceAssessment.Create("Topic", "UK");
        var invalidRisk = new ComplianceRisk { Description = "", Level = RiskLevel.Low, Category = "C1", Regulation = "R1", Mitigation = "M1" };

        Assert.Throws<InvalidOperationException>(() => assessment.AddRisk(invalidRisk));
    }

    [Fact]
    public void ComplianceAssessment_GetRisksAbove_FiltersCorrectly()
    {
        var assessment = ComplianceAssessment.Create("Topic", "UK");
        assessment.AddRisk(new ComplianceRisk { Description = "Low Risk", Level = RiskLevel.Low, Category = "C1", Regulation = "R1", Mitigation = "M1" });
        assessment.AddRisk(new ComplianceRisk { Description = "High Risk", Level = RiskLevel.High, Category = "C2", Regulation = "R2", Mitigation = "M2" });

        var highAndAbove = assessment.GetRisksAbove(RiskLevel.High).ToList();
        Assert.Single(highAndAbove);
        Assert.Equal("High Risk", highAndAbove[0].Description);
    }
}
