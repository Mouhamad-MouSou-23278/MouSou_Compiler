# ADR-004: Asynchronous Execution Pipeline via Redis Streams and Resilient Memory Queues

## Context
Code execution runs between hundreds of milliseconds and several seconds. Spawning Docker containers directly inside synchronous ASP.NET Core HTTP request handlers would quickly exhaust Kestrel worker threads, degrade API throughput, and risk client HTTP connection dropouts.

## Decision
We engineered an asynchronous execution pipeline decoupled via an `IExecutionQueue` interface:

1. **Redis List / Stream Transport**:
   - The Web API creates an `Execution` record with status `Queued` in PostgreSQL and enqueues the `ExecutionJobPayload` to Redis (`online_compiler:executions_queue`).
   - The API immediately responds with `202 Accepted` returning the execution ID.
   - The frontend connects to `/hubs/execution` via SignalR and joins the group `exec-{id}` to receive streaming stdout/stderr frames.

2. **Dedicated Background Workers (`ExecutionWorker`)**:
   - Workers poll the queue, spin up the sandboxed container, stream logs chunk-by-chunk to SignalR, and record exit codes, outputs, and execution metrics upon termination.

3. **Interactive Stdin via Redis Pub/Sub**:
   - When a user types into the interactive terminal stdin box, the Web API publishes the input to Redis channel `online_compiler:stdin:{executionId}`. The active container process consumes it without polling.

4. **In-Memory Resilient Fallback**:
   - If Redis is unavailable or undergoing maintenance, `RedisExecutionQueue` automatically diverts payloads to an in-memory `System.Threading.Channels.Channel<ExecutionJobPayload>`, ensuring continuous zero-downtime execution in single-instance and developer environments.

## Consequences
### Positive
- Predictable API latency (< 20ms) regardless of code execution length.
- Horizontal scalability: multiple worker instances can dequeue jobs concurrently without lock contention.
- Resilient against temporary message broker dropouts.
