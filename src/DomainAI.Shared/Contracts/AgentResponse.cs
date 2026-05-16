using System.Text.Json;
using System.Text.Json.Serialization;

namespace DomainAI.Shared.Contracts;

/// <summary>
/// Structured JSON-based message returned FROM an agent.
/// Carries the bounded context's output back to the orchestrator.
/// </summary>
public sealed record AgentResponse : DomainAI.Shared.Domain.IMessage
{
    [JsonPropertyName("messageId")]
    public Guid MessageId { get; init; } = Guid.NewGuid();

    [JsonPropertyName("messageType")]
    public string MessageType { get; init; } = "AgentResponse";

    [JsonPropertyName("timestamp")]
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;

    [JsonPropertyName("correlationId")]
    public string CorrelationId { get; init; } = string.Empty;

    [JsonPropertyName("sourceAgentId")]
    public string SourceAgentId { get; init; } = string.Empty;

    [JsonPropertyName("sourceAgentName")]
    public string SourceAgentName { get; init; } = string.Empty;

    [JsonPropertyName("domain")]
    public string Domain { get; init; } = string.Empty;

    [JsonPropertyName("success")]
    public bool Success { get; init; }

    [JsonPropertyName("result")]
    public JsonElement Result { get; init; }

    [JsonPropertyName("summary")]
    public string Summary { get; init; } = string.Empty;

    [JsonPropertyName("errorMessage")]
    public string? ErrorMessage { get; init; }

    [JsonPropertyName("executionTimeMs")]
    public long ExecutionTimeMs { get; init; }
}
