# Secure Sandbox Execution Architecture & Defense-in-Depth Specification

## 1. Threat Modeling & Attack Vectors

The sandbox execution engine is architected under the zero-trust assumption that **all submitted code is potentially malicious**. The threat model identifies five primary threat categories:

| Threat ID | Attack Vector | Potential Impact | Mitigation in Online Compiler V2 |
|---|---|---|---|
| **TH-01** | **Network Exfiltration / SSRF** | Data theft, scanning internal AWS metadata (`169.254.169.254`), or probing PostgreSQL/Redis. | `--network none` disconnects all bridge and host interfaces. Kernel rejects all outbound sockets. |
| **TH-02** | **Fork Bomb / Process Exhaustion** | System freeze via `:(){ :|:& };:` exhausting host PID table. | `PidsLimit = 64` prevents creation of more than 64 processes/threads. |
| **TH-03** | **Memory Exhaustion (OOM DOS)** | Allocating gigabytes of RAM to force host OS out-of-memory killer. | `Memory = 256MB` and `MemorySwap = 256MB` enforce hard ceiling without swap thrashing. |
| **TH-04** | **CPU Starvation / Mining** | Infinite loops (`while True: pass`) consuming 100% of host cores. | `NanoCPUs = 1,000,000,000` (1 Core max) + hard timeout termination (default 15s). |
| **TH-05** | **Filesystem Tampering / Persistence** | Overwriting container binaries, planting backdoors, or escaping root. | Ephemeral isolated bind mounts (`/workspace`), read-only rootfs, non-root user (UID 1000), `/tmp` on tmpfs (`noexec,nosuid,size=64m`). |

---

## 2. Kernel & Container Isolation Controls

### 2.1 Cgroups v2 Resource Allocations
Every execution container is provisioned with deterministic cgroups limits:
```json
{
  "Memory": 268435456,
  "MemorySwap": 268435456,
  "NanoCPUs": 1000000000,
  "PidsLimit": 64,
  "CpuPeriod": 100000,
  "CpuQuota": 100000
}
```

### 2.2 Network Namespace Isolation
```json
{
  "NetworkDisabled": true,
  "HostConfig": {
    "NetworkMode": "none"
  }
}
```
Within the container, `ip link show` displays only the loopback interface (`lo`). DNS resolution, HTTP/HTTPS requests, raw socket creation, and ICMP pings fail instantaneously with `Operation not permitted`.

### 2.3 Filesystem Boundary Architecture
```
[Host System]
  /tmp/compiler_sandbox/{executionId}/
       |-- main.py
       |-- helper.py
       `-- input.txt
            |
      (Read-Write Volume Bind)
            v
[Container Sandbox]
  /workspace/ (Working Directory)
  /tmp/ (Tmpfs mount: size=64m, flags: rw, noexec, nosuid)
  / (Rootfs: Minimal Alpine Linux, unprivileged)
```

1. **Working Directory (`/workspace`)**:
   Contains only the user's project files. Files are cleaned up from the host in a `finally` block immediately upon container exit.
2. **Ephemeral Scratch Space (`/tmp`)**:
   Mounted as a memory-backed tmpfs capped at 64MB with `noexec` to prevent execution of downloaded binaries.
3. **Container Destruction**:
   Containers are created with `--rm` and explicitly removed via `dockerClient.Containers.RemoveContainerAsync(id, Force=true)` to guarantee zero zombie container leaks.

---

## 3. Execution Timeout & Watchdog Enforcers

Every execution job is monitored by a dual-watchdog timer:
1. **Application Watchdog**:
   A `CancellationTokenSource` initialized with `TimeSpan.FromSeconds(15)`. If the timeout fires, a cancellation request is broadcast, immediately sending a `SIGKILL` to the container via `dockerClient.Containers.StopContainerAsync`.
2. **SignalR Health Status**:
   If the container times out, the execution status is recorded as `Timeout`, an exit code of `124` is logged, and a system notice is pushed to the client terminal:
   ```
   [System] Execution timeout exceeded (15s) - Sandbox forcefully terminated.
   ```
