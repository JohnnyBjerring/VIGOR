# Copilot Instructions

## General Guidelines
- Follow GRASP, SOLID, KISS, DRY, Dependency Injection, and MVC principles.
- Implement only minimal changes per iteration/checklist.
- Keep naming consistent and EF Core-friendly.
- Avoid adding features outside the current use case (UC02).

## Project-Specific Rules
- Use shared models located in `VIGOR.Shared.Models`.
- Maintain separation of shared models and interfaces.
- Adhere to iterative Scrum+XP methodologies.
- Version the app using 0.Major.Minor.Buildversion format. Automatically bump the Buildversion for every update and update it in `MainLayout.razor` (e.g. `Version 0.2.1.X alpha`).