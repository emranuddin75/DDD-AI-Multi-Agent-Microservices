namespace DomainAI.Shared.Domain;

/// <summary>
/// Marker interface for all inter-agent messages (domain events / commands).
/// Enforces the structured JSON-based contract between bounded contexts.
/// </summary>
public interface IMessage
{
    Guid MessageId { get; }
    string MessageType { get; }
    DateTimeOffset Timestamp { get; }
    string CorrelationId { get; }
}
