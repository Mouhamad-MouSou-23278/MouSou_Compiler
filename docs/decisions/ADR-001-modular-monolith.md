# ADR-001: Adoption of Modular Monolith Architecture with Clean Architecture Boundaries

## Context
When designing an online code execution platform that supports user management, file hierarchies, sandboxed execution workers, and AI assistants, teams frequently face the architectural choice between Microservices and a Modular Monolith.

Premature microservice decomposition introduces:
- Substantial network latency between services
- Complex distributed transaction coordination (Sagas / 2PC)
- High deployment and local development friction
- Increased infrastructure overhead (service discovery, distributed tracing, API gateways)

## Decision
We adopted a **Modular Monolith** pattern organized around **Clean Architecture** (Domain, Application, Infrastructure, Web, Workers). 

1. Modules are logically partitioned into separate assemblies (`.csproj`) with strict compile-time dependency enforcement:
   - `Domain` has zero dependencies.
   - `Application` depends exclusively on `Domain`.
   - `Infrastructure` implements abstractions defined in `Application`.
   - `Web` and `Workers` serve as the runtime hosting entry points.
2. The asynchronous execution engine runs as an independent `BackgroundService` (`Workers`) communicating via an `IExecutionQueue` interface. In production, this can be scaled independently across multiple worker nodes or run inside the same container cluster.

## Consequences
### Positive
- Single unified solution (`.sln`) with straightforward local development (`docker compose up` or `dotnet run`).
- Compile-time type safety across all layer interactions.
- Zero network hop latency between domain operations and database contexts.
- Simple transition to independent microservices in the future should traffic patterns demand horizontal scaling of specific sub-domains.

### Negative
- All modules share the same release cycle unless deployed as separate container artifacts.
