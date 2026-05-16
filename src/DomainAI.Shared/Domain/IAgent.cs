namespace DomainAI.Shared.Domain;

/// <summary>
/// Core domain interface for all AI agents in the system.
/// Each bounded context implements this contract.
/// </summary>
public interface IAgent
{
    string AgentId { get; }
    string AgentName { get; }
    string Domain { get; }
    Task<AgentResponse> ExecuteAsync(AgentRequest request, CancellationToken cancellationToken = default);
    Task<bool> CanHandleAsync(AgentRequest request, CancellationToken cancellationToken = default);
}
