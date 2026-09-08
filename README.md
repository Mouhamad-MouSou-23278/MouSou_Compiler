# MouSou Compiler V2 🚀
### Production-Grade Cloud IDE, Multi-Language Code Execution Sandbox & AI Copilot

[![.NET 8](https://img.shields.io/badge/.NET-8.0-512BD4?logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/)
[![React 18](https://img.shields.io/badge/React-18.3-61DAFB?logo=react&logoColor=black)](https://react.dev/)
[![TypeScript](https://img.shields.io/badge/TypeScript-5.4-3178C6?logo=typescript&logoColor=white)](https://www.typescriptlang.org/)
[![Docker](https://img.shields.io/badge/Docker-Sandboxing-2496ED?logo=docker&logoColor=white)](https://www.docker.com/)
[![PostgreSQL](https://img.shields.io/badge/PostgreSQL-16-4169E1?logo=postgresql&logoColor=white)](https://www.postgresql.org/)
[![Redis](https://img.shields.io/badge/Redis-7-DC382D?logo=redis&logoColor=white)](https://redis.io/)
[![OpenRouter](https://img.shields.io/badge/OpenRouter-AI%20Copilot-6366F1)](https://openrouter.ai/)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

**MouSou Compiler V2** is a cloud development environment and secure code execution platform engineered to Microsoft and FAANG senior architect standards. It enables developers to write, compile, execute, and debug multi-file software projects in isolated, unprivileged Docker sandboxes with sub-second latency and real-time streaming output over WebSockets.

The platform includes an **AI Copilot & Code Doctor** powered by OpenRouter that performs syntax and algorithmic explanations, generates unit tests, conducts security audits, and features an **autonomous self-healing bugfix loop** that diagnoses execution failures, applies code patches, and re-executes tests automatically.

---

## 🌟 Key Features

- **Multi-File Project Management**:
  - Full CRUD workspaces with hierarchical file trees (`src/`, `utils/`, etc.).
  - Designated entry-point file resolution with visual star indicators.
  - Multi-tab file switching in Monaco Editor (the engine powering VS Code).
  - One-click batch saving across all open editor tabs.

- **Production-Grade Sandboxing Engine**:
  - Multi-language execution: **Python 3.11**, **Node.js 20**, **C# 12 (.NET 8)**, **Go 1.22**, **C++ 20**, and **Rust**.
  - **Zero-Network Isolation (`--network none`)**: Outbound socket calls fail at kernel level, eliminating SSRF and data exfiltration.
  - **Cgroups Resource Ceilings**: 256 MB RAM limit, 1.0 CPU core, `pids_limit=64` (neutralizing fork bombs).
  - **Filesystem Defense**: Read-only root filesystem, memory-backed `/tmp` (noexec, nosuid, 64MB), ephemeral workspace destruction in `finally` blocks.
  - **Execution Timeouts**: Automated watchdog termination (default 15s) with `SIGKILL` cleanup.

- **Real-Time Streaming & Interactive Stdin**:
  - High-throughput WebSocket log streaming via ASP.NET Core SignalR.
  - Interactive terminal standard input (`stdin`) routed through Redis pub/sub to active sandboxed processes.
  - Execution metrics dashboard: Duration (ms), exit codes, memory flags, and sandbox isolation status.

- **AI Copilot & Self-Healing Auto-Fix Loop**:
  - OpenRouter LLM integration with zero-cost **free model defaults** (`meta-llama/llama-3.2-3b-instruct:free`, `google/gemini-2.0-flash-exp:free`).
  - Code explanation, algorithmic optimization, test case generation, and security reviews.
  - **"Auto-Fix & Rerun"**: Failed executions feed stack traces to the AI, which generates unified code patches (`@@ -1,x +1,y @@`), applies them to the database, and immediately reruns the project to confirm the fix.

- **Enterprise Security & Authentication**:
  - Hybrid authentication: Short-lived HMAC-SHA256 JWT access tokens + long-lived **HTTP-Only, SameSite=Lax/Strict refresh cookies**.
  - BCrypt password hashing (work factor 12) with cryptographically secure token rotation.
  - RFC 7807 ProblemDetails structured exception handling middleware.
  - Sliding-window rate limiter per client IP address.
  - Security headers middleware (`nosniff`, `DENY`, HSTS, Referrer-Policy).

---

## 🏗️ Architecture & Technology Stack

```
                                  +-------------------------------------------------------+
                                  |         React 18 + TypeScript SPA (Vite + Tailwind)   |
                                  |      Monaco Editor * Interactive Terminal * SignalR   |
                                  +-------------------------------------------------------+
                                                              |
                                                              v
+--------------------------------------------------------------------------------------------------------------------+
|                                              ASP.NET Core 8 Web API                                                |
|                                                                                                                    |
|   +--------------------------+   +--------------------------+   +-----------------------+   +------------------+   |
|   |     AuthController       |   |    ProjectsController    |   |  ExecutionsController |   |   AIController   |   |
|   |  (HTTP-Only Cookie JWT)  |   |    (Multi-File CRUD)     |   |   (Asynchronous Run)  |   | (OpenRouter LLM) |   |
|   +--------------------------+   +--------------------------+   +-----------------------+   +------------------+   |
|                                                                                                                    |
|                    SignalR ExecutionHub  *  RateLimitingMiddleware  *  ExceptionHandlingMiddleware                 |
+--------------------------------------------------------------------------------------------------------------------+
                                      |                                    |
                                      v                                    v
                       +-----------------------------+      +-----------------------------+
                       |        PostgreSQL 16        |      |           Redis 7           |
                       | (Users, Projects, Exec, AI) |      | (Job Queue & Stdin Pub/Sub) |
                       +-----------------------------+      +-----------------------------+
                                                                           |
                                                                           v
                                                            +-----------------------------+
                                                            |   ExecutionWorker Service   |
                                                            +-----------------------------+
                                                                           |
                                                                           v
                                                            +-----------------------------+
                                                            |  Isolated Docker Sandboxes  |
                                                            |   (net=none, 256MB, 1 CPU)  |
                                                            +-----------------------------+
```

| Layer | Technology | Key Responsibilities |
|---|---|---|
| **Frontend** | React 18, TypeScript, Vite, Monaco Editor, Tailwind CSS, Zustand | Cloud IDE interface, syntax highlighting, WebSocket log streaming, AI drawer |
| **Gateway / Ingress** | Nginx Reverse Proxy | Static file serving, `/api` routing, `/hubs` WebSocket upgrade proxy |
| **API Server** | ASP.NET Core 8 Web API (C#) | REST API, JWT authentication, SignalR hub, rate limiting, OpenAPI 3.0 |
| **Application** | Clean Architecture (Use cases, DTOs) | Decoupled domain business logic, service facades, interface abstractions |
| **Persistence** | EF Core 8 with PostgreSQL 16 | Relational entities, foreign keys, cascade deletes, unique indexes |
| **Message Queue** | Redis 7 & Streams / In-memory Channels | Decoupled execution queuing, pub/sub stdin distribution |
| **Execution Engine**| Docker.DotNet SDK / Linux Cgroups | Ephemeral sandboxing, non-root UID 1000, network cutoff, watchdog timers |
| **AI Intelligence** | OpenRouter REST API | Model agnostic prompt engineering, structured code diffs, auto-fix loop |

---

## 🚀 Quick Start with Docker Compose

Ensure [Docker Desktop](https://www.docker.com/products/docker-desktop/) is running.

```bash
# 1. Clone the repository
git clone https://github.com/Mouhamad-MouSou-23278/MouSou_Compiler.git
cd MouSou_Compiler

# 2. Configure environment (optional: add your free OpenRouter API key)
cp .env.example .env

# 3. Spin up the entire platform (Postgres, Redis, API, Worker, Frontend)
docker compose up --build -d
```

Once running, access the platform:
- **Cloud IDE UI**: [http://localhost:3000](http://localhost:3000)
- **Interactive Swagger / OpenAPI**: [http://localhost:5000/swagger](http://localhost:5000/swagger)
- **Health Probes**: [http://localhost:5000/healthz](http://localhost:5000/healthz)

### Pre-Seeded Demo Credentials
The database automatically seeds a demo developer workspace on first startup:
- **Email**: `admin@mousou.dev`
- **Password**: `Admin123!`
*(Or click **"Fill Demo Admin"** directly on the sign-in modal)*

---

## 💻 Local Development (Manual Setup)

If developing locally without Docker Compose:

### 1. Backend API & Background Worker
```bash
# Build the entire solution
dotnet build OnlineCompiler.sln

# Run unit tests
dotnet test tests/UnitTests/UnitTests.csproj

# Start the Web API (Runs with in-memory DB & resilient channel queue if Postgres/Redis are offline!)
dotnet run --project src/Web/Web.csproj

# Start the Background Worker (in a separate terminal)
dotnet run --project src/Workers/Workers.csproj
```

### 2. Frontend Cloud Studio
```bash
cd frontend
npm install
npm run dev
# Studio launches at http://localhost:3000 with hot module reloading!
```

---

## ⚙️ Environment Variables Reference

| Variable | Default Value | Description |
|---|---|---|
| `POSTGRES_DB` | `online_compiler` | PostgreSQL database name |
| `POSTGRES_USER` | `postgres` | Database superuser |
| `POSTGRES_PASSWORD` | `postgres` | Database password |
| `POSTGRES_PORT` | `5432` | Host port binding for PostgreSQL |
| `REDIS_PORT` | `6379` | Host port binding for Redis queue |
| `API_PORT` | `5000` | Web API port |
| `JWT_SECRET` | `SuperSecretEnterpriseProductionKey...` | 32+ character HMAC-SHA256 secret key |
| `OPENROUTER_API_KEY` | *(empty / free)* | OpenRouter API Key ([Get free key](https://openrouter.ai/keys)) |
| `OPENROUTER_MODEL` | `meta-llama/llama-3.2-3b-instruct:free` | Default AI LLM model |
| `EXECUTION_TIMEOUT_SECONDS` | `15` | Watchdog timeout for container termination |
| `SANDBOX_MEMORY_LIMIT_MB` | `256` | Maximum RAM allocated to each sandbox container |

---

## 🧪 Testing & Verification

The test suite validates authentication invariants, password hashing security, project lifecycle events, and asynchronous queue dispatching:

```bash
dotnet test tests/UnitTests/UnitTests.csproj --logger "console;verbosity=detailed"
```

**Results**:
- `AuthServiceTests.RegisterAsync_WithNewEmail_ShouldReturnTokensAndUser` -> **PASSED**
- `AuthServiceTests.RegisterAsync_WithDuplicateEmail_ShouldThrowInvalidOperationException` -> **PASSED**
- `PasswordHasherTests.HashPassword_ShouldReturnValidBcryptHash` -> **PASSED**
- `PasswordHasherTests.VerifyPassword_WithCorrectPassword_ShouldReturnTrue` -> **PASSED**
- `ProjectServiceTests.CreateProjectAsync_ShouldCreateProjectWithDefaultStarterFile` -> **PASSED**
- `ProjectServiceTests.DeleteProjectAsync_ShouldArchiveProject` -> **PASSED**
- `ExecutionServiceTests.EnqueueExecutionAsync_ShouldCreateExecutionAndPushToQueue` -> **PASSED**

---

## 📚 Architecture Decision Records (ADRs)

Detailed decision drivers and trade-off analyses are documented in `docs/decisions/`:
- [ADR-001: Modular Monolith vs Microservices](docs/decisions/ADR-001-modular-monolith.md)
- [ADR-002: Docker Sandboxing Isolation & Defense-in-Depth](docs/decisions/ADR-002-docker-sandboxing-security.md)
- [ADR-003: Hybrid JWT + HTTP-Only Cookie Authentication](docs/decisions/ADR-003-jwt-httponly-cookie-authentication.md)
- [ADR-004: Asynchronous Execution Pipeline via Redis](docs/decisions/ADR-004-redis-streams-execution-pipeline.md)
- [ADR-005: OpenRouter AI Integration & Auto-Fix Loop](docs/decisions/ADR-005-openrouter-ai-integration.md)
- [Security Sandbox Specification](docs/security/sandbox-design.md)
- [OpenAPI 3.0 Contract](docs/api/openapi.yaml)

---

## 📄 License

This project is licensed under the terms of the [MIT License](LICENSE).  
Copyright (c) 2026 **Mouhamad MouSou**.
