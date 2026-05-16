using MediatR;

namespace DomainAI.Agents.Compliance.Application.Notifications;

/// <summary>
/// MediatR Notification published when the Compliance agent completes its task.
/// Allows side-effects (logging, auditing, downstream events) without coupling.
/// </summary>
public record AgentTaskCompletedNotification(
    string AgentId,
    string AgentName,
    string Topic,
    bool Success) : INotification;
