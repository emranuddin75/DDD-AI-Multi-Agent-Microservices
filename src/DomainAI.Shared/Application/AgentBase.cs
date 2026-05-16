using System.Diagnostics;
using System.Text.Json;
using DomainAI.Shared.Contracts;
using DomainAI.Shared.Domain;
using Microsoft.Extensions.Logging;

namespace DomainAI.Shared.Application;

/// <summary>
/// Abstract base class for all agents. Implements the template-method pattern
/// over IAgent, providing cross-cutting concerns (timing, logging, error handling).
/// Each bounded context inherits from this and provides domain-specific logic.
/// </summary>
public abstract class AgentBase : IAgent
{
    private readonly ILogger _logger;

    protected AgentBase(ILogger logger)
    {
        _logger = logger;
    }

    public abstract string AgentId { get; }
    public abstract string AgentName { get; }
    public abstract string Domain { get; }

    public async Task<AgentResponse> ExecuteAsync(AgentRequest request, CancellationToken cancellationToken = default)
    {
        var sw = Stopwatch.StartNew();
        _logger.LogInformation("[{AgentName}] Starting execution. CorrelationId={CorrelationId}, Intent={Intent}",
            AgentName, request.CorrelationId, request.Intent);

        try
        {
            var result = await ExecuteCoreAsync(request, cancellationToken);
            sw.Stop();

            _logger.LogInformation("[{AgentName}] Completed in {ElapsedMs}ms. Success={Success}",
                AgentName, sw.ElapsedMilliseconds, result.Success);

            return result with
            {
                SourceAgentId = AgentId,
                SourceAgentName = AgentName,
                Domain = Domain,
                CorrelationId = request.CorrelationId,
                ExecutionTimeMs = sw.ElapsedMilliseconds
            };
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogError(ex, "[{AgentName}] Execution failed after {ElapsedMs}ms", AgentName, sw.ElapsedMilliseconds);

            return new AgentResponse
            {
                SourceAgentId = AgentId,
                SourceAgentName = AgentName,
                Domain = Domain,
                CorrelationId = request.CorrelationId,
                Success = false,
                ErrorMessage = ex.Message,
                ExecutionTimeMs = sw.ElapsedMilliseconds
            };
        }
    }

    public virtual Task<bool> CanHandleAsync(AgentRequest request, CancellationToken cancellationToken = default)
        => Task.FromResult(string.IsNullOrEmpty(request.TargetAgentId) || request.TargetAgentId == AgentId);

    protected abstract Task<AgentResponse> ExecuteCoreAsync(AgentRequest request, CancellationToken cancellationToken);

    protected static JsonElement SerializeToElement<T>(T value)
    {
        var json = JsonSerializer.Serialize(value);
        return JsonDocument.Parse(json).RootElement.Clone();
    }
}
