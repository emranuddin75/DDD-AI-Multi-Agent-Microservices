# DomainAI — Resilient, Message-Driven AI Multi-Agent System

A .NET 8 solution demonstrating how **Domain-Driven Design (DDD)** principles can be applied to structure an **AI multi-agent microservice system** with a **resilient, asynchronous architecture**.

The system utilizes the **Magentic (Manager/Planner) Orchestration Pattern** to solve complex, open-ended business problems through a coordinated team of specialist agents. By integrating **CQRS**, **MassTransit (RabbitMQ)**, and **Redis**, we've built a self-healing platform that eliminates single points of failure.

---

## Key Features

- **DDD Bounded Contexts**: 4 independent specialist agents (Market, Compliance, Costing, Reporting).
- **Asynchronous Messaging**: Decoupled communication via RabbitMQ + MassTransit.
- **Persistent Workflow State**: Workflows survive restarts using Redis distributed state.
- **Unified CQRS**: Commands, Queries, and Notifications implemented across all services via MediatR.
- **LLM Specialization**: Each agent uses a model optimized for its domain (gpt-4o, gpt-4-turbo, gpt-4o-mini).
- **Docker Ready**: Full `docker-compose` support for the entire microservice fleet.

---

## Architecture Overview

### Strategic Design: Bounded Context Map
Intelligence is strategically distributed across four independent bubbles. Each agent operates within its own **Bounded Context**, ensuring the domain rules of "Costing" never interfere with "Compliance."

![DDD Bounded Context Map](https://static.prod-images.emergentagent.com/jobs/e085d1fb-6cc0-465a-a6dc-69a3d9ada55c/images/44a4eee0fc5bb98b2b8da0c3130cfe45f28030a06e09813bb58adeee9a41eae4.png)

### Tactical Implementation: CQRS & Resilience
Inside each microservice, the **CQRS Pattern** separates high-latency AI reasoning (Writes) from real-time status updates (Queries). The Orchestrator manages the workflow state in **Redis**, while **RabbitMQ** ensures no command is ever lost.

![Enterprise AI Agent Ecosystem](https://static.prod-images.emergentagent.com/jobs/e085d1fb-6cc0-465a-a6dc-69a3d9ada55c/images/fde2205064d095efe79389cc0baf7cbf71ed38d804d33aa00cf6096f4d6e68df.png)

---

## Operational Flow (Event Storming)

The workflow is not a rigid script; it is a series of choreographed actions and reactive policies.

![Final Event Storming Big Picture](https://static.prod-images.emergentagent.com/jobs/e085d1fb-6cc0-465a-a6dc-69a3d9ada55c/images/b76225b8fcc47d7bbcd48fbcbbc5882036ddc0f9c33525eafca7cbdc0e87fe41.png)

---

## Resilience: Eliminating the Single Point of Failure (SPOF)
To achieve production-grade reliability, the Orchestrator has been refactored into a Resilient/Stateful component.

![Eliminating the Single Point of Failure (SPOF)](https://static.prod-images.emergentagent.com/jobs/e085d1fb-6cc0-465a-a6dc-69a3d9ada55c/images/f833bf285f0d4068b5ce88fd37e698f42d40c530edd7a3a5916a3997801ab681.png)

---

## Tactical Implementation: Unified CQRS & MediatR
Inside every microservice, the CQRS Pattern separates "Writing" (high-latency AI reasoning) from "Reading" (instant status updates).

![Tactical Implementation: Unified CQRS & MediatR](https://static.prod-images.emergentagent.com/jobs/e085d1fb-6cc0-465a-a6dc-69a3d9ada55c/images/9c2100959480cd29c4022db7fdab28589eea1a88db4aecc354616e3440539751.png)

---

## Specialized AI Workforce

| Bounded Context | Microservice | Model | Responsibility |
|---|---|---|---|
| **Market Trends** | `MarketTrends.Api` | `gpt-4o` | Extracts signals and sentiment from web data. |
| **Compliance** | `Compliance.Api` | `gpt-4-turbo` | Validates findings against regulatory frameworks. |
| **Costing** | `Costing.Api` | `gpt-4o-mini` | Generates estimates with a 15% contingency rule. |
| **Reporting** | `Reporting.Api` | `gpt-4o` | Synthesizes all events into an executive summary. |

---

## Getting Started

### Prerequisites
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Docker & Docker Compose](https://docs.docker.com/get-docker/)

### Quick Start (Local)
1. **Clone the repository.**
2. **Start Infrastructure**:
   ```bash
   docker-compose up -d rabbitmq redis
   ```
3. **Build the Solution**:
   ```bash
   dotnet build DomainAI.sln
   ```
4. **Run Tests**:
   ```bash
   dotnet test DomainAI.sln
   ```
   *(91 tests should pass )*

### Running the Ecosystem (Full Fleet)
Run all 5 microservices and infrastructure:
```bash
docker-compose up --build
```

---

## Documentation
For a deep dive into the design decisions, resilience patterns, and code structure, refer to the **[DESIGN.md](./DESIGN.md)** file.

**The future of software is orchestrated.**
"# DDD-AI-Multi-Agent-Microservices" 
