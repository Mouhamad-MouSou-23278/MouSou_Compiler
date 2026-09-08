# System Architecture Overview - Online Compiler V2

## Executive Summary

**Online Compiler V2** is an enterprise-grade cloud development environment and secure execution platform designed to Microsoft and FAANG engineering standards. The system enables developers to write, compile, run, and debug multi-file codebases in isolated, secure sandboxes with sub-second latency and real-time streaming output over WebSockets.

An integrated AI Copilot powered by OpenRouter provides autonomous code analysis, error diagnostics, and self-healing automated bug fixes.

---

## Architectural Principles & Patterns

The platform strictly adheres to **Clean Architecture** (Ports & Adapters / Onion Architecture) to decouple business domain logic from infrastructure frameworks, execution engines, and communication protocols.

```
+-------------------------------------------------------------+
|                      Presentation (Web)                     |
|         ASP.NET Core 8 Web API  *  SignalR Execution Hub    |
+-------------------------------------------------------------+
                              |
+-------------------------------------------------------------+
|                      Application Layer                      |
|       Use Cases  *  DTOs  *  Interfaces  *  Service Facades |
+-------------------------------------------------------------+
                              |
+-------------------------------------------------------------+
|                         Domain Layer                        |
|       Entities  *  Enums  *  Business Rules & Invariants    |
+-------------------------------------------------------------+
                              ^
+-------------------------------------------------------------+
|                     Infrastructure Layer                    |
|  EF Core / PostgreSQL * Redis Queue * Docker Sandbox Engine |
|            BCrypt Hasher * OpenRouter AI Client             |
+-------------------------------------------------------------+
```

### Design Patterns Utilized
1. **Facade Pattern (`AIFacadeService`)**: Simplifies the complex multi-step orchestration of reading project files, synthesizing system and user prompts, querying OpenRouter models, parsing structured JSON patches, updating the filesystem in EF Core, and triggering automated re-executions.
2. **Strategy Pattern (`IDockerSandboxService` & Language Executors)**: Encapsulates runtime arguments, compiler flags, and container images per language (Python, Node.js, C#, Go, C++, Rust), allowing transparent extensibility for new languages without altering the worker pipeline.
3. **Producer-Consumer Pattern (`IExecutionQueue` & `ExecutionWorker`)**: Decouples HTTP request ingestion from heavy container creation. The Web API enqueues job payloads into Redis, and dedicated worker processes consume and execute them asynchronously.
4. **Observer / Publish-Subscribe Pattern (`ExecutionHub` via SignalR)**: Container output streams (stdout/stderr) are broadcast chunk-by-chunk to client WebSocket groups (`exec-{id}`) without blocking the worker thread.
5. **Circuit Breaker & Fallback Resilience Pattern**:
   - `RedisExecutionQueue` includes an in-memory `System.Threading.Channels` fallback if Redis is temporarily unreachable.
   - `OpenRouterAIService` includes a local diagnostic heuristic fallback if the upstream AI provider experiences rate limiting or network downtime.
   - `DockerSandboxService` includes a safe local process executor fallback for offline development environments.

---

## Component Breakdown

### 1. Web API (`src/Web`)
- **ASP.NET Core 8**: High-throughput asynchronous HTTP API hosting RESTful endpoints and SignalR hubs.
- **Security Middleware Pipeline**:
  - `ExceptionHandlingMiddleware`: Centralized RFC 7807 ProblemDetails JSON error formatting.
  - `SecurityHeadersMiddleware`: Enforces `X-Content-Type-Options: nosniff`, `X-Frame-Options: DENY`, and strict HTTPS headers.
  - `RateLimitingMiddleware`: Sliding window rate limiter per client IP address.
- **Authentication**: Stateless short-lived JWT access tokens accompanied by cryptographically secure HTTP-Only refresh cookies with `SameSite=Lax/Strict` to prevent Cross-Site Scripting (XSS) and Cross-Site Request Forgery (CSRF).

### 2. Application Layer (`src/Application`)
- Contains all application contracts, service orchestration, DTOs, and validation logic.
- References only the Domain layer; completely independent of EF Core, Docker, or Redis details.

### 3. Domain Layer (`src/Domain`)
- Core business entities: `User`, `Project`, `ProjectFile`, `Execution`, `AIJob`.
- Zero external package dependencies.

### 4. Infrastructure Layer (`src/Infrastructure`)
- **PostgreSQL 16 via EF Core**: Fully configured entity mappings with cascade rules, unique indexes, and foreign keys.
- **Redis Streams & Lists**: Asynchronous job distribution and pub/sub for real-time interactive stdin delivery.
- **Docker.DotNet SDK**: Manages dynamic container lifecycles with strict cgroup isolation.
- **OpenRouter AI Integration**: Consumes state-of-the-art free and open-source models with JSON schema extraction.

### 5. Execution Worker (`src/Workers`)
- .NET `BackgroundService` that processes execution queues, mounts isolated read-only volumes, handles execution timeouts, records resource metrics, and manages the self-healing AI auto-fix loop.

### 6. Frontend Client (`frontend/`)
- **React 18 & TypeScript**: Single Page Application built on Vite.
- **Monaco Editor**: The same editor engine that powers VS Code, with full language syntax highlighting, keyboard shortcuts (`Ctrl+Enter` to run, `Ctrl+S` to save), and multi-tab file switching.
- **SignalR Client**: Real-time terminal log viewer with color-coded stdout/stderr output.
- **Zustand State Store**: Lightweight, reactive state management.
