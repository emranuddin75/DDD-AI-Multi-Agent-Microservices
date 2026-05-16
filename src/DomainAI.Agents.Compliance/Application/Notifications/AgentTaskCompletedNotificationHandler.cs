using MediatR;
using Microsoft.Extensions.Logging;

namespace DomainAI.Agents.Compliance.Application.Notifications;

public class AgentTaskCompletedNotificationHandler : INotificationHandler<AgentTaskCompletedNotification>
{
    private readonly ILogger<AgentTaskCompletedNotificationHandler> _logger;

    public AgentTaskCompletedNotificationHandler(ILogger<AgentTaskCompletedNotificationHandler> logger)
    {
        _logger = logger;
    }

    public Task Handle(AgentTaskCompletedNotification notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "NOTIFICATION: [{AgentName}] completed task for topic '{Topic}'. Success={Success}",
            notification.AgentName, notification.Topic, notification.Success);
        return Task.CompletedTask;
    }
}
