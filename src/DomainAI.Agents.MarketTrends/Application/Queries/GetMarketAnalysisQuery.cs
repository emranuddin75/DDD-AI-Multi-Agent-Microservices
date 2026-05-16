using MediatR;
using System.Text.Json;

namespace DomainAI.Agents.MarketTrends.Application.Queries;

public record GetMarketAnalysisQuery(string Topic) : IRequest<JsonElement?>;
