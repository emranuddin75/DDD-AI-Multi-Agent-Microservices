using System.Text.Json;
using DomainAI.Agents.Compliance.Domain;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

namespace DomainAI.Agents.Compliance.Infrastructure;

/// <summary>
/// Infrastructure implementation of the compliance service using an LLM.
/// </summary>
public sealed class LlmComplianceService : IComplianceService
{
    private readonly IChatClient _chatClient;
    private readonly ILogger<LlmComplianceService> _logger;

    public LlmComplianceService(IChatClient chatClient, ILogger<LlmComplianceService> logger)
    {
        _chatClient = chatClient;
        _logger = logger;
    }

    public async Task<ComplianceAssessment> AssessComplianceAsync(string topic, string region, CancellationToken ct = default)
    {
        _logger.LogInformation("Assessing compliance for {Topic} in {Region} using LLM", topic, region);

        var prompt = $"""
            Act as a regulatory compliance expert. Assess the compliance risks for '{topic}' operating in the '{region}' region.
            Return a JSON object with:
            - Risks (array of objects with Category (string), Description (string), Level (string: "Low", "Medium", "High", or "Critical"), Regulation (string), and Mitigation (string))
            Only return valid JSON. No explanation outside the JSON block.
            """;

        var messages = new List<ChatMessage> { new ChatMessage(ChatRole.User, prompt) };
        var response = await _chatClient.GetResponseAsync(messages, cancellationToken: ct);
        var responseText = response.Text ?? "{}";

        var assessment = ComplianceAssessment.Create(topic, region);

        try
        {
            var parsed = JsonSerializer.Deserialize<ComplianceAssessmentResponse>(responseText,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (parsed?.Risks != null)
            {
                foreach (var r in parsed.Risks)
                {
                    if (Enum.TryParse<RiskLevel>(r.Level, ignoreCase: true, out var level))
                    {
                        assessment.AddRisk(new ComplianceRisk
                        {
                            Category = r.Category,
                            Description = r.Description,
                            Level = level,
                            Regulation = r.Regulation,
                            Mitigation = r.Mitigation
                        });
                    }
                }
            }
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Failed to parse LLM response as structured compliance data");
        }

        return assessment;
    }

    private record ComplianceAssessmentResponse(List<RiskDto> Risks);

    private record RiskDto(
        string Category,
        string Description,
        string Level,
        string Regulation,
        string Mitigation);
}
