# VIGOR – Digital Shift Handover System

**VIGOR** is a digital shift handover system designed to support continuity, transparency and secure information sharing in residential care environments.

The system replaces paper-based and Excel-based overlap sheets with a structured digital solution that provides a clear overview of:

- Citizen status
- Medication administration
- Assigned employees
- Tasks and notes
- Shift-specific responsibilities

> VIGOR is developed as part of a 3rd semester Datamatiker project and focuses on clean architecture, maintainability and scalable design.

---

## Purpose

The purpose of VIGOR is to ensure safe and consistent communication between shifts in a 24/7 care environment.

The system provides:

- Structured shift selection (Day / Evening / Night)
- Snapshot-based citizen status tracking per shift
- Medication registration and traceability
- Task and note management
- Role-based access control
- Full historical traceability of shift data

---

## Architecture Overview

The solution is structured into multiple projects:

| Project | Description |
|---------|-------------|
| **VIGOR.Shared** | Shared domain models, DTOs and interfaces |
| **VIGOR.Web** | ASP.NET Core Web API (server-side logic & data access) |
| **VIGOR.Web.Client** | Blazor Web client |
| **VIGOR (MAUI)** | .NET MAUI Blazor Hybrid application |

The architecture follows layered principles with centralized Dependency Injection configuration and separation of concerns.

---

## Technology Stack

- .NET 10 (LTS)
- ASP.NET Core Web API
- Blazor
- .NET MAUI Blazor Hybrid
- Entity Framework Core (planned)
- Git / GitHub for version control

---

## Current Status

**Iteration 0 (Foundation & Setup):**

- [x] Solution structure established
- [x] Dependency Injection centralized
- [x] Blazor scaffolding configured
- [x] Clean build (0 warnings, 0 errors)
- [x] Git baseline ready

---

## Project Context

This project is developed as part of the 3rd semester Datamatiker curriculum and serves as the foundation for exams in:

- System Development
- Programming
- Technology

The focus is not only functional delivery, but also:

- Domain modeling
- Clean architecture
- Documentation
- Secure and scalable design
