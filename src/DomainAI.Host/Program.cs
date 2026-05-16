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
using DomainAI.Shared.Domain;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureLogging(logging =>
    {
        logging.ClearProviders();
        logging.AddConsole();
        logging.SetMinimumLevel(LogLevel.Information);
    })
    .ConfigureServices(services =>
    {
        // Register MediatR — scan all bounded-context assemblies for handlers
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(MarketTrendsAgent).Assembly);
            cfg.RegisterServicesFromAssembly(typeof(ComplianceAgent).Assembly);
            cfg.RegisterServicesFromAssembly(typeof(CostingAgent).Assembly);
            cfg.RegisterServicesFromAssembly(typeof(ReportingAgent).Assembly);
            cfg.RegisterServicesFromAssembly(typeof(WorkflowOrchestrator).Assembly);
        });

        // MassTransit: In-memory transport for the host demo
        services.AddMassTransit(x =>
        {
            x.AddConsumer<AgentTaskCompletedConsumer>();
            x.UsingInMemory((context, cfg) =>
            {
                cfg.ConfigureEndpoints(context);
            });
        });

        // State store: In-memory for the host demo
        services.AddSingleton<IWorkflowStateStore, InMemoryWorkflowStateStore>();

        // Register stub domain services (no LLM required for the host demo)
        services.AddSingleton<IMarketAnalysisService, StubMarketAnalysisService>();
        services.AddSingleton<IComplianceService, StubComplianceService>();
        services.AddSingleton<ICostingService, StubCostingService>();
        services.AddSingleton<IReportingService, StubReportingService>();

        // Register all specialist agents (each is a separate bounded context)
        services.AddSingleton<MarketTrendsAgent>();
        services.AddSingleton<ComplianceAgent>();
        services.AddSingleton<CostingAgent>();
        services.AddSingleton<ReportingAgent>();

        // Register the central orchestrator
        services.AddSingleton<WorkflowOrchestrator>();
        services.AddSingleton<IOrchestrator>(sp =>
        {
            var orchestrator = sp.GetRequiredService<WorkflowOrchestrator>();

            // Wire up agents into the orchestrator (Magentic registration)
            orchestrator.RegisterAgent(sp.GetRequiredService<MarketTrendsAgent>());
            orchestrator.RegisterAgent(sp.GetRequiredService<ComplianceAgent>());
            orchestrator.RegisterAgent(sp.GetRequiredService<CostingAgent>());
            orchestrator.RegisterAgent(sp.GetRequiredService<ReportingAgent>());

            return orchestrator;
        });
    })
    .Build();

// Run a demonstration workflow
var orchestrator = host.Services.GetRequiredService<IOrchestrator>();
var logger = host.Services.GetRequiredService<ILogger<Program>>();

Console.WriteLine(new string('=', 70));
Console.WriteLine("  DomainAI — Resilient Message-Driven Multi-Agent System");
Console.WriteLine("  Architecture: DDD + CQRS + MassTransit + Redis State");
Console.WriteLine(new string('=', 70));
Console.WriteLine();

var workflowRequest = new WorkflowRequest
{
    Topic = "AI-powered Procurement Automation",
    Industry = "Financial Services",
    Region = "United Kingdom / EU",
    Parameters = new Dictionary<string, string>
    {
        ["budget"] = "500000",
        ["timeline"] = "12 months",
        ["priority"] = "compliance-first"
    }
};

Console.WriteLine($"Workflow Request: {workflowRequest.Topic}");
Console.WriteLine($"Industry: {workflowRequest.Industry} | Region: {workflowRequest.Region}");
Console.WriteLine($"Architecture: Message-driven via MassTransit (InMemory transport for demo)");
Console.WriteLine();

var result = await orchestrator.RunWorkflowAsync(workflowRequest);

Console.WriteLine();
Console.WriteLine(new string('=', 70));
Console.WriteLine($"  WORKFLOW ACCEPTED — Success: {result.Success}");
Console.WriteLine($"  Total time: {result.TotalExecutionTimeMs}ms");
Console.WriteLine($"  Note: Agents execute asynchronously via message bus");
Console.WriteLine(new string('=', 70));
Console.WriteLine();

if (result.Success)
{
    Console.WriteLine(result.FinalReport);
    if (result.ExecutionPlan?.Count > 0)
    {
        Console.WriteLine("\nExecution Plan:");
        foreach (var step in result.ExecutionPlan)
            Console.WriteLine($"  {step}");
    }
}
else
{
    Console.WriteLine($"ERROR: {result.ErrorMessage}");
    foreach (var agentResult in result.AgentResults)
    {
        Console.WriteLine($"  [{agentResult.SourceAgentName}]: {(agentResult.Success ? "OK" : "FAILED — " + agentResult.ErrorMessage)}");
    }
}

// ---------------------------------------------------------------------------
// Stub domain services used in the host demo.
// Provide deterministic results without requiring an LLM connection.
// ---------------------------------------------------------------------------

internal sealed class StubMarketAnalysisService : IMarketAnalysisService
{
    public Task<TrendAnalysis> AnalyzeMarketAsync(string topic, string industry, string region, CancellationToken ct = default)
    {
        var analysis = TrendAnalysis.Create(topic, industry, region);
        analysis.AddSignal(new MarketSignal
        {
            SignalType = "Digital",
            Description = $"Strong adoption growth for {topic} in {industry}",
            Confidence = 0.88,
            Direction = "bullish"
        });
        analysis.AddSignal(new MarketSignal
        {
            SignalType = "Regulatory",
            Description = "Increasing regulatory focus on AI governance",
            Confidence = 0.75,
            Direction = "neutral"
        });
        return Task.FromResult(analysis);
    }
}

internal sealed class StubComplianceService : IComplianceService
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

        assessment.AddRisk(new ComplianceRisk
        {
            Category = "ContractualLiability",
            Description = "Third-party vendor contracts must include AI liability clauses",
            Level = RiskLevel.Low,
            Regulation = "Contract Law / NEC4",
            Mitigation = "Review and update vendor agreements with AI-specific liability terms"
        });

        if (region.Contains("US", StringComparison.OrdinalIgnoreCase) ||
            region.Contains("United States", StringComparison.OrdinalIgnoreCase))
        {
            assessment.AddRisk(new ComplianceRisk
            {
                Category = "SectorRegulation",
                Description = $"SEC / FTC AI disclosure requirements applicable to {topic}",
                Level = RiskLevel.High,
                Regulation = "SEC AI Guidance 2024",
                Mitigation = "Ensure material AI use-cases are disclosed in filings"
            });
        }

        return Task.FromResult(assessment);
    }
}

internal sealed class StubCostingService : ICostingService
{
    public Task<CostEstimate> EstimateCostsAsync(string topic, string industry, bool hasHighComplianceRisk, CancellationToken ct = default)
    {
        var estimate = CostEstimate.Create(topic);

        estimate.AddItem(new CostItem
        {
            Category = CostCategory.Infrastructure,
            Description = "Cloud compute (AI inference workloads) — annual",
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
            Category = CostCategory.Labour,
            Description = "Data Engineer (1 FTE)",
            UnitCost = 6200m,
            Quantity = 12,
            Unit = "person-month"
        });
        estimate.AddItem(new CostItem
        {
            Category = CostCategory.Licensing,
            Description = $"AI platform licensing for {industry}",
            UnitCost = 18000m,
            Quantity = 1,
            Unit = "year"
        });
        estimate.AddItem(new CostItem
        {
            Category = CostCategory.Compliance,
            Description = hasHighComplianceRisk
                ? "Regulatory compliance programme (elevated — high risk identified)"
                : "Standard compliance and audit activities",
            UnitCost = hasHighComplianceRisk ? 35000m : 12000m,
            Quantity = 1,
            Unit = "year"
        });

        return Task.FromResult(estimate);
    }
}

internal sealed class StubReportingService : IReportingService
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
            var sectionTitle = agentResult.Domain switch
            {
                "MarketTrends" => "Market Analysis & Trends",
                "Compliance" => "Regulatory & Compliance Assessment",
                "Costing" => "Financial Estimates & Cost Breakdown",
                _ => $"{agentResult.Domain} Findings"
            };

            report.AddSection(new ReportSection
            {
                Order = order++,
                Title = sectionTitle,
                Content = agentResult.Summary,
                SourceAgent = agentResult.SourceAgentName
            });
        }

        var recommendations = new List<string>();
        var resultsList = previousResults.ToList();

        var marketResult = resultsList.FirstOrDefault(r => r.Domain == "MarketTrends");
        if (marketResult?.Success == true)
            recommendations.Add($"**Accelerate {topic} investment** — Market signals indicate a bullish trend with high-confidence technology adoption drivers.");

        var complianceResult = resultsList.FirstOrDefault(r => r.Domain == "Compliance");
        if (complianceResult?.Success == true)
            recommendations.Add("**Establish a Compliance Governance Board** — Assign a dedicated AI governance lead to manage regulatory obligations proactively.");

        var costingResult = resultsList.FirstOrDefault(r => r.Domain == "Costing");
        if (costingResult?.Success == true)
            recommendations.Add("**Phase the investment** — Stage infrastructure spending over 12 months to align with delivery milestones and manage cash flow.");

        recommendations.Add($"**Implement a feedback loop** — Define KPIs for {topic} outcomes and review quarterly against cost and compliance baselines.");
        recommendations.Add("**Run a Proof of Concept** — De-risk the programme with a 90-day PoC before full-scale deployment.");

        report.AddSection(new ReportSection
        {
            Order = order,
            Title = "Strategic Recommendations",
            Content = string.Join("\n\n", recommendations.Select((r, i) => $"{i + 1}. {r}")),
            SourceAgent = "Report Writer Agent"
        });

        return Task.FromResult(report);
    }
}
