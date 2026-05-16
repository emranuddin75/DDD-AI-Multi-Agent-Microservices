using System.Text.Json;
using Xunit;

namespace DomainAI.Tests;

/// <summary>
/// Tests verifying the LlmSettings configuration for each Agent API project.
/// These tests read the actual appsettings.json files to confirm the correct
/// specialized models are configured per the Optimization principle.
/// </summary>
public class LlmSpecializationConfigTests
{
    private static readonly string SolutionRoot = Path.GetFullPath(
        Path.Combine(AppContext.BaseDirectory, "../../../../../"));

    private static JsonDocument LoadAppsettings(string projectRelativePath)
    {
        var path = Path.Combine(SolutionRoot, projectRelativePath);
        var json = File.ReadAllText(path);
        return JsonDocument.Parse(json);
    }

    private static string? GetLlmSetting(JsonDocument doc, string key)
    {
        if (doc.RootElement.TryGetProperty("LlmSettings", out var section))
        {
            if (section.TryGetProperty(key, out var value))
                return value.GetString();
        }
        return null;
    }

    // -------------------------------------------------------------------------
    // MarketTrends API — gpt-4o (high-reasoning trend analysis)
    // -------------------------------------------------------------------------

    [Fact]
    public void MarketTrends_Appsettings_HasLlmSettingsSection()
    {
        using var doc = LoadAppsettings("src/DomainAI.Agents.MarketTrends.Api/appsettings.json");
        Assert.NotNull(GetLlmSetting(doc, "Provider"));
        Assert.NotNull(GetLlmSetting(doc, "ModelName"));
    }

    [Fact]
    public void MarketTrends_Appsettings_UsesGpt4o()
    {
        using var doc = LoadAppsettings("src/DomainAI.Agents.MarketTrends.Api/appsettings.json");
        Assert.Equal("gpt-4o", GetLlmSetting(doc, "ModelName"));
        Assert.Equal("OpenAI", GetLlmSetting(doc, "Provider"));
    }

    // -------------------------------------------------------------------------
    // Compliance API — gpt-4-turbo (deep logic, regulatory checking)
    // -------------------------------------------------------------------------

    [Fact]
    public void Compliance_Appsettings_HasLlmSettingsSection()
    {
        using var doc = LoadAppsettings("src/DomainAI.Agents.Compliance.Api/appsettings.json");
        Assert.NotNull(GetLlmSetting(doc, "Provider"));
        Assert.NotNull(GetLlmSetting(doc, "ModelName"));
    }

    [Fact]
    public void Compliance_Appsettings_UsesGpt4Turbo()
    {
        using var doc = LoadAppsettings("src/DomainAI.Agents.Compliance.Api/appsettings.json");
        Assert.Equal("gpt-4-turbo", GetLlmSetting(doc, "ModelName"));
        Assert.Equal("OpenAI", GetLlmSetting(doc, "Provider"));
    }

    // -------------------------------------------------------------------------
    // Costing API — gpt-4o-mini (efficient, cost-effective estimation)
    // -------------------------------------------------------------------------

    [Fact]
    public void Costing_Appsettings_HasLlmSettingsSection()
    {
        using var doc = LoadAppsettings("src/DomainAI.Agents.Costing.Api/appsettings.json");
        Assert.NotNull(GetLlmSetting(doc, "Provider"));
        Assert.NotNull(GetLlmSetting(doc, "ModelName"));
    }

    [Fact]
    public void Costing_Appsettings_UsesGpt4oMini()
    {
        using var doc = LoadAppsettings("src/DomainAI.Agents.Costing.Api/appsettings.json");
        Assert.Equal("gpt-4o-mini", GetLlmSetting(doc, "ModelName"));
        Assert.Equal("OpenAI", GetLlmSetting(doc, "Provider"));
    }

    // -------------------------------------------------------------------------
    // Reporting API — gpt-4o (high-quality narrative synthesis)
    // -------------------------------------------------------------------------

    [Fact]
    public void Reporting_Appsettings_HasLlmSettingsSection()
    {
        using var doc = LoadAppsettings("src/DomainAI.Agents.Reporting.Api/appsettings.json");
        Assert.NotNull(GetLlmSetting(doc, "Provider"));
        Assert.NotNull(GetLlmSetting(doc, "ModelName"));
    }

    [Fact]
    public void Reporting_Appsettings_UsesGpt4o()
    {
        using var doc = LoadAppsettings("src/DomainAI.Agents.Reporting.Api/appsettings.json");
        Assert.Equal("gpt-4o", GetLlmSetting(doc, "ModelName"));
        Assert.Equal("OpenAI", GetLlmSetting(doc, "Provider"));
    }

    // -------------------------------------------------------------------------
    // Cross-cutting: each agent has a distinct, appropriate model
    // -------------------------------------------------------------------------

    [Fact]
    public void EachAgent_HasConfiguredModel_AndModelsAreAppropriatelyChosen()
    {
        using var marketDoc   = LoadAppsettings("src/DomainAI.Agents.MarketTrends.Api/appsettings.json");
        using var compliDoc   = LoadAppsettings("src/DomainAI.Agents.Compliance.Api/appsettings.json");
        using var costingDoc  = LoadAppsettings("src/DomainAI.Agents.Costing.Api/appsettings.json");
        using var reportDoc   = LoadAppsettings("src/DomainAI.Agents.Reporting.Api/appsettings.json");

        var marketModel   = GetLlmSetting(marketDoc,  "ModelName");
        var compliModel   = GetLlmSetting(compliDoc,  "ModelName");
        var costingModel  = GetLlmSetting(costingDoc, "ModelName");
        var reportModel   = GetLlmSetting(reportDoc,  "ModelName");

        Assert.NotEmpty(marketModel!);
        Assert.NotEmpty(compliModel!);
        Assert.NotEmpty(costingModel!);
        Assert.NotEmpty(reportModel!);

        // Costing uses the lightweight model for cost-effective structured output
        Assert.Equal("gpt-4o-mini", costingModel);

        // High-capability model where reasoning quality matters
        Assert.Equal("gpt-4o", marketModel);
        Assert.Equal("gpt-4o", reportModel);

        // Deep-logic model for regulatory compliance
        Assert.Equal("gpt-4-turbo", compliModel);
    }

    // -------------------------------------------------------------------------
    // Program.cs: LLM service registration patterns
    // -------------------------------------------------------------------------

    [Theory]
    [InlineData("src/DomainAI.Agents.MarketTrends.Api/Program.cs", "LlmMarketAnalysisService")]
    [InlineData("src/DomainAI.Agents.Compliance.Api/Program.cs",   "LlmComplianceService")]
    [InlineData("src/DomainAI.Agents.Costing.Api/Program.cs",      "LlmCostingService")]
    [InlineData("src/DomainAI.Agents.Reporting.Api/Program.cs",    "LlmReportingService")]
    public void ProgramCs_RegistersRealLlmService_NotStubOrMock(string relativePath, string expectedService)
    {
        var content = File.ReadAllText(Path.Combine(SolutionRoot, relativePath));

        Assert.Contains(expectedService, content);
        Assert.DoesNotContain("MockMarketAnalysisService", content);
        Assert.DoesNotContain("StubComplianceService", content);
        Assert.DoesNotContain("StubCostingService", content);
        Assert.DoesNotContain("StubReportingService", content);
    }

    [Theory]
    [InlineData("src/DomainAI.Agents.MarketTrends.Api/Program.cs")]
    [InlineData("src/DomainAI.Agents.Compliance.Api/Program.cs")]
    [InlineData("src/DomainAI.Agents.Costing.Api/Program.cs")]
    [InlineData("src/DomainAI.Agents.Reporting.Api/Program.cs")]
    public void ProgramCs_RegistersAddChatClient_WithAsIChatClient(string relativePath)
    {
        var content = File.ReadAllText(Path.Combine(SolutionRoot, relativePath));

        Assert.Contains("AddChatClient", content);
        Assert.Contains("AsIChatClient()", content);
        Assert.Contains("OPENAI_API_KEY", content);
    }

    [Theory]
    [InlineData("src/DomainAI.Agents.MarketTrends.Api/Program.cs")]
    [InlineData("src/DomainAI.Agents.Compliance.Api/Program.cs")]
    [InlineData("src/DomainAI.Agents.Costing.Api/Program.cs")]
    [InlineData("src/DomainAI.Agents.Reporting.Api/Program.cs")]
    public void ProgramCs_ReadsModelNameFromLlmSettingsConfiguration(string relativePath)
    {
        var content = File.ReadAllText(Path.Combine(SolutionRoot, relativePath));

        Assert.Contains("LlmSettings", content);
        Assert.Contains("ModelName", content);
    }
}
