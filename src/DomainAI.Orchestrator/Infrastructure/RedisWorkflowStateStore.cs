using System.Text.Json;
using DomainAI.Orchestrator.Domain;
using DomainAI.Shared.Contracts;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace DomainAI.Orchestrator.Infrastructure;

public sealed class RedisWorkflowStateStore : IWorkflowStateStore
{
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<RedisWorkflowStateStore> _logger;
    private static readonly TimeSpan DefaultExpiry = TimeSpan.FromHours(24);

    public RedisWorkflowStateStore(IConnectionMultiplexer redis, ILogger<RedisWorkflowStateStore> logger)
    {
        _redis = redis;
        _logger = logger;
    }

    private static string Key(Guid workflowId) => $"workflow:plan:{workflowId}";

    public async Task SavePlanAsync(ExecutionPlan plan, CancellationToken ct = default)
    {
        var db = _redis.GetDatabase();
        var dto = ExecutionPlanDto.FromDomain(plan);
        var json = JsonSerializer.Serialize(dto);
        await db.StringSetAsync(Key(plan.WorkflowId), json, DefaultExpiry);
        _logger.LogInformation("Saved execution plan for workflow {WorkflowId} to Redis", plan.WorkflowId);
    }

    public async Task<ExecutionPlan?> LoadPlanAsync(Guid workflowId, CancellationToken ct = default)
    {
        var db = _redis.GetDatabase();
        var json = await db.StringGetAsync(Key(workflowId));
        if (json.IsNullOrEmpty)
        {
            _logger.LogWarning("No execution plan found in Redis for workflow {WorkflowId}", workflowId);
            return null;
        }

        var dto = JsonSerializer.Deserialize<ExecutionPlanDto>(json!);
        return dto?.ToDomain();
    }

    public async Task RemovePlanAsync(Guid workflowId, CancellationToken ct = default)
    {
        var db = _redis.GetDatabase();
        await db.KeyDeleteAsync(Key(workflowId));
        _logger.LogInformation("Removed execution plan for workflow {WorkflowId} from Redis", workflowId);
    }

    private sealed record ExecutionPlanDto
    {
        public Guid WorkflowId { get; init; }
        public string CorrelationId { get; init; } = string.Empty;
        public string Topic { get; init; } = string.Empty;
        public List<WorkflowStep> Steps { get; init; } = new();
        public List<AgentResponse> CompletedResults { get; init; } = new();

        public static ExecutionPlanDto FromDomain(ExecutionPlan plan) => new()
        {
            WorkflowId = plan.WorkflowId,
            CorrelationId = plan.CorrelationId,
            Topic = plan.Topic,
            Steps = plan.Steps.ToList(),
            CompletedResults = plan.CompletedResults.ToList()
        };

        public ExecutionPlan ToDomain()
        {
            var plan = ExecutionPlan.Create(CorrelationId, Topic, WorkflowId);
            plan.RestoreSteps(Steps);
            plan.RestoreResults(CompletedResults);
            return plan;
        }
    }
}
