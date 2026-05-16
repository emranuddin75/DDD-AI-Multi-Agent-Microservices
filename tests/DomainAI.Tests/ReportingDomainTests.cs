using DomainAI.Agents.Reporting.Domain;
using Xunit;

namespace DomainAI.Tests;

public class ReportingDomainTests
{
    [Fact]
    public void Report_Render_ContainsTitle()
    {
        var report = Report.Create("Test Report Title");
        report.SetExecutiveSummary("This is a summary.");

        var rendered = report.Render();
        Assert.Contains("Test Report Title", rendered);
        Assert.Contains("This is a summary.", rendered);
    }

    [Fact]
    public void Report_Render_IncludesSectionsInOrder()
    {
        var report = Report.Create("Test Report");
        report.SetExecutiveSummary("Summary");
        report.AddSection(new ReportSection { Order = 2, Title = "Section B", Content = "Content B", SourceAgent = "AgentB" });
        report.AddSection(new ReportSection { Order = 1, Title = "Section A", Content = "Content A", SourceAgent = "AgentA" });

        var rendered = report.Render();
        var posA = rendered.IndexOf("Section A");
        var posB = rendered.IndexOf("Section B");
        Assert.True(posA < posB, "Section A should appear before Section B");
    }

    [Fact]
    public void Report_Render_IncludesGenerationMetadata()
    {
        var report = Report.Create("Report");
        report.SetExecutiveSummary("Summary");

        var rendered = report.Render();
        Assert.Contains("Generated:", rendered);
        Assert.Contains("DomainAI Multi-Agent System", rendered);
    }
}
