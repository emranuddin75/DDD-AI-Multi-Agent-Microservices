using System.Text.Json;
using System.Text.Json.Serialization;

namespace DomainAI.Shared.Contracts;

/// <summary>
/// Structured JSON-based message sent TO an agent.
/// Represents the shared kernel contract crossing bounded context boundaries.
/// </summary>
public sealed record AgentRequest : DomainAI.Shared.Domain.IMessage
{
    [JsonPropertyName("messageId")]
    public Guid MessageId { get; init; } = Guid.NewGuid();

    [JsonPropertyName("messageType")]
    public string MessageType { get; init; } = "AgentRequest";

    [JsonPropertyName("timestamp")]
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;

    [JsonPropertyName("correlationId")]
    public string CorrelationId { get; init; } = Guid.NewGuid().ToString();

    [JsonPropertyName("targetAgentId")]
    public string TargetAgentId { get; init; } = string.Empty;

    [JsonPropertyName("intent")]
    public string Intent { get; init; } = string.Empty;

    [JsonPropertyName("payload")]
    public JsonElement Payload { get; init; }

    [JsonPropertyName("metadata")]
    public Dictionary<string, string> Metadata { get; init; } = new();

    [JsonPropertyName("previousResults")]
    public List<AgentResponse> PreviousResults { get; init; } = new();
}
