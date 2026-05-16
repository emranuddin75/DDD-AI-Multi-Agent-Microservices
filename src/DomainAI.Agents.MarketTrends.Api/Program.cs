using DomainAI.Agents.MarketTrends.Application;
using DomainAI.Agents.MarketTrends.Application.Queries;
using DomainAI.Agents.MarketTrends.Domain;
using DomainAI.Agents.MarketTrends.Infrastructure;
using DomainAI.Agents.MarketTrends.Api.Consumers;
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
        Title = "DomainAI Market Trends Agent API",
        Version = "v1",
        Description =
            "Specialist AI agent bounded context responsible for market trend analysis. " +
            "Uses gpt-4o to extract signals, sentiment, and competitive intelligence from unstructured data. " +
            "Receives ExecuteAgentCommand messages from the Orchestrator via RabbitMQ and publishes " +
            "AgentTaskCompletedEvent on completion. Implements CQRS: POST endpoints trigger LLM-based " +
            "analysis, GET endpoints retrieve cached results via MediatR query handlers.",
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

// DDD: Register MediatR for the MarketTrends bounded context
builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssembly(typeof(MarketTrendsAgent).Assembly);
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

// LLM Optimization: gpt-4o for high-reasoning market trend analysis
var llmSettings = builder.Configuration.GetSection("LlmSettings");
var modelName = llmSettings["ModelName"] ?? "gpt-4o";
var apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY") ?? "placeholder-api-key";

builder.Services.AddChatClient(
    new OpenAIClient(apiKey).GetChatClient(modelName).AsIChatClient());

builder.Services.AddScoped<IMarketAnalysisService, LlmMarketAnalysisService>();
builder.Services.AddScoped<MarketTrendsAgent>();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "DomainAI Market Trends Agent API v1");
    c.RoutePrefix = "swagger";
    c.DocumentTitle = "DomainAI Market Trends Agent API";
});

app.UseHttpsRedirection();

app.MapPost("/execute", async ([FromBody] AgentRequest request, MarketTrendsAgent agent) =>
{
    var result = await agent.ExecuteAsync(request);
    return result.Success ? Results.Ok(result) : Results.BadRequest(result);
})
.WithName("ExecuteMarketAnalysis")
.WithTags("Market Analysis")
.WithOpenApi(operation =>
{
    operation.Summary = "Execute a market trend analysis task";
    operation.Description =
        "Triggers the MarketTrends AI agent to perform deep market analysis on the provided topic and payload. " +
        "The agent uses gpt-4o to extract market signals, competitive sentiment, and emerging trend indicators. " +
        "Accepts previous agent results in PreviousResults for context-aware analysis chaining within a workflow. " +
        "Returns HTTP 200 with a structured AgentResponse on success, or HTTP 400 with error details on failure.";
    operation.RequestBody.Description =
        "Agent request containing the analysis topic, intent, payload data, and optional results from upstream agents.";
    return operation;
});

app.MapGet("/status/{topic}", async (string topic, IMediator mediator) =>
{
    var result = await mediator.Send(new GetMarketAnalysisQuery(topic));
    return result is not null ? Results.Ok(result) : Results.NotFound();
})
.WithName("GetMarketAnalysisStatus")
.WithTags("Market Analysis")
.WithOpenApi(operation =>
{
    operation.Summary = "Get the latest market analysis result for a topic";
    operation.Description =
        "Retrieves the most recent market analysis result for the specified topic using the CQRS query path. " +
        "Does not trigger a new LLM invocation. " +
        "Returns HTTP 200 with the cached TrendAnalysis domain object, or HTTP 404 if no analysis has been completed.";
    var topicParam = operation.Parameters.FirstOrDefault(p => p.Name == "topic");
    if (topicParam is not null)
    {
        topicParam.Description = "The market topic or sector to retrieve analysis for (e.g. 'renewable-energy', 'fintech').";
        topicParam.Required = true;
    }
    return operation;
});

app.Run();
