using System.Text.Json.Serialization;

namespace DomainAI.Shared.Contracts.Messages;

public sealed record WorkflowCompletedNotification
{
    [JsonPropertyName("notificationId")]
    public Guid NotificationId { get; init; } = Guid.NewGuid();

    [JsonPropertyName("workflowId")]
    public Guid WorkflowId { get; init; }

    [JsonPropertyName("correlationId")]
    public string CorrelationId { get; init; } = string.Empty;

    [JsonPropertyName("success")]
    public bool Success { get; init; }

    [JsonPropertyName("totalSteps")]
    public int TotalSteps { get; init; }

    [JsonPropertyName("completedAt")]
    public DateTimeOffset CompletedAt { get; init; } = DateTimeOffset.UtcNow;
}
