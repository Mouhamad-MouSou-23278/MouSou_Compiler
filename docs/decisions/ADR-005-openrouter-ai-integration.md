# ADR-005: OpenRouter AI Integration & Autonomous Self-Healing Bugfix Loop

## Context
Developers using online compilers frequently encounter runtime exceptions, syntax errors, and algorithmic logic bugs. Integrating AI to explain errors, optimize code, and automatically patch broken programs adds immense value. However, hard-coding a proprietary LLM provider (e.g. OpenAI only or Anthropic only) risks vendor lock-in, high costs, and requires paid credit cards for local developers.

## Decision
We adopted **OpenRouter** as the unified AI gateway with autonomous self-healing capabilities:

1. **Unified Gateway with Free Tier Support**:
   - OpenRouter standardizes access to hundreds of LLMs through an OpenAI-compatible REST API.
   - The default configuration uses high-performance **free models** (such as `meta-llama/llama-3.2-3b-instruct:free` or `google/gemini-2.0-flash-exp:free`), allowing the platform to run out-of-the-box without paid API keys.
   - The model can be changed instantly in `.env` (`OPENROUTER_MODEL=anthropic/claude-3-haiku` or `openai/gpt-4o-mini`).

2. **Structured Code Patching & Diff Engine**:
   - Prompt templates instruct the LLM to output clean, unified code blocks.
   - A diff generator parses the output and produces a structured unified diff (`@@ -1,x +1,y @@`), which is recorded in the `AIJob` entity and presented visually in the UI.

3. **Autonomous "Auto-Fix & Rerun" Loop**:
   - When a program execution fails with non-zero exit code or stderr output, the user (or worker with `AutoFixOnError=true`) can trigger `IAIFacadeService.AutoFixAsync`.
   - The system queries the AI model with the error stack trace, applies the patch to the file in PostgreSQL, records the audit trail, and immediately re-triggers sandboxed execution to verify whether the fix succeeded.

4. **Graceful Heuristic Fallback**:
   - If `OPENROUTER_API_KEY` is not provided, the service falls back to a deterministic diagnostic heuristic that explains the error patterns without throwing exceptions.

## Consequences
### Positive
- Zero cost barrier for developers testing the platform.
- Zero vendor lock-in; switch from open-source models to Claude/GPT-4o by changing a single configuration key.
- Eliminates manual copy-pasting of error messages into external chat interfaces.
