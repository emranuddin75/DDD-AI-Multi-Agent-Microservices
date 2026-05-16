using MediatR;

namespace DomainAI.Orchestrator.Application.Notifications;

/// <summary>
/// MediatR Notification published when the full multi-agent workflow completes.
/// Allows side-effects (audit trail, metrics, alerting) without tight coupling.
/// </summary>
public record WorkflowCompletedNotification(
    Guid WorkflowId,
    string Topic,
    bool Success,
    long ExecutionTimeMs) : INotification;
