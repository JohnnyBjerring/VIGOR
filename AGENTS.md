# AGENTS.md

## Scope

These instructions apply to the whole repository unless a more specific `AGENTS.md` exists in a subdirectory.

## Project Summary

VIGOR is a .NET-based system with a MAUI Blazor Hybrid client, an ASP.NET Core Web API backend, and a shared project for common models, DTOs, and interfaces. The system handles citizens in an organizational context where access is controlled by the authenticated user's role and department.

Main projects:

- `VIGOR/VIGOR.MAUI`: primary MAUI Blazor Hybrid frontend.
- `VIGOR/VIGOR.Web`: ASP.NET Core Web API backend with business logic, data access, authentication, and authorization.
- `VIGOR/VIGOR.Shared`: shared domain models, DTOs, interfaces, and shared client services.
- `VIGOR/VIGOR.UnitTests`: unit tests.
- `VIGOR/VIGOR.Web.Client`: reserved for future kiosk UI.

Use `Documentation/` for longer project documentation, diagrams, and supporting material. Keep this file focused on rules agents must follow.

## Architecture Rules

- Follow KISS, GRASP, SOLID, DRY, Single Source of Truth, Dependency Injection, MVC where relevant, and clear separation of concerns.
- Make changes minimal, targeted, and tied to the current issue or use case.
- Do not introduce unnecessary complexity, duplicate flows, broad refactorings, or new features unless explicitly requested.
- Respect the existing solution structure and naming conventions.
- Use Dependency Injection for services. Do not manually construct services with `new` when DI should own them.
- Use shared models and contracts from `VIGOR.Shared` where they already exist.

## Authentication And Authorization

- Authentication is based on ASP.NET Identity plus JWT.
- Login goes through the API endpoint that validates credentials through Identity.
- On successful login, the API returns a JWT to the MAUI client.
- The JWT must contain the Identity user id, email, and role.
- The backend depends on the Identity user id claim to link the authenticated user to an `Employee` and a `Department`.
- The MAUI client stores the JWT in SecureStorage using the existing `jwt_token` key.
- MAUI API calls must go through the DI-configured `HttpClient` and `MauiAuthHttpHandler`.
- `MauiAuthHttpHandler` is responsible for reading the JWT from local storage and adding the `Authorization: Bearer <token>` header.
- Do not create alternate API-call flows that bypass this authenticated `HttpClient`.
- Backend JWT validation is configured centrally and must use issuer, audience, and secret from configuration.
- Protected endpoints must use the JWT Bearer authentication scheme and role-based authorization where required.

## UC02 Citizen Access Rule

- UC02 is about showing citizens for the authenticated user's department.
- The user must only see citizens from their own department.
- This security rule must be enforced only on the server.
- The client may send a department id, but the backend must ignore it for authorization decisions.
- The backend must resolve the safe department from `IdentityUserId -> Employee -> DepartmentId`.
- If the authenticated user has no valid employee or department relation, deny access to citizen data.
- Do not weaken this rule to make client-side flows easier.

## Current Priority

The highest priority is stabilizing the existing MAUI-to-API authentication flow so UC02 works without `401 Unauthorized` after login. Do not add unrelated features while this remains unresolved.

## Debugging Strategy For 401 Unauthorized

Debug systematically and avoid guessing:

- First verify whether the request reaches the controller.
- Then verify whether the `Authorization` header is actually sent.
- Then verify whether the token exists in SecureStorage and is read by `MauiAuthHttpHandler`.
- Then verify whether the token is structurally valid and accepted by backend JWT validation.
- Then verify which claims are available at runtime, especially `ClaimTypes.NameIdentifier` and role claims.
- Only change code after the likely cause is identified from evidence.

## Development Workflow

- Build after each meaningful code change.
- Run unit tests after each meaningful code change.
- Keep changes small enough to review easily.
- Update the visible version in `VIGOR/VIGOR.Shared/Components/Layout/MainLayout.razor` for each project update. The current repo format is `Version 0.2.1.X alpha`; normally increment only the final build number.
- Use short, precise commit messages that include the version number when committing.
- Push to GitHub when the user has asked for Git delivery or when the agreed workflow requires it.

Useful commands:

```powershell
dotnet restore VIGOR.slnx
dotnet build VIGOR.slnx
dotnet test VIGOR.slnx
dotnet run --project VIGOR/VIGOR.Web/VIGOR.Web.csproj --launch-profile http
dotnet run --project VIGOR/VIGOR.MAUI/VIGOR.MAUI.csproj -f net10.0-windows10.0.19041.0
```

## Output Expectations

- Report only changed files unless broader context is necessary.
- Describe changes in a short diff-like summary.
- Include a brief explanation of expected runtime behavior.
- If key repository information is missing, say `Not found in repo` instead of inventing details.
