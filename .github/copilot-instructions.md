# Copilot Instructions

## Purpose

Help GitHub Copilot and contributors produce code and suggestions tailored for this Blazor Server application (target: .NET 10, C# 14) while following the repository's existing conventions.

## Project Overview

Blazor Server app that controls UniFi PoE switch ports to power cameras on/off. Uses Azure Table Storage (Azurite for local dev) and a UniFi controller REST API. Deployed via Docker Compose.

## Tech Stack

- Framework: ASP.NET Core Blazor Server (Interactive Server rendering)
- Target: .NET 10 (`net10.0`)
- Storage: Azure Table Storage via `Azure.Data.Tables` (Azurite for local development)
- Containerization: Docker + Docker Compose
- CI/CD: GitHub Actions with a self-hosted runner

Apply repository conventions first

- Follow existing code patterns in the repo before introducing new patterns.
- Keep changes minimal and focused; avoid large refactors unless requested.
- Prioritize security, readability, and testability.

## C# Style & Conventions (preferred)

- Use file-scoped namespaces (`namespace X;`).
- Use explicit types instead of `var` for local variables when clarity is preferred (e.g., `HttpResponseMessage response = ...`).
- Use `this.` prefix for instance members.
- Use `[]` for initializing empty collections where that matches the existing codebase (e.g., `List<PortStatus> statuses = [];`).
- Use target-typed `new()` when the type is clear from context (e.g., `HttpClientHandler handler = new();`).
- Prefer primary constructors for simple DI-bound service types (e.g., `public class ConfigurationService(TableServiceClient tableServiceClient, ILogger<ConfigurationService> logger)`).
- Use traditional constructors with `this.` assignment when additional initialization is required.
- Prefer plain C# classes for models in this repository (the codebase currently uses classes, not records).
- Use `string.Empty` for default string values.
- Use `readonly` for fields assigned only in constructors.
- Use anonymous types for creating JSON payloads when appropriate.

## Async & Concurrency

- Name async methods with the `Async` suffix.
- Use `SemaphoreSlim` for async-safe locks (e.g., authentication, initialization).
- Use double-check locking pattern for lazy initialization.
- Accept `CancellationToken` for long-running or I/O-bound public methods where appropriate.

## Error Handling & Logging

- Return `null` or `false` from service methods that interact with external systems on expected failures; throw only when callers must react to the failure.
- Use `try/catch` with structured logging for context.
- Catch specific Azure `RequestFailedException` with status checks when interacting with Table Storage (e.g., `when (ex.Status == 404)`).
- Use structured logging templates (e.g., `"Error getting port status for port {PortNumber}"`) — avoid string interpolation inside log messages.
- Use appropriate log levels: `LogInformation`, `LogWarning`, `LogError`.

## Blazor / Components

- Keep component UI logic in `.razor` files for small components; move complex logic to `.razor.cs` partial classes when necessary.
- Use `@inject` for DI and implement `IDisposable` to clean up timers/cancellation tokens.
- Use `InvokeAsync` + `StateHasChanged` when updating UI from background threads/timers.
- Prefer scoped services for per-user state when needed; avoid injecting scoped services into singletons.

## Services & Registration

- Register services in `Program.cs` using top-level statements.
- Use `IOptions<T>` for configuration.
- Use typed `HttpClient` via `IHttpClientFactory` for external HTTP calls.
- Avoid registering services with incorrect lifetimes (e.g., do not inject `Scoped` into `Singleton`).

## Models & Persistence

- Models in `Models/` are plain classes with auto-properties and defaults. Use `init` for entity mapping properties where appropriate.
- Internal entity types may be nested private classes inside services (e.g., `PortConfigurationEntity`).

## Security & Secrets

- Never store secrets in code. Use environment variables or secret stores.
- Validate and sanitize any input reaching the server.

## Performance & Reliability

- Avoid blocking calls (no `.Result`/`.Wait()`).
- Use cancellation tokens for timeouts and cooperative cancellation.
- Use `ConfigureAwait(false)` in library/helper code when appropriate.

## Docker & Deployment

- Use multi-stage Docker builds and Docker Compose for local dev with Azurite.
- App listens on port `8080` by default.

## Documentation & PRs

- Keep diffs small and add clear PR descriptions.
- Update README or QUICKSTART when setup or behavior changes.

## Boundaries

- Do not modify auto-generated files or third-party libraries.
- Do not add new top-level frameworks without explicit request.

If unsure

- Prefer conservative changes and add a short comment explaining choices.
- When a design decision is unclear, add a brief note in code or the PR for reviewers.
