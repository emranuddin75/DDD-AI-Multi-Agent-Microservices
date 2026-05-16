using MediatR;
using Microsoft.Extensions.Logging;

namespace DomainAI.Orchestrator.Application.Notifications;

public class WorkflowCompletedNotificationHandler : INotificationHandler<WorkflowCompletedNotification>
{
    private readonly ILogger<WorkflowCompletedNotificationHandler> _logger;

    public WorkflowCompletedNotificationHandler(ILogger<WorkflowCompletedNotificationHandler> logger)
    {
        _logger = logger;
    }

    public Task Handle(WorkflowCompletedNotification notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "NOTIFICATION: Workflow {WorkflowId} for topic '{Topic}' completed. Success={Success}, Duration={ElapsedMs}ms",
            notification.WorkflowId, notification.Topic, notification.Success, notification.ExecutionTimeMs);
        return Task.CompletedTask;
    }
}
