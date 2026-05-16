using DomainAI.Orchestrator.Application;
using DomainAI.Orchestrator.Application.Commands;
using DomainAI.Orchestrator.Application.Queries;
using DomainAI.Orchestrator.Consumers;
using DomainAI.Orchestrator.Domain;
using DomainAI.Orchestrator.Infrastructure;
using DomainAI.Shared.Contracts;
using MassTransit;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi.Models;
using StackExchange.Redis;
using System.Reflection;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "DomainAI Orchestrator API",
        Version = "v1",
        Description =
            "The central command hub of the DomainAI multi-agent system. " +
            "Implements the Magentic (Manager/Planner) orchestration pattern to decompose " +
            "high-level business queries into specialist agent tasks dispatched via RabbitMQ. " +
            "Manages workflow lifecycle state in Redis and aggregates results from the " +
            "MarketTrends, Compliance, Costing, and Reporting bounded contexts. " +
            "POST commands initiate asynchronous AI workflows; GET queries return real-time " +
            "status from persistent Redis state.",
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

// DDD: Register MediatR for the Orchestrator bounded context
builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssembly(typeof(WorkflowOrchestrator).Assembly);
});

// MassTransit: Register RabbitMQ transport and AgentTaskCompletedConsumer
builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<AgentTaskCompletedConsumer>();
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

// Redis: Register connection for workflow state persistence
var redisConnection = builder.Configuration["Redis:Connection"] ?? "localhost:6379";
builder.Services.AddSingleton<IConnectionMultiplexer>(_ =>
    ConnectionMultiplexer.Connect(redisConnection));
builder.Services.AddSingleton<IWorkflowStateStore, RedisWorkflowStateStore>();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "DomainAI Orchestrator API v1");
    c.RoutePrefix = "swagger";
    c.DocumentTitle = "DomainAI Orchestrator API";
});

app.UseHttpsRedirection();

// POST /api/workflow — Start a new workflow
app.MapPost("/api/workflow", async ([FromBody] WorkflowRequest request, IMediator mediator) =>
{
    var result = await mediator.Send(new StartWorkflowCommand(request));
    return result.Success ? Results.Accepted($"/api/workflow/{result.WorkflowId}/status", result) : Results.BadRequest(result);
})
.WithName("StartWorkflow")
.WithTags("Workflow")
.WithOpenApi(operation =>
{
    operation.Summary = "Start a new AI multi-agent workflow";
    operation.Description =
        "Initiates a new multi-agent workflow by decomposing the submitted topic into specialist tasks. " +
        "The orchestrator dispatches ExecuteAgentCommand messages to the MarketTrends, Compliance, Costing, " +
        "and Reporting agents via RabbitMQ. The workflow runs asynchronously; use the returned workflowId " +
        "to poll GET /api/workflow/{id}/status for completion. Returns HTTP 202 Accepted with a Location header.";
    operation.RequestBody.Description = "Workflow parameters including topic, industry, region, and optional metadata.";
    return operation;
});

// GET /api/workflow/{id}/status — Get workflow status from Redis
app.MapGet("/api/workflow/{id}/status", async (Guid id, IMediator mediator) =>
{
    var plan = await mediator.Send(new GetWorkflowStatusQuery(id));
    return plan is not null ? Results.Ok(plan) : Results.NotFound(new { message = $"Workflow {id} not found" });
})
.WithName("GetWorkflowStatus")
.WithTags("Workflow")
.WithOpenApi(operation =>
{
    operation.Summary = "Get the current status of a workflow";
    operation.Description =
        "Retrieves the current execution state of a workflow from Redis persistent store. " +
        "Returns the aggregated execution plan including per-agent results, overall progress, " +
        "and completion status. Returns HTTP 404 if the workflow ID has not been registered or has expired from Redis.";
    var idParam = operation.Parameters.FirstOrDefault(p => p.Name == "id");
    if (idParam is not null)
    {
        idParam.Description = "The unique GUID identifier of the workflow, returned by the POST /api/workflow endpoint.";
        idParam.Required = true;
    }
    return operation;
});

app.Run();
