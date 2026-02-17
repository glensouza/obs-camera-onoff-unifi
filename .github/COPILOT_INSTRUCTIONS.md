# Copilot Instructions — .NET Blazor Server

Purpose: Help GitHub Copilot produce code and suggestions tailored for a .NET Blazor Server application (target: .NET 10, C# 14).

Principles
- Follow the repository's existing conventions before introducing new patterns.
- Keep changes minimal and focused; avoid large refactors unless requested.
- Prioritize security, readability, and testability.

Project defaults
- Target framework: .NET 10; language: C# 14.
- Prefer file-scoped namespaces and modern C# features available in the target TFM.
- Assume `Nullable` context is enabled unless project shows otherwise.

Coding style and patterns
- Use dependency injection for services; register services with the correct lifetimes (`Singleton`, `Scoped`, `Transient`).
- Avoid injecting `Scoped` services into `Singleton`s.
- Name async methods with `Async` suffix and accept `CancellationToken` where long-running or I/O-bound.
- Use `ILogger<T>` for structured logging; include useful context in logs but avoid sensitive data.
- Use `IOptions<T>` / `IOptionsMonitor<T>` for configuration models; prefer records for DTO/config shapes.
- Use `HttpClientFactory` (typed clients) for external HTTP calls.
- Use `ConfigureAwait(false)` in library/helper code when appropriate.

Blazor Server specifics
- Prefer server-side SignalR-friendly patterns (avoid long-running background loops without cancellation).
- Keep component UI logic in Razor components; use partial class code-behind files (`.razor.cs`) for complex logic.
- Use `StateHasChanged()` only when needed; prefer data-binding and event callbacks.
- Use scoped services to hold per-user state if necessary; prefer cascading parameters or explicit state containers for UI state.
- Minimize JS interop; when necessary, keep JS calls small and isolated behind typed service wrappers.

Security and secrets
- Never store secrets in code or commit them. Use `IConfiguration` from environment variables or secret stores.
- Validate and sanitize any input that reaches the server.
- For authentication/authorization, prefer built-in ASP.NET Core Identity or external providers (OIDC) and use policy-based authorization.

Performance and reliability
- Avoid blocking calls (no `.Result`/`.Wait()`); use async end-to-end.
- Stream large payloads instead of buffering them when possible.
- Use cancellation tokens for timeouts and cooperative cancelation.

Testing
- Add unit tests for services with xUnit (or existing test framework). Mock external dependencies; do not mock code under test.
- Use bUnit for component/unit tests of Razor components.
- Keep tests deterministic and independent of environment; avoid network/disk I/O in unit tests.

Documentation and PRs
- Keep diffs small and add clear PR descriptions explaining intent and risk.
- Add or update a short README or QUICKSTART when behavior or setup changes.

Boundaries
- Do not modify auto-generated files or 3rd-party libraries.
- Do not add new top-level frameworks (e.g., switch to Blazor WASM) without explicit request.

If unsure
- Prefer conservative changes and add a short comment explaining the choice.
- When a design decision is unclear, add a brief note in code or the PR so reviewers can confirm.

End of instructions.