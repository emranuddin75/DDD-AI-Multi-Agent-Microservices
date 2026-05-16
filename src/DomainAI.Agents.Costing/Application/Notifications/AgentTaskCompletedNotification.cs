using MediatR;

namespace DomainAI.Agents.Costing.Application.Notifications;

/// <summary>
/// MediatR Notification published when the Costing agent completes its task.
/// </summary>
public record AgentTaskCompletedNotification(
    string AgentId,
    string AgentName,
    string Topic,
    bool Success) : INotification;
