using MediatR;

namespace DomainAI.Agents.MarketTrends.Application.Notifications;

public record MarketAnalysisCompletedNotification(string Topic, double Sentiment) : INotification;
