using DomainAI.Agents.Reporting.Domain;
using DomainAI.Agents.Reporting.Application.Notifications;
using DomainAI.Shared.Contracts;
using MediatR;
using System.Text.Json;

namespace DomainAI.Agents.Reporting.Application.Commands;

public class ExecuteAgentCommandHandler : IRequestHandler<ExecuteAgentCommand, AgentResponse>
{
    private readonly IReportingService _reportingService;
    private readonly IMediator _mediator;

    public ExecuteAgentCommandHandler(IReportingService reportingService, IMediator mediator)
    {
        _reportingService = reportingService;
        _mediator = mediator;
    }

    public async Task<AgentResponse> Handle(ExecuteAgentCommand command, CancellationToken cancellationToken)
    {
        var request = command.Request;
        var topic = request.Payload.TryGetProperty("topic", out var t) ? t.GetString() ?? "Unknown Topic" : "Unknown Topic";
        var industry = request.Payload.TryGetProperty("industry", out var i) ? i.GetString() ?? "" : "";
        var region = request.Payload.TryGetProperty("region", out var r) ? r.GetString() ?? "" : "";

        var report = await _reportingService.GenerateReportAsync(topic, industry, region, request.PreviousResults, cancellationToken);

        var renderedReport = report.Render();

        var resultPayload = new
        {
            title = report.Title,
            executiveSummary = report.ExecutiveSummary,
            sectionCount = report.Sections.Count,
            generatedAt = report.GeneratedAt,
            fullReport = renderedReport
        };

        var response = new AgentResponse
        {
            Success = true,
            Result = JsonSerializer.SerializeToElement(resultPayload),
            Summary = renderedReport
        };

        await _mediator.Publish(
            new AgentTaskCompletedNotification("reporting-agent", "Report Writer Agent", topic, true),
            cancellationToken);

        return response;
    }
}
