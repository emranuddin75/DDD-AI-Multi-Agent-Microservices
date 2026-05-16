using DomainAI.Agents.Costing.Domain;
using DomainAI.Agents.Costing.Application.Notifications;
using DomainAI.Shared.Contracts;
using MediatR;
using System.Text.Json;

namespace DomainAI.Agents.Costing.Application.Commands;

public class ExecuteAgentCommandHandler : IRequestHandler<ExecuteAgentCommand, AgentResponse>
{
    private readonly ICostingService _costingService;
    private readonly IMediator _mediator;

    public ExecuteAgentCommandHandler(ICostingService costingService, IMediator mediator)
    {
        _costingService = costingService;
        _mediator = mediator;
    }

    public async Task<AgentResponse> Handle(ExecuteAgentCommand command, CancellationToken cancellationToken)
    {
        var request = command.Request;
        var topic = request.Payload.TryGetProperty("topic", out var t) ? t.GetString() ?? "" : "";
        var industry = request.Payload.TryGetProperty("industry", out var i) ? i.GetString() ?? "" : "";

        var complianceResult = request.PreviousResults
            .FirstOrDefault(r => r.Domain == "Compliance");
        var hasHighCompliance = complianceResult?.Summary?.Contains("High") == true ||
                                complianceResult?.Summary?.Contains("Critical") == true;

        var estimate = await _costingService.EstimateCostsAsync(topic, industry, hasHighCompliance, cancellationToken);

        var breakdown = estimate.GetBreakdownByCategory();

        var resultPayload = new
        {
            topic,
            currency = estimate.Currency,
            subtotal = estimate.Subtotal,
            contingency = estimate.Contingency,
            totalCost = estimate.TotalCost,
            contingencyRatePercent = 15,
            complianceEscalated = hasHighCompliance,
            breakdown = breakdown.ToDictionary(
                kvp => kvp.Key.ToString(),
                kvp => kvp.Value),
            lineItems = estimate.Items.Select(item => new
            {
                category = item.Category.ToString(),
                description = item.Description,
                unitCost = item.UnitCost,
                quantity = item.Quantity,
                unit = item.Unit,
                totalCost = item.TotalCost
            }).ToList(),
            estimatedAt = DateTimeOffset.UtcNow
        };

        var summary = $"Cost estimate for '{topic}': " +
                      $"Subtotal {estimate.Currency} {estimate.Subtotal:N0}, " +
                      $"Contingency {estimate.Currency} {estimate.Contingency:N0} (15%), " +
                      $"Total {estimate.Currency} {estimate.TotalCost:N0}. " +
                      (hasHighCompliance ? "Compliance costs escalated due to identified regulatory risks." : "");

        var response = new AgentResponse
        {
            Success = true,
            Result = JsonSerializer.SerializeToElement(resultPayload),
            Summary = summary
        };

        await _mediator.Publish(
            new AgentTaskCompletedNotification("costing-agent", "Costing Agent", topic, true),
            cancellationToken);

        return response;
    }
}
