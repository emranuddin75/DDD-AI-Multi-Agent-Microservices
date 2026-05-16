using DomainAI.Shared.Contracts;
using MediatR;

namespace DomainAI.Agents.MarketTrends.Application.Commands;

/// <summary>
/// CQRS Command: Represents the intent to perform a market analysis.
/// </summary>
public record ExecuteMarketAnalysisCommand(AgentRequest Request) : IRequest<AgentResponse>;
