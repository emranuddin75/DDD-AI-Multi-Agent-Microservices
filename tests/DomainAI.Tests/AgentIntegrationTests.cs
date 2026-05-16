using System.Text.Json;
using DomainAI.Agents.Compliance.Application;
using DomainAI.Agents.Compliance.Domain;
using DomainAI.Agents.Costing.Application;
using DomainAI.Agents.Costing.Domain;
using DomainAI.Agents.MarketTrends.Application;
using DomainAI.Agents.MarketTrends.Domain;
using DomainAI.Agents.Reporting.Application;
using DomainAI.Agents.Reporting.Domain;
using DomainAI.Orchestrator.Application;
using DomainAI.Orchestrator.Consumers;
using DomainAI.Orchestrator.Domain;
using DomainAI.Orchestrator.Infrastructure;
using DomainAI.Shared.Contracts;
using MassTransit;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace DomainAI.Tests;

/// <summary>
/// Integration tests wiring real MediatR handlers via Microsoft DI.
/// Updated for the resilient, message-driven architecture with MassTransit and Redis.
/// </summary>
public class AgentIntegrationTests
{
    // ---------------------------------------------------------------------------
    // Helpers
    // ---------------------------------------------------------------------------

    private static AgentRequest BuildRequest(string topic = "Test Topic", string industry = "Tech", string region = "UK")
    {
        var payload = JsonSerializer.Serialize(new { topic, industry, region });
        using var doc = JsonDocument.Parse(payload);
        return new AgentRequest
        {
            Intent = "Test",
            Payload = doc.RootElement.Clone()
        };
    }

    /// <summary>Builds a full DI container with MediatR, MassTransit InMemory, and all stub domain services.</summary>
    private static ServiceProvider BuildServiceProvider()
    {
        var services = new ServiceCollection();

        services.AddLogging(b => b.AddProvider(NullLoggerProvider.Instance));

        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(MarketTrendsAgent).Assembly);
            cfg.RegisterServicesFromAssembly(typeof(ComplianceAgent).Assembly);
            cfg.RegisterServicesFromAssembly(typeof(CostingAgent).Assembly);
            cfg.RegisterServicesFromAssembly(typeof(ReportingAgent).Assembly);
            cfg.RegisterServicesFromAssembly(typeof(WorkflowOrchestrator).Assembly);
        });

        // MassTransit: In-memory bus for integration tests
        services.AddMassTransit(x =>
        {
            x.AddConsumer<AgentTaskCompletedConsumer>();
            x.UsingInMemory((context, cfg) =>
            {
                cfg.ConfigureEndpoints(context);
            });
        });

        // State store: In-memory for tests
        services.AddSingleton<IWorkflowStateStore, InMemoryWorkflowStateStore>();

        // Domain service stubs
        services.AddSingleton<IMarketAnalysisService, StubMarketAnalysisService>();
        services.AddSingleton<IComplianceService, StubComplianceService>();
        services.AddSingleton<ICostingService, StubCostingService>();
        services.AddSingleton<IReportingService, StubReportingService>();

        services.AddSingleton<MarketTrendsAgent>();
        services.AddSingleton<ComplianceAgent>();
        services.AddSingleton<CostingAgent>();
        services.AddSingleton<ReportingAgent>();
        services.AddSingleton<WorkflowOrchestrator>();

        return services.BuildServiceProvider();
    }

    // ---------------------------------------------------------------------------
    // Agent tests (unchanged — agents still work the same way)
    // ---------------------------------------------------------------------------

    [Fact]
    public async Task MarketTrendsAgent_Execute_ReturnsSuccess()
    {
        await using var sp = BuildServiceProvider();
        var agent = sp.GetRequiredService<MarketTrendsAgent>();
        var request = BuildRequest();

        var response = await agent.ExecuteAsync(request);

        Assert.True(response.Success);
        Assert.Equal("market-trends-agent", response.SourceAgentId);
        Assert.Equal("MarketTrends", response.Domain);
        Assert.NotEmpty(response.Summary);
    }

    [Fact]
    public async Task ComplianceAgent_Execute_ReturnsSuccess()
    {
        await using var sp = BuildServiceProvider();
        var agent = sp.GetRequiredService<ComplianceAgent>();
        var request = BuildRequest(region: "EU");

        var response = await agent.ExecuteAsync(request);

        Assert.True(response.Success);
        Assert.Equal("compliance-agent", response.SourceAgentId);
        Assert.Equal("Compliance", response.Domain);
        Assert.Contains("Compliance", response.Summary);
    }

    [Fact]
    public async Task CostingAgent_Execute_ReturnsSuccess()
    {
        await using var sp = BuildServiceProvider();
        var agent = sp.GetRequiredService<CostingAgent>();
        var request = BuildRequest();

        var response = await agent.ExecuteAsync(request);

        Assert.True(response.Success);
        Assert.Equal("costing-agent", response.SourceAgentId);
        Assert.Equal("Costing", response.Domain);
        Assert.Contains("GBP", response.Summary);
    }

    [Fact]
    public async Task CostingAgent_WithHighComplianceRisk_EscalatesCosts()
    {
        await using var sp = BuildServiceProvider();
        var agent = sp.GetRequiredService<CostingAgent>();

        var complianceResponse = new AgentResponse
        {
            Domain = "Compliance",
            Success = true,
            Summary = "Overall risk level is High. 3 risks identified."
        };

        var payload = JsonSerializer.Serialize(new { topic = "Topic", industry = "Finance", region = "EU" });
        using var doc = JsonDocument.Parse(payload);
        var request = new AgentRequest
        {
            Intent = "EstimateCosts",
            Payload = doc.RootElement.Clone(),
            PreviousResults = new List<AgentResponse> { complianceResponse }
        };

        var response = await agent.ExecuteAsync(request);

        Assert.True(response.Success);
        Assert.Contains("escalated", response.Summary, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ReportingAgent_Execute_GeneratesReport()
    {
        await using var sp = BuildServiceProvider();
        var agent = sp.GetRequiredService<ReportingAgent>();

        var previousResults = new List<AgentResponse>
        {
            new AgentResponse { Domain = "MarketTrends", SourceAgentName = "MarketTrends Agent", Success = true, Summary = "Market is bullish." },
            new AgentResponse { Domain = "Compliance", SourceAgentName = "Compliance Agent", Success = true, Summary = "Medium risk." },
            new AgentResponse { Domain = "Costing", SourceAgentName = "Costing Agent", Success = true, Summary = "Total GBP 250,000." }
        };

        var payload = JsonSerializer.Serialize(new { topic = "AI Strategy", industry = "Finance", region = "UK" });
        using var doc = JsonDocument.Parse(payload);
        var request = new AgentRequest
        {
            Intent = "GenerateReport",
            Payload = doc.RootElement.Clone(),
            PreviousResults = previousResults
        };

        var response = await agent.ExecuteAsync(request);

        Assert.True(response.Success);
        Assert.Contains("AI Strategy Report", response.Summary);
        Assert.Contains("Executive Summary", response.Summary);
    }

    // ---------------------------------------------------------------------------
    // Orchestrator tests — UPDATED for message-driven architecture
    // The orchestrator now returns an "accepted" response immediately;
    // agents execute asynchronously via the message bus.
    // ---------------------------------------------------------------------------

    [Fact]
    public async Task WorkflowOrchestrator_FullRun_ReturnsSuccess()
    {
        await using var sp = BuildServiceProvider();
        var orchestrator = sp.GetRequiredService<WorkflowOrchestrator>();
        orchestrator.RegisterAgent(sp.GetRequiredService<MarketTrendsAgent>());
        orchestrator.RegisterAgent(sp.GetRequiredService<ComplianceAgent>());
        orchestrator.RegisterAgent(sp.GetRequiredService<CostingAgent>());
        orchestrator.RegisterAgent(sp.GetRequiredService<ReportingAgent>());

        var request = new WorkflowRequest
        {
            Topic = "Cloud Migration",
            Industry = "Healthcare",
            Region = "UK"
        };

        var result = await orchestrator.RunWorkflowAsync(request);

        // Async architecture: workflow is accepted, agents run via message bus
        Assert.True(result.Success);
        Assert.NotEmpty(result.FinalReport);
        Assert.Contains("asynchronously", result.FinalReport);
        Assert.Equal(4, result.ExecutionPlan.Count);
    }

    [Fact]
    public async Task WorkflowOrchestrator_AgentsPassContextForward()
    {
        await using var sp = BuildServiceProvider();
        var orchestrator = sp.GetRequiredService<WorkflowOrchestrator>();
        orchestrator.RegisterAgent(sp.GetRequiredService<MarketTrendsAgent>());
        orchestrator.RegisterAgent(sp.GetRequiredService<ComplianceAgent>());
        orchestrator.RegisterAgent(sp.GetRequiredService<CostingAgent>());
        orchestrator.RegisterAgent(sp.GetRequiredService<ReportingAgent>());

        var result = await orchestrator.RunWorkflowAsync(new WorkflowRequest
        {
            Topic = "Data Platform",
            Industry = "Retail",
            Region = "EU"
        });

        // Async architecture: the plan contains all 4 steps
        Assert.Equal(4, result.ExecutionPlan.Count);
        Assert.Contains("MarketTrends", result.ExecutionPlan[0]);
        Assert.Contains("Compliance", result.ExecutionPlan[1]);
        Assert.Contains("Costing", result.ExecutionPlan[2]);
        Assert.Contains("ReportWriter", result.ExecutionPlan[3]);
    }

    [Fact]
    public async Task Agent_CanHandle_ReturnsTrueForMatchingId()
    {
        await using var sp = BuildServiceProvider();
        var agent = sp.GetRequiredService<MarketTrendsAgent>();
        var request = new AgentRequest { TargetAgentId = "market-trends-agent" };
        var canHandle = await agent.CanHandleAsync(request);
        Assert.True(canHandle);
    }

    [Fact]
    public async Task Agent_CanHandle_ReturnsFalseForDifferentId()
    {
        await using var sp = BuildServiceProvider();
        var agent = sp.GetRequiredService<MarketTrendsAgent>();
        var request = new AgentRequest { TargetAgentId = "some-other-agent" };
        var canHandle = await agent.CanHandleAsync(request);
        Assert.False(canHandle);
    }

    // ---------------------------------------------------------------------------
    // CQRS-specific tests — Commands, Queries, Notifications via IMediator directly
    // ---------------------------------------------------------------------------

    [Fact]
    public async Task ComplianceCommand_ViaMediator_ReturnsSuccess()
    {
        await using var sp = BuildServiceProvider();
        var mediator = sp.GetRequiredService<IMediator>();

        var request = BuildRequest(topic: "AI Governance", region: "EU");
        var response = await mediator.Send(
            new DomainAI.Agents.Compliance.Application.Commands.ExecuteAgentCommand(request));

        Assert.True(response.Success);
        Assert.Contains("Compliance assessment", response.Summary);
    }

    [Fact]
    public async Task CostingCommand_ViaMediator_ReturnsSuccess()
    {
        await using var sp = BuildServiceProvider();
        var mediator = sp.GetRequiredService<IMediator>();

        var request = BuildRequest(topic: "Cloud Infra", industry: "Finance");
        var response = await mediator.Send(
            new DomainAI.Agents.Costing.Application.Commands.ExecuteAgentCommand(request));

        Assert.True(response.Success);
        Assert.Contains("GBP", response.Summary);
    }

    [Fact]
    public async Task ReportingCommand_ViaMediator_ReturnsSuccess()
    {
        await using var sp = BuildServiceProvider();
        var mediator = sp.GetRequiredService<IMediator>();

        var payload = JsonSerializer.Serialize(new { topic = "Digital Strategy", industry = "Banking", region = "UK" });
        using var doc = JsonDocument.Parse(payload);
        var request = new AgentRequest
        {
            Intent = "GenerateReport",
            Payload = doc.RootElement.Clone()
        };
        var response = await mediator.Send(
            new DomainAI.Agents.Reporting.Application.Commands.ExecuteAgentCommand(request));

        Assert.True(response.Success);
        Assert.Contains("AI Strategy Report", response.Summary);
    }

    [Fact]
    public async Task ComplianceStatusQuery_ViaMediator_ReturnsData()
    {
        await using var sp = BuildServiceProvider();
        var mediator = sp.GetRequiredService<IMediator>();

        var result = await mediator.Send(
            new DomainAI.Agents.Compliance.Application.Queries.GetAgentStatusQuery("AI Procurement"));

        Assert.NotNull(result);
        var json = result.Value.GetRawText();
        Assert.Contains("compliance-agent", json);
    }

    [Fact]
    public async Task CostingStatusQuery_ViaMediator_ReturnsData()
    {
        await using var sp = BuildServiceProvider();
        var mediator = sp.GetRequiredService<IMediator>();

        var result = await mediator.Send(
            new DomainAI.Agents.Costing.Application.Queries.GetAgentStatusQuery("AI Procurement"));

        Assert.NotNull(result);
        var json = result.Value.GetRawText();
        Assert.Contains("costing-agent", json);
    }

    [Fact]
    public async Task ReportingStatusQuery_ViaMediator_ReturnsData()
    {
        await using var sp = BuildServiceProvider();
        var mediator = sp.GetRequiredService<IMediator>();

        var result = await mediator.Send(
            new DomainAI.Agents.Reporting.Application.Queries.GetAgentStatusQuery("AI Procurement"));

        Assert.NotNull(result);
        var json = result.Value.GetRawText();
        Assert.Contains("reporting-agent", json);
    }

    [Fact]
    public async Task WorkflowCompletedNotification_PublishesWithoutError()
    {
        await using var sp = BuildServiceProvider();
        var mediator = sp.GetRequiredService<IMediator>();

        var notification = new DomainAI.Orchestrator.Application.Notifications.WorkflowCompletedNotification(
            Guid.NewGuid(), "Test Topic", true, 123);

        await mediator.Publish(notification);
    }

    [Fact]
    public async Task ComplianceTaskCompletedNotification_PublishesWithoutError()
    {
        await using var sp = BuildServiceProvider();
        var mediator = sp.GetRequiredService<IMediator>();

        var notification = new DomainAI.Agents.Compliance.Application.Notifications.AgentTaskCompletedNotification(
            "compliance-agent", "Compliance Agent", "Test", true);

        await mediator.Publish(notification);
    }

    // ---------------------------------------------------------------------------
    // LLM Domain Service interface tests (using stubs)
    // ---------------------------------------------------------------------------

    [Fact]
    public async Task IComplianceService_Stub_AssessesCompliance()
    {
        await using var sp = BuildServiceProvider();
        var service = sp.GetRequiredService<IComplianceService>();

        var assessment = await service.AssessComplianceAsync("AI Procurement", "EU");

        Assert.NotNull(assessment);
        Assert.Equal("AI Procurement", assessment.Topic);
        Assert.Equal("EU", assessment.Region);
        Assert.NotEmpty(assessment.Risks);
    }

    [Fact]
    public async Task IComplianceService_Stub_EURegion_HasHighRisk()
    {
        await using var sp = BuildServiceProvider();
        var service = sp.GetRequiredService<IComplianceService>();

        var assessment = await service.AssessComplianceAsync("GDPR-Heavy System", "EU");

        Assert.Contains(assessment.Risks, r => r.Level >= RiskLevel.High);
    }

    [Fact]
    public async Task ICostingService_Stub_EstimatesCosts()
    {
        await using var sp = BuildServiceProvider();
        var service = sp.GetRequiredService<ICostingService>();

        var estimate = await service.EstimateCostsAsync("Cloud Platform", "Finance", false);

        Assert.NotNull(estimate);
        Assert.Equal("Cloud Platform", estimate.Topic);
        Assert.NotEmpty(estimate.Items);
        Assert.True(estimate.TotalCost > 0);
    }

    [Fact]
    public async Task ICostingService_Stub_HighCompliance_IncludesElevatedCost()
    {
        await using var sp = BuildServiceProvider();
        var service = sp.GetRequiredService<ICostingService>();

        var lowRiskEstimate = await service.EstimateCostsAsync("Platform", "Finance", false);
        var highRiskEstimate = await service.EstimateCostsAsync("Platform", "Finance", true);

        var lowComplianceCost = lowRiskEstimate.Items
            .Where(i => i.Category == CostCategory.Compliance)
            .Sum(i => i.TotalCost);

        var highComplianceCost = highRiskEstimate.Items
            .Where(i => i.Category == CostCategory.Compliance)
            .Sum(i => i.TotalCost);

        Assert.True(highComplianceCost > lowComplianceCost);
    }

    [Fact]
    public async Task IReportingService_Stub_GeneratesReport()
    {
        await using var sp = BuildServiceProvider();
        var service = sp.GetRequiredService<IReportingService>();

        var previousResults = new List<AgentResponse>
        {
            new AgentResponse { Domain = "MarketTrends", SourceAgentName = "Market Agent", Success = true, Summary = "Bullish market." },
            new AgentResponse { Domain = "Compliance", SourceAgentName = "Compliance Agent", Success = true, Summary = "Medium risk." }
        };

        var report = await service.GenerateReportAsync("AI Platform", "Finance", "UK", previousResults);

        Assert.NotNull(report);
        Assert.Contains("AI Platform", report.Title);
        Assert.NotEmpty(report.ExecutiveSummary);
        Assert.NotEmpty(report.Sections);
    }

    [Fact]
    public async Task IReportingService_Stub_EmptyPreviousResults_StillGeneratesReport()
    {
        await using var sp = BuildServiceProvider();
        var service = sp.GetRequiredService<IReportingService>();

        var report = await service.GenerateReportAsync("New Topic", "Tech", "UK", new List<AgentResponse>());

        Assert.NotNull(report);
        Assert.Contains("New Topic", report.Title);
    }

    // ---------------------------------------------------------------------------
    // Stub domain services for tests
    // ---------------------------------------------------------------------------

    private sealed class StubMarketAnalysisService : IMarketAnalysisService
    {
        public Task<TrendAnalysis> AnalyzeMarketAsync(string topic, string industry, string region, CancellationToken ct = default)
        {
            var analysis = TrendAnalysis.Create(topic, industry, region);
            analysis.AddSignal(new MarketSignal
            {
                SignalType = "Digital",
                Description = $"Growth in {topic}",
                Confidence = 0.9,
                Direction = "bullish"
            });
            return Task.FromResult(analysis);
        }
    }

    private sealed class StubComplianceService : IComplianceService
    {
        public Task<ComplianceAssessment> AssessComplianceAsync(string topic, string region, CancellationToken ct = default)
        {
            var assessment = ComplianceAssessment.Create(topic, region);

            assessment.AddRisk(new ComplianceRisk
            {
                Category = "DataPrivacy",
                Description = $"GDPR / regional data protection obligations for {topic} processing in {region}",
                Level = region.Contains("EU", StringComparison.OrdinalIgnoreCase) ? RiskLevel.High : RiskLevel.Medium,
                Regulation = "GDPR / CCPA",
                Mitigation = "Implement data minimisation, obtain explicit consent, appoint DPO if required"
            });

            assessment.AddRisk(new ComplianceRisk
            {
                Category = "AIGovernance",
                Description = $"EU AI Act classification requirements for AI-powered {topic} solutions",
                Level = RiskLevel.Medium,
                Regulation = "EU AI Act 2024",
                Mitigation = "Conduct AI impact assessment, implement human oversight mechanisms"
            });

            return Task.FromResult(assessment);
        }
    }

    private sealed class StubCostingService : ICostingService
    {
        public Task<CostEstimate> EstimateCostsAsync(string topic, string industry, bool hasHighComplianceRisk, CancellationToken ct = default)
        {
            var estimate = CostEstimate.Create(topic);

            estimate.AddItem(new CostItem
            {
                Category = CostCategory.Infrastructure,
                Description = "Cloud compute — annual",
                UnitCost = 2400m,
                Quantity = 12,
                Unit = "month"
            });
            estimate.AddItem(new CostItem
            {
                Category = CostCategory.Labour,
                Description = "AI/ML Engineers (2 FTE)",
                UnitCost = 7500m,
                Quantity = 24,
                Unit = "person-month"
            });
            estimate.AddItem(new CostItem
            {
                Category = CostCategory.Compliance,
                Description = hasHighComplianceRisk
                    ? "Elevated compliance programme"
                    : "Standard compliance activities",
                UnitCost = hasHighComplianceRisk ? 35000m : 12000m,
                Quantity = 1,
                Unit = "year"
            });

            return Task.FromResult(estimate);
        }
    }

    private sealed class StubReportingService : IReportingService
    {
        public Task<Report> GenerateReportAsync(
            string topic, string industry, string region,
            IReadOnlyList<AgentResponse> previousResults,
            CancellationToken ct = default)
        {
            var report = Report.Create($"AI Strategy Report: {topic}");

            var summaries = previousResults
                .Where(prev => prev.Success)
                .Select(prev => $"- **{prev.SourceAgentName}** ({prev.Domain}): {prev.Summary}")
                .ToList();

            var executiveSummary = summaries.Count > 0
                ? $"This report synthesises findings from {summaries.Count} specialist agents " +
                  $"regarding '{topic}' in the {industry} sector ({region}).\n\n" +
                  string.Join("\n", summaries)
                : $"Analysis of '{topic}' in {industry} ({region}).";

            report.SetExecutiveSummary(executiveSummary);

            int order = 1;
            foreach (var agentResult in previousResults.Where(prev => prev.Success))
            {
                report.AddSection(new ReportSection
                {
                    Order = order++,
                    Title = $"{agentResult.Domain} Findings",
                    Content = agentResult.Summary,
                    SourceAgent = agentResult.SourceAgentName
                });
            }

            return Task.FromResult(report);
        }
    }
}
