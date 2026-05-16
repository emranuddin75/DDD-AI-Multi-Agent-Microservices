using MediatR;

namespace DomainAI.Agents.Reporting.Application.Notifications;

/// <summary>
/// MediatR Notification published when the Reporting agent completes its task.
/// </summary>
public record AgentTaskCompletedNotification(
    string AgentId,
    string AgentName,
    string Topic,
    bool Success) : INotification;
