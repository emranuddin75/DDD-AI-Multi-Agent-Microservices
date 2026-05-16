using System.Text.Json;
using DomainAI.Orchestrator.Application.Commands;
using DomainAI.Orchestrator.Application.Queries;
using DomainAI.Orchestrator.Consumers;
using DomainAI.Orchestrator.Domain;
using DomainAI.Orchestrator.Infrastructure;
using DomainAI.Shared.Contracts;
using DomainAI.Shared.Contracts.Messages;
using MassTransit;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace DomainAI.Tests;

public class OrchestratorApiTests
{
    private static ServiceProvider BuildOrchestratorProvider()
    {
        var services = new ServiceCollection();

        services.AddLogging(b => b.AddProvider(NullLoggerProvider.Instance));

        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(DomainAI.Orchestrator.Application.WorkflowOrchestrator).Assembly);
        });

        services.AddMassTransit(x =>
        {
            x.AddConsumer<AgentTaskCompletedConsumer>();
            x.UsingInMemory((context, cfg) =>
            {
                cfg.ConfigureEndpoints(context);
            });
        });

        services.AddSingleton<IWorkflowStateStore, InMemoryWorkflowStateStore>();

        return services.BuildServiceProvider();
    }

    [Fact]
    public async Task StartWorkflowCommand_ViaMediator_ReturnsAccepted()
    {
        await using var sp = BuildOrchestratorProvider();
        var mediator = sp.GetRequiredService<IMediator>();

        var request = new WorkflowRequest
        {
            Topic = "AI Procurement",
            Industry = "Finance",
            Region = "UK"
        };

        var result = await mediator.Send(new StartWorkflowCommand(request));

        Assert.True(result.Success);
        Assert.Contains("asynchronously", result.FinalReport);
        Assert.Equal(4, result.ExecutionPlan.Count);
    }

    [Fact]
    public async Task GetWorkflowStatusQuery_AfterStart_ReturnsPlan()
    {
        await using var sp = BuildOrchestratorProvider();
        var mediator = sp.GetRequiredService<IMediator>();

        var request = new WorkflowRequest
        {
            Topic = "Cloud Migration",
            Industry = "Healthcare",
            Region = "EU"
        };

        var result = await mediator.Send(new StartWorkflowCommand(request));

        var plan = await mediator.Send(new GetWorkflowStatusQuery(result.WorkflowId));

        Assert.NotNull(plan);
        Assert.Equal(4, plan!.Steps.Count);
        Assert.Equal("Cloud Migration", plan.Topic);
    }

    [Fact]
    public async Task GetWorkflowStatusQuery_UnknownId_ReturnsNull()
    {
        await using var sp = BuildOrchestratorProvider();
        var mediator = sp.GetRequiredService<IMediator>();

        var plan = await mediator.Send(new GetWorkflowStatusQuery(Guid.NewGuid()));

        Assert.Null(plan);
    }

    [Fact]
    public void InMemoryWorkflowStateStore_SaveAndLoad_RoundTrips()
    {
        var store = new InMemoryWorkflowStateStore();
        var plan = ExecutionPlan.Create("corr-1", "Test Topic");

        plan.AddStep(new WorkflowStep
        {
            Order = 1,
            AgentId = "market-trends-agent",
            AgentName = "MarketTrends",
            Intent = "AnalyseMarketTrends",
            Description = "Test step"
        });

        store.SavePlanAsync(plan).Wait();
        var loaded = store.LoadPlanAsync(plan.WorkflowId).Result;

        Assert.NotNull(loaded);
        Assert.Single(loaded!.Steps);
        Assert.Equal("Test Topic", loaded.Topic);
    }

    [Fact]
    public void InMemoryWorkflowStateStore_Remove_ClearsPlan()
    {
        var store = new InMemoryWorkflowStateStore();
        var plan = ExecutionPlan.Create("corr-1", "Test");

        store.SavePlanAsync(plan).Wait();
        store.RemovePlanAsync(plan.WorkflowId).Wait();

        var loaded = store.LoadPlanAsync(plan.WorkflowId).Result;
        Assert.Null(loaded);
    }

    [Fact]
    public void ExecutionPlan_Create_HasCorrectProperties()
    {
        var plan = ExecutionPlan.Create("corr-123", "AI Strategy");

        Assert.Equal("corr-123", plan.CorrelationId);
        Assert.Equal("AI Strategy", plan.Topic);
        Assert.NotEqual(Guid.Empty, plan.WorkflowId);
        Assert.Empty(plan.Steps);
    }

    [Fact]
    public void ExecutionPlan_MarkStepCompleted_UpdatesStatus()
    {
        var plan = ExecutionPlan.Create("corr-1", "Topic");
        plan.AddStep(new WorkflowStep
        {
            Order = 1,
            AgentId = "agent-1",
            AgentName = "Agent1",
            Intent = "Test",
            Description = "Step 1"
        });

        plan.MarkStepInProgress(1);
        Assert.Equal(StepStatus.InProgress, plan.Steps[0].Status);

        plan.MarkStepCompleted(1, new AgentResponse { Success = true, Summary = "Done" });
        Assert.Equal(StepStatus.Completed, plan.Steps[0].Status);
        Assert.True(plan.AllStepsCompleted);
        Assert.False(plan.HasFailures);
    }

    [Fact]
    public void ExecutionPlan_MarkStepFailed_SetsFailureFlag()
    {
        var plan = ExecutionPlan.Create("corr-1", "Topic");
        plan.AddStep(new WorkflowStep
        {
            Order = 1,
            AgentId = "agent-1",
            AgentName = "Agent1",
            Intent = "Test",
            Description = "Step 1"
        });

        plan.MarkStepFailed(1, new AgentResponse { Success = false, Summary = "Error" });

        Assert.True(plan.AllStepsCompleted);
        Assert.True(plan.HasFailures);
    }

    [Fact]
    public void ExecutionPlan_GetNextPendingStep_SkipsCompleted()
    {
        var plan = ExecutionPlan.Create("corr-1", "Topic");
        plan.AddStep(new WorkflowStep { Order = 1, AgentId = "a1", AgentName = "A1", Intent = "I1", Description = "D1" });
        plan.AddStep(new WorkflowStep { Order = 2, AgentId = "a2", AgentName = "A2", Intent = "I2", Description = "D2" });

        plan.MarkStepCompleted(1, new AgentResponse { Success = true });

        var next = plan.GetNextPendingStep();
        Assert.NotNull(next);
        Assert.Equal(2, next!.Order);
    }

    [Fact]
    public void ExecutionPlan_Describe_ReturnsStepDescriptions()
    {
        var plan = ExecutionPlan.Create("corr-1", "Topic");
        plan.AddStep(new WorkflowStep { Order = 1, AgentId = "a1", AgentName = "MarketTrends", Intent = "I1", Description = "Analyse market" });

        var desc = plan.Describe().ToList();
        Assert.Single(desc);
        Assert.Contains("MarketTrends", desc[0]);
        Assert.Contains("Analyse market", desc[0]);
    }

    [Fact]
    public void StartWorkflowCommandHandler_BuildExecutionPlan_Has4Steps()
    {
        var request = new WorkflowRequest
        {
            Topic = "Test",
            Industry = "Tech",
            Region = "UK"
        };

        var plan = StartWorkflowCommandHandler.BuildExecutionPlan(request);

        Assert.Equal(4, plan.Steps.Count);
        Assert.Equal("market-trends-agent", plan.Steps[0].AgentId);
        Assert.Equal("compliance-agent", plan.Steps[1].AgentId);
        Assert.Equal("costing-agent", plan.Steps[2].AgentId);
        Assert.Equal("reporting-agent", plan.Steps[3].AgentId);
    }

    [Fact]
    public void StartWorkflowCommandHandler_BuildAgentCommand_SetsPayload()
    {
        var request = new WorkflowRequest
        {
            Topic = "AI Platform",
            Industry = "Finance",
            Region = "EU"
        };

        var step = new WorkflowStep
        {
            Order = 1,
            AgentId = "market-trends-agent",
            AgentName = "MarketTrends",
            Intent = "AnalyseMarketTrends",
            Description = "Test step",
            DependsOnPreviousResults = false
        };

        var cmd = StartWorkflowCommandHandler.BuildAgentCommand(request, step, new List<AgentResponse>());

        Assert.Equal("market-trends-agent", cmd.TargetAgentId);
        Assert.Equal("AnalyseMarketTrends", cmd.Intent);
        Assert.Equal(1, cmd.StepOrder);
        Assert.Equal("AI Platform", cmd.Payload.GetProperty("topic").GetString());
    }

    [Fact]
    public void WorkflowStep_DefaultStatus_IsPending()
    {
        var step = new WorkflowStep
        {
            Order = 1,
            AgentId = "test-agent",
            AgentName = "Test",
            Intent = "Test",
            Description = "Test step"
        };

        Assert.Equal(StepStatus.Pending, step.Status);
    }

    [Fact]
    public void AgentTaskCompletedEvent_HasDefaultValues()
    {
        var evt = new AgentTaskCompletedEvent
        {
            WorkflowId = Guid.NewGuid(),
            SourceAgentId = "test-agent"
        };

        Assert.NotEqual(Guid.Empty, evt.EventId);
        Assert.Equal("test-agent", evt.SourceAgentId);
        Assert.NotEqual(default, evt.CompletedAt);
    }

    [Fact]
    public void ExecuteAgentCommand_Message_HasDefaultValues()
    {
        var cmd = new DomainAI.Shared.Contracts.Messages.ExecuteAgentCommand
        {
            WorkflowId = Guid.NewGuid(),
            TargetAgentId = "market-trends-agent",
            Intent = "AnalyseMarketTrends"
        };

        Assert.NotEqual(Guid.Empty, cmd.CommandId);
        Assert.Equal("market-trends-agent", cmd.TargetAgentId);
        Assert.NotEqual(default, cmd.PublishedAt);
    }
}
