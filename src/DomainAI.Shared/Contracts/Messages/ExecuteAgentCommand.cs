using System.Text.Json;
using System.Text.Json.Serialization;

namespace DomainAI.Shared.Contracts.Messages;

public sealed record ExecuteAgentCommand
{
    [JsonPropertyName("commandId")]
    public Guid CommandId { get; init; } = Guid.NewGuid();

    [JsonPropertyName("workflowId")]
    public Guid WorkflowId { get; init; }

    [JsonPropertyName("correlationId")]
    public string CorrelationId { get; init; } = string.Empty;

    [JsonPropertyName("targetAgentId")]
    public string TargetAgentId { get; init; } = string.Empty;

    [JsonPropertyName("intent")]
    public string Intent { get; init; } = string.Empty;

    [JsonPropertyName("stepOrder")]
    public int StepOrder { get; init; }

    [JsonPropertyName("payload")]
    public JsonElement Payload { get; init; }

    [JsonPropertyName("previousResults")]
    public List<AgentResponse> PreviousResults { get; init; } = new();

    [JsonPropertyName("metadata")]
    public Dictionary<string, string> Metadata { get; init; } = new();

    [JsonPropertyName("publishedAt")]
    public DateTimeOffset PublishedAt { get; init; } = DateTimeOffset.UtcNow;
}
