using DomainAI.Agents.Reporting.Application;
using DomainAI.Agents.Reporting.Application.Queries;
using DomainAI.Agents.Reporting.Domain;
using DomainAI.Agents.Reporting.Infrastructure;
using DomainAI.Agents.Reporting.Api.Consumers;
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
        Title = "DomainAI Reporting Agent API",
        Version = "v1",
        Description =
            "Specialist AI agent bounded context responsible for synthesising all agent outputs into " +
            "an executive-grade business report. Uses gpt-4o to aggregate findings from the MarketTrends, " +
            "Compliance, and Costing agents and produce a cohesive structured narrative with recommendations. " +
            "This is the terminal stage in the multi-agent workflow pipeline. Receives ExecuteAgentCommand " +
            "messages from the Orchestrator via RabbitMQ and publishes AgentTaskCompletedEvent to signal " +
            "workflow completion. Implements CQRS: POST endpoints trigger LLM-based report generation, " +
            "GET endpoints retrieve cached reports.",
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
    cfg.RegisterServicesFromAssembly(typeof(ReportingAgent).Assembly);
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
var modelName = llmSettings["ModelName"] ?? "gpt-4o";
var apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY") ?? "placeholder-api-key";

builder.Services.AddChatClient(
    new OpenAIClient(apiKey).GetChatClient(modelName).AsIChatClient());

builder.Services.AddScoped<IReportingService, LlmReportingService>();
builder.Services.AddScoped<ReportingAgent>();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "DomainAI Reporting Agent API v1");
    c.RoutePrefix = "swagger";
    c.DocumentTitle = "DomainAI Reporting Agent API";
});

app.UseHttpsRedirection();

app.MapPost("/execute", async ([FromBody] AgentRequest request, ReportingAgent agent) =>
{
    var result = await agent.ExecuteAsync(request);
    return result.Success ? Results.Ok(result) : Results.BadRequest(result);
})
.WithName("ExecuteReportGeneration")
.WithTags("Report Generation")
.WithOpenApi(operation =>
{
    operation.Summary = "Execute an executive report generation task";
    operation.Description =
        "Triggers the Reporting AI agent to synthesise all upstream agent results into a structured executive report. " +
        "The agent uses gpt-4o to combine MarketTrends, Compliance, and Costing findings into a coherent " +
        "business narrative with an executive summary and strategic recommendations. " +
        "Pass all prior agent results in PreviousResults for a comprehensive final report. " +
        "Returns HTTP 200 with a structured AgentResponse on success, or HTTP 400 on failure.";
    operation.RequestBody.Description =
        "Agent request containing the reporting scope and the full set of upstream agent results (market, compliance, costing) to synthesise into an executive report.";
    return operation;
});

app.MapGet("/status/{topic}", async (string topic, IMediator mediator) =>
{
    var result = await mediator.Send(new GetAgentStatusQuery(topic));
    return result is not null ? Results.Ok(result) : Results.NotFound();
})
.WithName("GetReportingStatus")
.WithTags("Report Generation")
.WithOpenApi(operation =>
{
    operation.Summary = "Get the latest generated report for a topic";
    operation.Description =
        "Retrieves the most recently generated executive report for the specified topic using the CQRS query path. " +
        "Does not trigger a new LLM invocation. " +
        "Returns HTTP 200 with the cached report data, or HTTP 404 if no report has been generated for the topic.";
    var topicParam = operation.Parameters.FirstOrDefault(p => p.Name == "topic");
    if (topicParam is not null)
    {
        topicParam.Description = "The topic or business area to retrieve the generated report for (e.g. 'market-entry-analysis', 'quarterly-review').";
        topicParam.Required = true;
    }
    return operation;
});

app.Run();
