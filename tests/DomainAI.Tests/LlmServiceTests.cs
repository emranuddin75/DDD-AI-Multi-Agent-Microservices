using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using DomainAI.Agents.Compliance.Domain;
using DomainAI.Agents.Compliance.Infrastructure;
using DomainAI.Agents.Costing.Domain;
using DomainAI.Agents.Costing.Infrastructure;
using DomainAI.Agents.Reporting.Domain;
using DomainAI.Agents.Reporting.Infrastructure;
using DomainAI.Shared.Contracts;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace DomainAI.Tests;

/// <summary>
/// Unit tests for the LLM Infrastructure service implementations.
/// IChatClient is mocked so no real LLM connection is needed.
/// </summary>
public class LlmServiceTests
{
    // ---------------------------------------------------------------------------
    // LlmComplianceService
    // ---------------------------------------------------------------------------

    [Fact]
    public async Task LlmComplianceService_ValidJsonResponse_ParsesRisksCorrectly()
    {
        var llmJson = """
            {
                "Risks": [
                    { "Category": "DataPrivacy", "Description": "GDPR risk", "Level": "High", "Regulation": "GDPR", "Mitigation": "Encrypt data" },
                    { "Category": "AIGovernance", "Description": "AI Act risk", "Level": "Medium", "Regulation": "EU AI Act", "Mitigation": "Impact assessment" }
                ]
            }
            """;

        var chatClient = CreateMockChatClient(llmJson);
        var service = new LlmComplianceService(chatClient, NullLogger<LlmComplianceService>.Instance);

        var assessment = await service.AssessComplianceAsync("AI Procurement", "EU");

        Assert.NotNull(assessment);
        Assert.Equal("AI Procurement", assessment.Topic);
        Assert.Equal("EU", assessment.Region);
        Assert.Equal(2, assessment.Risks.Count);
        Assert.Equal(RiskLevel.High, assessment.OverallRiskLevel);
        Assert.False(assessment.IsCompliant);
    }

    [Fact]
    public async Task LlmComplianceService_MediumRiskOnly_IsCompliant()
    {
        var llmJson = """
            {
                "Risks": [
                    { "Category": "DataPrivacy", "Description": "Some risk", "Level": "Medium", "Regulation": "GDPR", "Mitigation": "Mitigate" }
                ]
            }
            """;

        var chatClient = CreateMockChatClient(llmJson);
        var service = new LlmComplianceService(chatClient, NullLogger<LlmComplianceService>.Instance);

        var assessment = await service.AssessComplianceAsync("Data Platform", "UK");

        Assert.True(assessment.IsCompliant);
        Assert.Equal(RiskLevel.Medium, assessment.OverallRiskLevel);
    }

    [Fact]
    public async Task LlmComplianceService_InvalidJson_ReturnsEmptyAssessment()
    {
        var chatClient = CreateMockChatClient("not valid json at all");
        var service = new LlmComplianceService(chatClient, NullLogger<LlmComplianceService>.Instance);

        var assessment = await service.AssessComplianceAsync("Topic", "UK");

        Assert.NotNull(assessment);
        Assert.Empty(assessment.Risks);
    }

    [Fact]
    public async Task LlmComplianceService_EmptyResponse_ReturnsEmptyAssessment()
    {
        var chatClient = CreateMockChatClient("{}");
        var service = new LlmComplianceService(chatClient, NullLogger<LlmComplianceService>.Instance);

        var assessment = await service.AssessComplianceAsync("Topic", "UK");

        Assert.NotNull(assessment);
        Assert.Empty(assessment.Risks);
    }

    // ---------------------------------------------------------------------------
    // LlmCostingService
    // ---------------------------------------------------------------------------

    [Fact]
    public async Task LlmCostingService_ValidJsonResponse_ParsesItemsCorrectly()
    {
        var llmJson = """
            {
                "Items": [
                    { "Category": "Infrastructure", "Description": "Cloud compute", "UnitCost": 2400, "Quantity": 12, "Unit": "month" },
                    { "Category": "Labour", "Description": "AI Engineers", "UnitCost": 7500, "Quantity": 24, "Unit": "person-month" }
                ]
            }
            """;

        var chatClient = CreateMockChatClient(llmJson);
        var service = new LlmCostingService(chatClient, NullLogger<LlmCostingService>.Instance);

        var estimate = await service.EstimateCostsAsync("AI Platform", "Finance", false);

        Assert.NotNull(estimate);
        Assert.Equal("AI Platform", estimate.Topic);
        Assert.Equal(2, estimate.Items.Count);
        Assert.True(estimate.Subtotal > 0);
        Assert.True(estimate.TotalCost > estimate.Subtotal);
    }

    [Fact]
    public async Task LlmCostingService_ContingencyApplied_Correctly()
    {
        var llmJson = """
            {
                "Items": [
                    { "Category": "Infrastructure", "Description": "Cloud", "UnitCost": 10000, "Quantity": 1, "Unit": "year" }
                ]
            }
            """;

        var chatClient = CreateMockChatClient(llmJson);
        var service = new LlmCostingService(chatClient, NullLogger<LlmCostingService>.Instance);

        var estimate = await service.EstimateCostsAsync("Platform", "Tech", false);

        Assert.Equal(10000m, estimate.Subtotal);
        Assert.Equal(1500m, estimate.Contingency);  // 15% of 10000
        Assert.Equal(11500m, estimate.TotalCost);
    }

    [Fact]
    public async Task LlmCostingService_InvalidJson_ReturnsEmptyEstimate()
    {
        var chatClient = CreateMockChatClient("not json");
        var service = new LlmCostingService(chatClient, NullLogger<LlmCostingService>.Instance);

        var estimate = await service.EstimateCostsAsync("Topic", "Industry", false);

        Assert.NotNull(estimate);
        Assert.Empty(estimate.Items);
        Assert.Equal(0m, estimate.Subtotal);
    }

    [Fact]
    public async Task LlmCostingService_HighComplianceRisk_PassedToPrompt()
    {
        string? capturedPrompt = null;
        var mockChatClient = new Mock<IChatClient>();
        // Mock the actual interface method GetResponseAsync(IEnumerable<ChatMessage>, ...)
        // NOT the extension method GetResponseAsync(string, ...) which Moq cannot intercept.
        mockChatClient
            .Setup(c => c.GetResponseAsync(
                It.IsAny<IEnumerable<ChatMessage>>(),
                It.IsAny<ChatOptions?>(),
                It.IsAny<CancellationToken>()))
            .Callback<IEnumerable<ChatMessage>, ChatOptions?, CancellationToken>(
                (msgs, _, _) => capturedPrompt = msgs.FirstOrDefault()?.Text)
            .ReturnsAsync(BuildChatResponse("{}"));

        var service = new LlmCostingService(mockChatClient.Object, NullLogger<LlmCostingService>.Instance);
        await service.EstimateCostsAsync("Platform", "Finance", true);

        Assert.NotNull(capturedPrompt);
        Assert.Contains("elevated compliance risk", capturedPrompt, StringComparison.OrdinalIgnoreCase);
    }

    // ---------------------------------------------------------------------------
    // LlmReportingService
    // ---------------------------------------------------------------------------

    [Fact]
    public async Task LlmReportingService_ValidJsonResponse_BuildsReportCorrectly()
    {
        var llmJson = """
            {
                "ExecutiveSummary": "This is the executive summary.",
                "Sections": [
                    { "Title": "Market Analysis", "Content": "Bullish signals.", "SourceAgent": "Market Agent" },
                    { "Title": "Compliance", "Content": "Medium risk.", "SourceAgent": "Compliance Agent" }
                ],
                "Recommendations": [
                    "Invest in AI",
                    "Phase the spend",
                    "Run a PoC"
                ]
            }
            """;

        var chatClient = CreateMockChatClient(llmJson);
        var service = new LlmReportingService(chatClient, NullLogger<LlmReportingService>.Instance);

        var previousResults = new List<AgentResponse>
        {
            new AgentResponse { Domain = "MarketTrends", SourceAgentName = "Market Agent", Success = true, Summary = "Bullish." }
        };

        var report = await service.GenerateReportAsync("AI Strategy", "Finance", "UK", previousResults);

        Assert.NotNull(report);
        Assert.Contains("AI Strategy", report.Title);
        Assert.Equal("This is the executive summary.", report.ExecutiveSummary);
        // 2 content sections + 1 recommendations section
        Assert.Equal(3, report.Sections.Count);
        Assert.Contains(report.Sections, s => s.Title == "Strategic Recommendations");
    }

    [Fact]
    public async Task LlmReportingService_InvalidJson_FallsBackToContextSummary()
    {
        var chatClient = CreateMockChatClient("not json");
        var service = new LlmReportingService(chatClient, NullLogger<LlmReportingService>.Instance);

        var previousResults = new List<AgentResponse>
        {
            new AgentResponse { Domain = "Compliance", SourceAgentName = "Compliance Agent", Success = true, Summary = "Medium risk." }
        };

        var report = await service.GenerateReportAsync("AI Project", "Tech", "UK", previousResults);

        Assert.NotNull(report);
        Assert.NotEmpty(report.ExecutiveSummary);
    }

    [Fact]
    public async Task LlmReportingService_RenderedReport_ContainsTitle()
    {
        var llmJson = """
            {
                "ExecutiveSummary": "Summary text.",
                "Sections": [{ "Title": "Key Findings", "Content": "Content here.", "SourceAgent": "Agent A" }],
                "Recommendations": ["Do X", "Do Y"]
            }
            """;

        var chatClient = CreateMockChatClient(llmJson);
        var service = new LlmReportingService(chatClient, NullLogger<LlmReportingService>.Instance);

        var report = await service.GenerateReportAsync("Cloud Migration", "IT", "Global", new List<AgentResponse>());
        var rendered = report.Render();

        Assert.Contains("Cloud Migration", rendered);
        Assert.Contains("Executive Summary", rendered);
        Assert.Contains("Key Findings", rendered);
    }

    // ---------------------------------------------------------------------------
    // Helpers
    // ---------------------------------------------------------------------------

    /// <summary>
    /// Creates a mock IChatClient that returns the given text for any call to the
    /// real interface method GetResponseAsync(IEnumerable&lt;ChatMessage&gt;, ...).
    /// Extension methods (e.g. GetResponseAsync(string, ...)) cannot be mocked with Moq;
    /// the LLM services have been updated to call the interface method directly.
    /// </summary>
    private static IChatClient CreateMockChatClient(string responseText)
    {
        var mock = new Mock<IChatClient>();
        mock.Setup(c => c.GetResponseAsync(
                It.IsAny<IEnumerable<ChatMessage>>(),
                It.IsAny<ChatOptions?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(BuildChatResponse(responseText));
        return mock.Object;
    }

    private static ChatResponse BuildChatResponse(string text)
    {
        var message = new ChatMessage(ChatRole.Assistant, text);
        return new ChatResponse(message);
    }
}
