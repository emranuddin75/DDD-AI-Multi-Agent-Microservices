using DomainAI.Agents.Costing.Application;
using DomainAI.Agents.Costing.Application.Queries;
using DomainAI.Agents.Costing.Domain;
using DomainAI.Agents.Costing.Infrastructure;
using DomainAI.Agents.Costing.Api.Consumers;
using DomainAI.Shared.Contracts;
using MassTransit;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.AI;
using Microsoft.OpenApi.Models;
using OpenAI;
using System.Reflection;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "DomainAI Costing Agent API",
        Version = "v1",
        Description =
            "Specialist AI agent bounded context responsible for cost estimation and financial modelling. " +
            "Uses gpt-4o-mini (optimised for structured output) to generate itemised cost breakdowns with " +
            "a mandatory 15% contingency applied to all estimates. " +
            "Receives ExecuteAgentCommand messages from the Orchestrator via RabbitMQ and enriches results " +
            "from the MarketTrends and Compliance agents to produce risk-adjusted financial projections. " +
            "Implements CQRS: POST endpoints trigger LLM-based estimation, GET endpoints retrieve cached results.",
        Contact = new OpenApiContact
        {
            Name = "DomainAI Platform",
            Url = new Uri("https://github.com/domainai")
        }
    });

    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
        options.IncludeXmlComments(xmlPath);
});

builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssembly(typeof(CostingAgent).Assembly);
});

// MassTransit: Register RabbitMQ transport and consumer
builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<ExecuteAgentCommandConsumer>();
    x.UsingRabbitMq((context, cfg) =>
    {
        var rabbitHost = builder.Configuration["RabbitMQ:Host"] ?? "localhost";
        cfg.Host(rabbitHost, "/", h =>
        {
            h.Username(builder.Configuration["RabbitMQ:Username"] ?? "guest");
            h.Password(builder.Configuration["RabbitMQ:Password"] ?? "guest");
        });
        cfg.ConfigureEndpoints(context);
    });
});

var llmSettings = builder.Configuration.GetSection("LlmSettings");
var modelName = llmSettings["ModelName"] ?? "gpt-4o-mini";
var apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY") ?? "placeholder-api-key";

builder.Services.AddChatClient(
    new OpenAIClient(apiKey).GetChatClient(modelName).AsIChatClient());

builder.Services.AddScoped<ICostingService, LlmCostingService>();
builder.Services.AddScoped<CostingAgent>();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "DomainAI Costing Agent API v1");
    c.RoutePrefix = "swagger";
    c.DocumentTitle = "DomainAI Costing Agent API";
});

app.UseHttpsRedirection();

app.MapPost("/execute", async ([FromBody] AgentRequest request, CostingAgent agent) =>
{
    var result = await agent.ExecuteAsync(request);
    return result.Success ? Results.Ok(result) : Results.BadRequest(result);
})
.WithName("ExecuteCostEstimation")
.WithTags("Cost Estimation")
.WithOpenApi(operation =>
{
    operation.Summary = "Execute a cost estimation task";
    operation.Description =
        "Triggers the Costing AI agent to generate a detailed, itemised cost estimate for the provided " +
        "topic and payload. The agent uses gpt-4o-mini to produce structured financial projections with " +
        "line-item breakdowns. A mandatory 15% contingency buffer is applied to all estimates. " +
        "Accepts prior MarketTrends and Compliance results in PreviousResults for risk-adjusted costing. " +
        "Returns HTTP 200 with a structured AgentResponse on success, or HTTP 400 on failure.";
    operation.RequestBody.Description =
        "Agent request containing the costing scope, parameters, and upstream market and compliance results for risk-adjusted estimation.";
    return operation;
});

app.MapGet("/status/{topic}", async (string topic, IMediator mediator) =>
{
    var result = await mediator.Send(new GetAgentStatusQuery(topic));
    return result is not null ? Results.Ok(result) : Results.NotFound();
})
.WithName("GetCostingStatus")
.WithTags("Cost Estimation")
.WithOpenApi(operation =>
{
    operation.Summary = "Get the latest cost estimate for a topic";
    operation.Description =
        "Retrieves the most recent cost estimation result for the specified topic using the CQRS query path. " +
        "Does not trigger a new LLM invocation. " +
        "Returns HTTP 200 with the cached CostEstimate domain object, or HTTP 404 if no estimation has been completed.";
    var topicParam = operation.Parameters.FirstOrDefault(p => p.Name == "topic");
    if (topicParam is not null)
    {
        topicParam.Description = "The topic or project area to retrieve cost estimation for (e.g. 'cloud-migration', 'product-launch').";
        topicParam.Required = true;
    }
    return operation;
});

app.Run();
