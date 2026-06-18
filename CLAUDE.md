# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Commands

Run all commands from the repo root.

```bash
# Build the whole solution
dotnet build MyApp.sln

# Run the API (Swagger at https://localhost:5001/swagger in Development)
dotnet run --project src/MyApp.Api

# Run all tests
dotnet test

# Run a single test project
dotnet test tests/MyApp.Application.Tests

# Run a single test by name (xUnit fully-qualified name or substring)
dotnet test --filter "FullyQualifiedName~EmployeeServicesTests"
dotnet test --filter "DisplayName~Deactivate"

# EF Core migrations (Infrastructure holds the DbContext, Api is the startup project)
dotnet ef migrations add <Name> --project src/MyApp.Infrastructure --startup-project src/MyApp.Api
dotnet ef database update      --project src/MyApp.Infrastructure --startup-project src/MyApp.Api
```

Target framework is **net8.0** with `Nullable` and `ImplicitUsings` enabled across all projects. Tests use **xUnit + Moq + FluentAssertions**. Default DB is SQL Server LocalDB; the `Default` connection string lives in `src/MyApp.Api/appsettings.json`. The DB is seeded on startup with an admin user (`admin@myapp.com` / `Admin@123!`) via `IdentitySeeder`.

## Architecture

Clean Architecture with strict dependency direction `Api → Application → Domain` and `Infrastructure → Application/Domain`. The Domain has no dependencies; Application defines interfaces that Infrastructure implements.

- **MyApp.Domain** — rich domain model. Entities (`Order`, `Customer`, `Employee`) have private setters and enforce invariants in constructors/methods, throwing `DomainException`. Aggregate roots raise domain events via `RaiseDomainEvent`. Repository *interfaces* live here (`IOrderRepository`, etc.).
- **MyApp.Application** — use-case services, DTOs, validators, and the `Result`/`Error` machinery. Organized by feature folder (`Orders/`, `Customers/`, `Employees/`, `Auth/`) plus `Common/`.
- **MyApp.Infrastructure** — EF Core (`AppDbContext`, entity configs), ASP.NET Identity (`AppUser`, JWT via `JwtTokenService`), `UnitOfWork`, repository implementations, and `SimpleMapper`. All DI wiring is in `DependencyInjection.AddInfrastructure`.
- **MyApp.Api** — controllers, `Program.cs`, JWT setup, `ExceptionMiddleware`. Application services are registered in `Program.cs`; infrastructure services in `AddInfrastructure`.

### The Result → HTTP convention (most important pattern)

Services never throw for expected failures and never return HTTP types. They return `Result` (commands) or `Result<T>` (queries), and **the suffix of `Error.Code` decides the HTTP status**. `ApiControllerBase.HandleResult` does the translation:

| `Error.Code` suffix | HTTP status            | Success case            |
|---------------------|------------------------|-------------------------|
| `.NotFound`         | 404                    | `Result<T>` → 200 OK    |
| `.Conflict`         | 409                    | `Result` (no value) → 204 No Content |
| `.Unauthorized`     | 401                    |                         |
| `.Forbidden`        | 403                    |                         |
| `.Validation`       | 422                    |                         |
| anything else       | 400                    |                         |

This means an error's code (e.g. `"Employee.NotFound"`, `"Employee.Conflict"`) is load-bearing — it must end with the right suffix to map correctly. Define errors as static factories/fields in per-feature `*Errors.cs` classes (see `EmployeeErrors`, `OrderErrors`). Controllers stay thin: build the `Result` in the service, return `HandleResult(...)`.

### Service hierarchy

```
BaseService              helpers only: Ok()/Ok<T>()/Fail()/Fail<T>() — no dependencies
  └── AuthorizedService  + ICurrentUser; identity props (IsAdmin, CurrentUserId…) and guards
        ├── OrderService
        ├── CustomerService
        └── EmployeeServices
```

A service inherits from `AuthorizedService` only if it needs identity/authorization; otherwise from `BaseService`. Guards (`EnsureRole`, `EnsureOwnerOrAdmin`, `EnsureAuthenticated`) return a ready `Result` — propagate it on failure (`if (guard.IsFailure) return guard;`).

### Two-tier authorization

Authorization is enforced in two layers, and both matter:
1. **Coarse** — `[Authorize(Roles = ...)]` on the controller/action, using compile-time composite constants from `AppRoles` (e.g. `AppRoles.AdminOrManager`, `AppRoles.All`). Never hardcode role strings in attributes.
2. **Fine** — inside the service, via guards. Three idiomatic patterns are illustrated in `EmployeeServices`: ownership refinement (`GetByIdAsync`), role + business-rule guards on a command (`DeactivateAsync`), and identity-based result *filtering* with no guard (`GetMyTeamAsync`, where identity changes *what* is returned, not *whether* it's allowed).

> Critical wiring in `Program.cs`: `RoleClaimType = AppClaimTypes.Role` and `NameClaimType = AppClaimTypes.Name` must be set on JWT validation, otherwise `[Authorize(Roles = ...)]` silently fails to match. A global `FallbackPolicy` requires authentication on every endpoint by default — use `[AllowAnonymous]` to opt out (e.g. login/register).

### Data access

`IUnitOfWork` exposes the repositories (`uow.Employees`, `uow.Orders`, `uow.Customers`) and `SaveAsync`/`CommitAsync`. Services typically inject `IUnitOfWork` rather than individual repositories, though repositories are also registered individually in DI. `SimpleMapper` is a hand-written `IMapper` — when you add a new entity↔DTO mapping you must add a `switch` arm to it (it throws `NotSupportedException` otherwise). DTOs expose `FromEntity` static factories.

### Error handling

Expected failures flow through `Result`/`Error`. Truly unexpected exceptions bubble up to `ExceptionMiddleware`, which logs them and returns a generic 500 — do not use exceptions for control flow in services.
