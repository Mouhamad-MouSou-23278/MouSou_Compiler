# ADR-002: Docker Sandboxing Isolation & Defense-in-Depth Execution Model

## Context
Executing arbitrary user-submitted code is inherently hazardous. Untrusted code can attempt:
- Fork bombs (`:(){ :|:& };:`) to exhaust kernel PID tables.
- Cryptominers or CPU hog loops to starve host resources.
- Memory allocation spikes to trigger kernel Out-Of-Memory (OOM) killer on system services.
- Network calls to exfiltrate private credentials or scan internal subnets (e.g. AWS metadata `169.254.169.254` or internal DBs).
- File tampering and privilege escalation to compromise host operating systems.

## Decision
We engineered a containerized sandboxing engine via `Docker.DotNet` enforcing five layers of isolation:

1. **Network Disconnection (`--network none`)**:
   - The container has no network interfaces beyond `lo`. Outbound socket calls fail immediately with `EPERM` / Network Unreachable.
2. **Strict Cgroups Resource Constraints**:
   - `Memory = 256MB`: Memory is capped strictly at 256MB. `MemorySwap` is equal to `Memory` to prohibit disk swap thrashing.
   - `NanoCPUs = 1,000,000,000` (1 Core): Containers cannot consume more than a single CPU core.
   - `PidsLimit = 64`: Thwarts fork bombs by preventing the creation of more than 64 processes/threads.
3. **Storage & Filesystem Isolation**:
   - Source code is written into a dedicated temporary directory (`/tmp/compiler_sandbox/{executionId}`) and mounted into `/workspace`.
   - `/tmp` is mounted as `Tmpfs` with `noexec,nosuid,size=64m`.
   - The host temporary directory is aggressively purged in a `finally` block post-execution.
4. **Non-Root Execution**:
   - Containers run as unprivileged user IDs (UID 1000).
5. **Enforced Timeouts**:
   - A `CancellationTokenSource` linked to an execution timer (default 15 seconds) forcefully issues a `docker kill` if the process hangs or enters an infinite loop.

## Consequences
### Positive
- Production-grade security preventing host escape, network exfiltration, or resource exhaustion.
- Reproducible multi-language runtime environments using official minimal Alpine images (`python:3.11-alpine`, `node:20-alpine`, `golang:1.22-alpine`, etc.).

### Negative
- Initial cold start image pull latency (mitigated by `EnsureImagePulledAsync` pre-warming during deployment).
