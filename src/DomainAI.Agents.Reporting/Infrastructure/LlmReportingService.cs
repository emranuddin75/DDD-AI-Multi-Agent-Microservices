using System.Text.Json;
using DomainAI.Agents.Reporting.Domain;
using DomainAI.Shared.Contracts;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

namespace DomainAI.Agents.Reporting.Infrastructure;

/// <summary>
/// Infrastructure implementation of the reporting service using an LLM.
/// </summary>
public sealed class LlmReportingService : IReportingService
{
    private readonly IChatClient _chatClient;
    private readonly ILogger<LlmReportingService> _logger;

    public LlmReportingService(IChatClient chatClient, ILogger<LlmReportingService> logger)
    {
        _chatClient = chatClient;
        _logger = logger;
    }

    public async Task<Report> GenerateReportAsync(
        string topic, string industry, string region,
        IReadOnlyList<AgentResponse> previousResults,
        CancellationToken ct = default)
    {
        _logger.LogInformation("Generating report for {Topic} in {Industry} ({Region}) using LLM", topic, industry, region);

        var agentSummaries = previousResults
            .Where(r => r.Success)
            .Select(r => $"- {r.SourceAgentName} ({r.Domain}): {r.Summary}")
            .ToList();

        var context = agentSummaries.Count > 0
            ? string.Join("\n", agentSummaries)
            : "No prior agent results available.";

        var prompt = $"""
            Act as a senior business analyst and report writer. Synthesise the following specialist agent findings into a concise executive-level report.

            Topic: {topic}
            Industry: {industry}
            Region: {region}

            Agent findings:
            {context}

            Return a JSON object with:
            - ExecutiveSummary (string: 2-4 sentence overview)
            - Sections (array of objects with Title (string), Content (string), and SourceAgent (string))
            - Recommendations (array of strings, 3-5 strategic recommendations)
            Only return valid JSON. No explanation outside the JSON block.
            """;

        var messages = new List<ChatMessage> { new ChatMessage(ChatRole.User, prompt) };
        var response = await _chatClient.GetResponseAsync(messages, cancellationToken: ct);
        var responseText = response.Text ?? "{}";

        var report = Report.Create($"AI Strategy Report: {topic}");

        try
        {
            var parsed = JsonSerializer.Deserialize<ReportResponse>(responseText,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (parsed != null)
            {
                report.SetExecutiveSummary(parsed.ExecutiveSummary ?? context);

                int order = 1;
                if (parsed.Sections != null)
                {
                    foreach (var section in parsed.Sections)
                    {
                        report.AddSection(new ReportSection
                        {
                            Order = order++,
                            Title = section.Title,
                            Content = section.Content,
                            SourceAgent = section.SourceAgent
                        });
                    }
                }

                if (parsed.Recommendations != null && parsed.Recommendations.Count > 0)
                {
                    var recommendationsContent = string.Join("\n\n",
                        parsed.Recommendations.Select((rec, idx) => $"{idx + 1}. {rec}"));

                    report.AddSection(new ReportSection
                    {
                        Order = order,
                        Title = "Strategic Recommendations",
                        Content = recommendationsContent,
                        SourceAgent = "Report Writer Agent (LLM)"
                    });
                }
            }
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Failed to parse LLM response as structured report data");
            report.SetExecutiveSummary(context);
        }

        return report;
    }

    private record ReportResponse(
        string? ExecutiveSummary,
        List<SectionDto>? Sections,
        List<string>? Recommendations);

    private record SectionDto(string Title, string Content, string SourceAgent);
}
