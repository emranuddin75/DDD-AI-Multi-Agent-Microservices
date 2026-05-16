using MediatR;
using Microsoft.Extensions.Logging;

namespace DomainAI.Agents.MarketTrends.Application.Notifications;

public class MarketAnalysisCompletedNotificationHandler : INotificationHandler<MarketAnalysisCompletedNotification>
{
    private readonly ILogger<MarketAnalysisCompletedNotificationHandler> _logger;

    public MarketAnalysisCompletedNotificationHandler(ILogger<MarketAnalysisCompletedNotificationHandler> logger)
    {
        _logger = logger;
    }

    public Task Handle(MarketAnalysisCompletedNotification notification, CancellationToken cancellationToken)
    {
        // Example side effect: Log, update a dashboard, or send a signal to another system
        _logger.LogInformation("NOTIFICATION: Market analysis for {Topic} finished with sentiment {Sentiment}", 
            notification.Topic, notification.Sentiment);
        return Task.CompletedTask;
    }
}
