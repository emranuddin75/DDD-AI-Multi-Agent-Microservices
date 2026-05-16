using System.Text.Json.Serialization;

namespace DomainAI.Shared.Contracts;

/// <summary>
/// Top-level request submitted to the Orchestrator.
/// The orchestrator decomposes this into agent-specific tasks.
/// </summary>
public sealed record WorkflowRequest
{
    [JsonPropertyName("workflowId")]
    public Guid WorkflowId { get; init; } = Guid.NewGuid();

    [JsonPropertyName("correlationId")]
    public string CorrelationId { get; init; } = Guid.NewGuid().ToString();

    [JsonPropertyName("topic")]
    public string Topic { get; init; } = string.Empty;

    [JsonPropertyName("industry")]
    public string Industry { get; init; } = string.Empty;

    [JsonPropertyName("region")]
    public string Region { get; init; } = string.Empty;

    [JsonPropertyName("parameters")]
    public Dictionary<string, string> Parameters { get; init; } = new();

    [JsonPropertyName("requestedAt")]
    public DateTimeOffset RequestedAt { get; init; } = DateTimeOffset.UtcNow;
}
