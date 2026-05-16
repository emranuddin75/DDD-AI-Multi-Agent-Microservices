# DomainAI — Architecture Design Document

## Overview

DomainAI is a multi-agent AI system built with Domain-Driven Design (DDD) principles,
CQRS (Command Query Responsibility Segregation) via MediatR, resilient messaging via
MassTransit (RabbitMQ), and persistent workflow state via Redis.

The system orchestrates four specialised AI agents — each a separate bounded context —
to perform end-to-end business analysis workflows: market trend analysis, regulatory
compliance assessment, cost estimation, and consolidated report generation.

---

## Architecture Layers

```
┌──────────────────────────────────────────────────────────────────┐
│                         API Layer                                │
│   Orchestrator.Api  │  MarketTrends.Api  │  Compliance.Api  │   │
│                     │  Costing.Api       │  Reporting.Api    │   │
├──────────────────────────────────────────────────────────────────┤
│                     Application Layer (CQRS)                     │
│   Commands → Handlers → Notifications → NotificationHandlers    │
│   Queries  → Handlers (Read Models)                              │
│   MediatR pipeline                                               │
├──────────────────────────────────────────────────────────────────┤
│                     Domain Layer                                 │
│   Aggregates, Value Objects, Domain Services, Interfaces         │
│   ExecutionPlan, WorkflowStep, TrendAnalysis, ComplianceRisk...  │
├──────────────────────────────────────────────────────────────────┤
│                   Infrastructure Layer                            │
│   LLM Services (OpenAI via Microsoft.Extensions.AI)              │
│   RedisWorkflowStateStore │ InMemoryWorkflowStateStore           │
│   MassTransit Consumers                                          │
├──────────────────────────────────────────────────────────────────┤
│                   Messaging Layer (MassTransit)                   │
│   RabbitMQ Transport (Production) │ InMemory (Dev/Test)          │
│   ExecuteAgentCommand → Agent Consumers                          │
│   AgentTaskCompletedEvent → Orchestrator Consumer                │
│   WorkflowCompletedNotification                                  │
├──────────────────────────────────────────────────────────────────┤
│                   State Layer (Redis)                             │
│   workflow:plan:{id} → JSON serialised ExecutionPlan             │
│   24-hour TTL, auto-cleanup on workflow completion                │
└──────────────────────────────────────────────────────────────────┘
```

---

## CQRS Pattern (MediatR)

Each bounded context (agent) follows the CQRS pattern:

### Commands (Write Side)
- **`ExecuteAgentCommand`** / **`ExecuteMarketAnalysisCommand`** — triggers domain logic
- Command Handlers invoke domain services, then publish a MediatR **Notification**
- Example: `ExecuteAgentCommandHandler` → calls `IComplianceService` → publishes `AgentTaskCompletedNotification`

### Queries (Read Side)
- **`GetAgentStatusQuery`** / **`GetMarketAnalysisQuery`** — retrieves agent status
- Query Handlers return data from a read model (mock/projection)
- Exposed via `GET /status/{topic}` on each agent API

### Notifications (Side Effects)
- **`AgentTaskCompletedNotification`** — published after each agent completes
- **`AuditNotificationHandler`** — logs the notification (audit trail)
- Decouples the write path from side effects (logging, metrics, downstream events)

### Orchestrator CQRS
- **`StartWorkflowCommand`** — creates an `ExecutionPlan`, saves to Redis, publishes first `ExecuteAgentCommand` via MassTransit
- **`GetWorkflowStatusQuery`** — loads `ExecutionPlan` from Redis
- **`WorkflowCompletedNotification`** — published when all steps finish

---

## Resilient Messaging (MassTransit + RabbitMQ)

### Message Flow

```
Client → POST /api/workflow
       → MediatR: StartWorkflowCommand
       → Handler saves ExecutionPlan to Redis
       → Handler publishes ExecuteAgentCommand to RabbitMQ
       → Agent Consumer picks up message
       → Agent executes domain logic via MediatR
       → Agent publishes AgentTaskCompletedEvent
       → Orchestrator's AgentTaskCompletedConsumer picks up
       → Updates Redis state, triggers next step or completes
```

### Message Contracts (DomainAI.Shared.Contracts.Messages)

| Message                       | Direction            | Purpose                              |
|-------------------------------|----------------------|--------------------------------------|
| `ExecuteAgentCommand`         | Orchestrator → Agent | Dispatch work to a specific agent    |
| `AgentTaskCompletedEvent`     | Agent → Orchestrator | Report completion/failure of a step  |
| `WorkflowCompletedNotification`| Orchestrator → Bus  | Signal all steps done                |

### Consumer Pattern (ExecuteAgentCommandConsumerBase)

All four agent APIs extend `ExecuteAgentCommandConsumerBase`:
- Filters messages by `TargetAgentId`
- Delegates to MediatR command handler
- Publishes `AgentTaskCompletedEvent` on completion
- Logs errors and publishes failure events

### Transport Configuration

| Context       | Transport     | Use Case                                    |
|---------------|---------------|---------------------------------------------|
| Production    | RabbitMQ      | All 5 APIs connect to shared RabbitMQ       |
| Host (Demo)   | InMemory      | Single-process demo without external deps   |
| Tests         | InMemory      | Fast unit/integration testing               |

---

## State Management (Redis)

### Workflow State Store

```csharp
public interface IWorkflowStateStore
{
    Task SavePlanAsync(ExecutionPlan plan, CancellationToken ct = default);
    Task<ExecutionPlan?> LoadPlanAsync(Guid workflowId, CancellationToken ct = default);
    Task RemovePlanAsync(Guid workflowId, CancellationToken ct = default);
}
```

- **`RedisWorkflowStateStore`** — Production implementation using StackExchange.Redis
  - Key format: `workflow:plan:{workflowId}`
  - 24-hour TTL for automatic cleanup
  - JSON serialisation via `ExecutionPlanDto`
- **`InMemoryWorkflowStateStore`** — Development/test implementation using `ConcurrentDictionary`

### State Transitions

```
Pending → InProgress → Completed / Failed
```

The `AgentTaskCompletedConsumer` drives transitions:
1. Receives `AgentTaskCompletedEvent`
2. Loads plan from Redis
3. Marks step as Completed/Failed
4. If more steps: publishes next `ExecuteAgentCommand`
5. If all done: publishes `WorkflowCompletedNotification`, removes plan from Redis

---

## API Endpoints

### Orchestrator API (DomainAI.Orchestrator.Api)

| Method | Path                           | Description                       |
|--------|--------------------------------|-----------------------------------|
| POST   | `/api/workflow`                | Start a new multi-agent workflow  |
| GET    | `/api/workflow/{id}/status`    | Get workflow execution status     |

### Agent APIs (4 × Bounded Context APIs)

| Method | Path              | Description                         |
|--------|-------------------|-------------------------------------|
| POST   | `/execute`        | Execute agent directly (HTTP)       |
| GET    | `/status/{topic}` | Query agent status via CQRS query   |

---

## Project Structure

```
DomainAI.sln
├── src/
│   ├── DomainAI.Shared/              # Shared kernel: contracts, interfaces, consumer base
│   ├── DomainAI.Orchestrator/        # Orchestrator domain: ExecutionPlan, state store, consumers
│   ├── DomainAI.Orchestrator.Api/    # Orchestrator API: workflow endpoints, MassTransit + Redis
│   ├── DomainAI.Agents.MarketTrends/ # Market analysis domain: commands, queries, notifications
│   ├── DomainAI.Agents.MarketTrends.Api/ # Market analysis API: MassTransit consumer, endpoints
│   ├── DomainAI.Agents.Compliance/   # Compliance domain
│   ├── DomainAI.Agents.Compliance.Api/
│   ├── DomainAI.Agents.Costing/      # Costing domain
│   ├── DomainAI.Agents.Costing.Api/
│   ├── DomainAI.Agents.Reporting/    # Reporting domain
│   ├── DomainAI.Agents.Reporting.Api/
│   └── DomainAI.Host/               # Console host (demo with InMemory transport)
└── tests/
    └── DomainAI.Tests/              # xUnit tests (91 tests)
```

---

## Technology Stack

| Component             | Technology                          | Version |
|-----------------------|-------------------------------------|---------|
| Runtime               | .NET 8                              | 8.0     |
| CQRS / Mediator       | MediatR                             | 14.1.0  |
| Message Bus           | MassTransit                         | 8.2.5   |
| Message Transport     | RabbitMQ (via MassTransit.RabbitMQ) | 8.2.5   |
| State Store           | Redis (StackExchange.Redis)         | 2.8.16  |
| AI/LLM Integration    | Microsoft.Extensions.AI + OpenAI    | 10.5.2  |
| API Framework         | ASP.NET Core Minimal APIs           | 8.0     |
| Testing               | xUnit + Moq                         | 2.5.3   |
