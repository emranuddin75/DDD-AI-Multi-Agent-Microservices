using System.Text.Json;
using DomainAI.Shared.Contracts;
using DomainAI.Shared.Domain;
using Xunit;

namespace DomainAI.Tests;

public class AgentRequestTests
{
    [Fact]
    public void AgentRequest_DefaultValues_AreValid()
    {
        var request = new AgentRequest();
        Assert.NotEqual(Guid.Empty, request.MessageId);
        Assert.Equal("AgentRequest", request.MessageType);
        Assert.NotEmpty(request.CorrelationId);
    }

    [Fact]
    public void AgentRequest_Payload_IsSerializable()
    {
        var payload = JsonSerializer.Serialize(new { topic = "test", value = 42 });
        using var doc = JsonDocument.Parse(payload);
        var request = new AgentRequest { Payload = doc.RootElement.Clone() };

        Assert.Equal("test", request.Payload.GetProperty("topic").GetString());
    }

    [Fact]
    public void AgentResponse_DefaultValues_AreValid()
    {
        var response = new AgentResponse();
        Assert.NotEqual(Guid.Empty, response.MessageId);
        Assert.Equal("AgentResponse", response.MessageType);
        Assert.False(response.Success);
    }

    [Fact]
    public void WorkflowRequest_DefaultValues_AreValid()
    {
        var request = new WorkflowRequest { Topic = "Test Topic" };
        Assert.NotEqual(Guid.Empty, request.WorkflowId);
        Assert.NotEmpty(request.CorrelationId);
        Assert.Equal("Test Topic", request.Topic);
    }
}
