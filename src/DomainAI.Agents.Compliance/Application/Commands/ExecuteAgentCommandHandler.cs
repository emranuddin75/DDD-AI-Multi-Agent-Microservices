using DomainAI.Agents.Compliance.Domain;
using DomainAI.Agents.Compliance.Application.Notifications;
using DomainAI.Shared.Contracts;
using MediatR;
using System.Text.Json;

namespace DomainAI.Agents.Compliance.Application.Commands;

public class ExecuteAgentCommandHandler : IRequestHandler<ExecuteAgentCommand, AgentResponse>
{
    private readonly IComplianceService _complianceService;
    private readonly IMediator _mediator;

    public ExecuteAgentCommandHandler(IComplianceService complianceService, IMediator mediator)
    {
        _complianceService = complianceService;
        _mediator = mediator;
    }

    public async Task<AgentResponse> Handle(ExecuteAgentCommand command, CancellationToken cancellationToken)
    {
        var request = command.Request;
        var topic = request.Payload.TryGetProperty("topic", out var t) ? t.GetString() ?? "" : "";
        var region = request.Payload.TryGetProperty("region", out var r) ? r.GetString() ?? "" : "";

        var assessment = await _complianceService.AssessComplianceAsync(topic, region, cancellationToken);

        var highRisks = assessment.GetRisksAbove(RiskLevel.High).ToList();

        var resultPayload = new
        {
            topic,
            region,
            overallRiskLevel = assessment.OverallRiskLevel.ToString(),
            isCompliant = assessment.IsCompliant,
            riskCount = assessment.Risks.Count,
            risks = assessment.Risks.Select(risk => new
            {
                category = risk.Category,
                description = risk.Description,
                level = risk.Level.ToString(),
                regulation = risk.Regulation,
                mitigation = risk.Mitigation
            }).ToList(),
            assessedAt = DateTimeOffset.UtcNow
        };

        var summary = $"Compliance assessment for '{topic}' in {region}: " +
                      $"Overall risk level is {assessment.OverallRiskLevel}. " +
                      $"{assessment.Risks.Count} risks identified" +
                      (highRisks.Count > 0 ? $", including {highRisks.Count} high/critical items requiring immediate attention." : ".");

        var response = new AgentResponse
        {
            Success = true,
            Result = JsonSerializer.SerializeToElement(resultPayload),
            Summary = summary
        };

        await _mediator.Publish(
            new AgentTaskCompletedNotification("compliance-agent", "Compliance Agent", topic, true),
            cancellationToken);

        return response;
    }
}
