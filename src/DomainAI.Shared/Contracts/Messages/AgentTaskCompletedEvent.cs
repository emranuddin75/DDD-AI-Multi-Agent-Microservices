using System.Text.Json;
using System.Text.Json.Serialization;

namespace DomainAI.Shared.Contracts.Messages;

public sealed record AgentTaskCompletedEvent
{
    [JsonPropertyName("eventId")]
    public Guid EventId { get; init; } = Guid.NewGuid();

    [JsonPropertyName("workflowId")]
    public Guid WorkflowId { get; init; }

    [JsonPropertyName("correlationId")]
    public string CorrelationId { get; init; } = string.Empty;

    [JsonPropertyName("sourceAgentId")]
    public string SourceAgentId { get; init; } = string.Empty;

    [JsonPropertyName("sourceAgentName")]
    public string SourceAgentName { get; init; } = string.Empty;

    [JsonPropertyName("stepOrder")]
    public int StepOrder { get; init; }

    [JsonPropertyName("success")]
    public bool Success { get; init; }

    [JsonPropertyName("agentResponse")]
    public AgentResponse AgentResponse { get; init; } = new();

    [JsonPropertyName("completedAt")]
    public DateTimeOffset CompletedAt { get; init; } = DateTimeOffset.UtcNow;
}
