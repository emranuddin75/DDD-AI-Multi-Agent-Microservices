using System.Text.Json.Serialization;

namespace DomainAI.Shared.Contracts;

/// <summary>
/// Final aggregated result from the orchestrator after all agents complete.
/// </summary>
public sealed record OrchestratorResult
{
    [JsonPropertyName("workflowId")]
    public Guid WorkflowId { get; init; }

    [JsonPropertyName("correlationId")]
    public string CorrelationId { get; init; } = string.Empty;

    [JsonPropertyName("success")]
    public bool Success { get; init; }

    [JsonPropertyName("agentResults")]
    public IReadOnlyList<AgentResponse> AgentResults { get; init; } = Array.Empty<AgentResponse>();

    [JsonPropertyName("finalReport")]
    public string FinalReport { get; init; } = string.Empty;

    [JsonPropertyName("executionPlan")]
    public IReadOnlyList<string> ExecutionPlan { get; init; } = Array.Empty<string>();

    [JsonPropertyName("totalExecutionTimeMs")]
    public long TotalExecutionTimeMs { get; init; }

    [JsonPropertyName("completedAt")]
    public DateTimeOffset CompletedAt { get; init; } = DateTimeOffset.UtcNow;

    [JsonPropertyName("errorMessage")]
    public string? ErrorMessage { get; init; }
}
