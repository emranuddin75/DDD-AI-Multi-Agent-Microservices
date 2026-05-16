using DomainAI.Agents.Compliance.Application;
using DomainAI.Agents.Compliance.Application.Queries;
using DomainAI.Agents.Compliance.Domain;
using DomainAI.Agents.Compliance.Infrastructure;
using DomainAI.Agents.Compliance.Api.Consumers;
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
        Title = "DomainAI Compliance Agent API",
        Version = "v1",
        Description =
            "Specialist AI agent bounded context responsible for regulatory compliance assessment. " +
            "Uses gpt-4-turbo to validate business findings against regulatory frameworks such as GDPR, " +
            "SOX, and industry-specific standards, producing structured risk scores and violation flags. " +
            "Receives ExecuteAgentCommand messages from the Orchestrator via RabbitMQ and enriches " +
            "results from the MarketTrends agent with compliance risk context. " +
            "Implements CQRS: POST endpoints trigger LLM-based assessment, GET endpoints retrieve cached results.",
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
    cfg.RegisterServicesFromAssembly(typeof(ComplianceAgent).Assembly);
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
var modelName = llmSettings["ModelName"] ?? "gpt-4-turbo";
var apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY") ?? "placeholder-api-key";

builder.Services.AddChatClient(
    new OpenAIClient(apiKey).GetChatClient(modelName).AsIChatClient());

builder.Services.AddScoped<IComplianceService, LlmComplianceService>();
builder.Services.AddScoped<ComplianceAgent>();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "DomainAI Compliance Agent API v1");
    c.RoutePrefix = "swagger";
    c.DocumentTitle = "DomainAI Compliance Agent API";
});

app.UseHttpsRedirection();

app.MapPost("/execute", async ([FromBody] AgentRequest request, ComplianceAgent agent) =>
{
    var result = await agent.ExecuteAsync(request);
    return result.Success ? Results.Ok(result) : Results.BadRequest(result);
})
.WithName("ExecuteComplianceAssessment")
.WithTags("Compliance Assessment")
.WithOpenApi(operation =>
{
    operation.Summary = "Execute a regulatory compliance assessment";
    operation.Description =
        "Triggers the Compliance AI agent to assess the provided topic and payload against applicable " +
        "regulatory frameworks. The agent uses gpt-4-turbo to identify compliance risks, flag violations, " +
        "and produce structured risk scores. Accepts prior MarketTrends results in PreviousResults for " +
        "context-aware compliance chaining within a workflow. " +
        "Returns HTTP 200 with an AgentResponse containing the compliance assessment, or HTTP 400 on failure.";
    operation.RequestBody.Description =
        "Agent request containing the compliance topic, regulatory context payload, and upstream market analysis results.";
    return operation;
});

app.MapGet("/status/{topic}", async (string topic, IMediator mediator) =>
{
    var result = await mediator.Send(new GetAgentStatusQuery(topic));
    return result is not null ? Results.Ok(result) : Results.NotFound();
})
.WithName("GetComplianceStatus")
.WithTags("Compliance Assessment")
.WithOpenApi(operation =>
{
    operation.Summary = "Get the latest compliance assessment for a topic";
    operation.Description =
        "Retrieves the most recent compliance assessment result for the specified topic using the CQRS query path. " +
        "Does not trigger a new LLM invocation. " +
        "Returns HTTP 200 with the cached ComplianceAssessment domain object, or HTTP 404 if no assessment has been completed.";
    var topicParam = operation.Parameters.FirstOrDefault(p => p.Name == "topic");
    if (topicParam is not null)
    {
        topicParam.Description = "The topic or business area to retrieve compliance assessment for (e.g. 'data-processing', 'financial-reporting').";
        topicParam.Required = true;
    }
    return operation;
});

app.Run();
