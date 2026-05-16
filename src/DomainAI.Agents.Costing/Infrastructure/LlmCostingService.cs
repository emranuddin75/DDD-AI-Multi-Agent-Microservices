using System.Text.Json;
using DomainAI.Agents.Costing.Domain;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

namespace DomainAI.Agents.Costing.Infrastructure;

/// <summary>
/// Infrastructure implementation of the costing service using an LLM.
/// </summary>
public sealed class LlmCostingService : ICostingService
{
    private readonly IChatClient _chatClient;
    private readonly ILogger<LlmCostingService> _logger;

    public LlmCostingService(IChatClient chatClient, ILogger<LlmCostingService> logger)
    {
        _chatClient = chatClient;
        _logger = logger;
    }

    public async Task<CostEstimate> EstimateCostsAsync(string topic, string industry, bool hasHighComplianceRisk, CancellationToken ct = default)
    {
        _logger.LogInformation("Estimating costs for {Topic} in {Industry} (highCompliance={HighRisk}) using LLM",
            topic, industry, hasHighComplianceRisk);

        var complianceContext = hasHighComplianceRisk
            ? "Note: elevated compliance risk has been identified — include a higher compliance cost line item."
            : "Standard compliance costs apply.";

        var prompt = $"""
            Act as a financial analyst specialising in technology projects. Estimate the costs for implementing '{topic}' in the '{industry}' industry.
            {complianceContext}
            Return a JSON object with:
            - Items (array of objects with Category (string: "Infrastructure", "Labour", "Licensing", "Compliance", or "Other"), Description (string), UnitCost (number), Quantity (integer), Unit (string))
            Only return valid JSON. No explanation outside the JSON block.
            """;

        var messages = new List<ChatMessage> { new ChatMessage(ChatRole.User, prompt) };
        var response = await _chatClient.GetResponseAsync(messages, cancellationToken: ct);
        var responseText = response.Text ?? "{}";

        var estimate = CostEstimate.Create(topic);

        try
        {
            var parsed = JsonSerializer.Deserialize<CostEstimateResponse>(responseText,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (parsed?.Items != null)
            {
                foreach (var item in parsed.Items)
                {
                    if (Enum.TryParse<CostCategory>(item.Category, ignoreCase: true, out var category))
                    {
                        estimate.AddItem(new CostItem
                        {
                            Category = category,
                            Description = item.Description,
                            UnitCost = item.UnitCost,
                            Quantity = item.Quantity,
                            Unit = item.Unit
                        });
                    }
                }
            }
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Failed to parse LLM response as structured costing data");
        }

        return estimate;
    }

    private record CostEstimateResponse(List<CostItemDto> Items);

    private record CostItemDto(
        string Category,
        string Description,
        decimal UnitCost,
        int Quantity,
        string Unit);
}
