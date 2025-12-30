# GitHub Copilot Workspace Rules

This repository implements a Mock EMSP simulator using OCPI 2.3.

These rules apply globally to ALL Copilot-generated code.

---

## Architecture
- Modular Monolith
- ASP.NET Core (.NET 10)
- Razor Pages (server-side rendered UI)
- EF Core with SQLite
- Single executable application
- No SPA frameworks
- No background workers
- No message brokers

---

## OCPI Scope
- Role: EMSP only
- OCPI Version: 2.3
- Implement ONLY:
  - Versions module
  - Credentials module
- Handshake only
- Single CPO connection
- Dual handshake mode:
  - EMSP initiates
  - CPO initiates

---

## Explicit Exclusions (Hard Rules)
Copilot MUST NOT:
- Implement Sessions, CDRs, Tariffs, Tokens, Commands, ChargingProfiles
- Add billing logic
- Add authentication frameworks (JWT, OAuth, Identity)
- Add logging / tracing frameworks
- Add retry, circuit breaker, or resilience libraries
- Add SPA frameworks (React, Angular, Vue)
- Introduce microservices or Clean Architecture layers

---

## Persistence Rules
- SQLite only
- Tokens MUST persist across restarts
- Raw OCPI payloads MUST be stored verbatim
- Only one CPO connection is supported

---

## Error Simulation
- Deterministic only
- Supported errors:
  - HTTP 401
  - HTTP 403
- Controlled via database flags and internal UI endpoints
- No random or time-based behavior

---

## UI Rules (Razor Pages)
- Razor Pages only (no MVC Views)
- Pages are functional admin/debug tools
- No UI framework required
- Focus on observability and control
- Server-side rendering only

---

## Code Style
- Explicit over implicit
- No reflection-heavy abstractions
- No “magic” base classes
- Prefer simple services over frameworks
- Optimize for debuggability

---

## General Guardrail
If a feature does NOT directly support:
- OCPI handshake simulation
- OCPI debugging
- OCPI interoperability testing

DO NOT implement it.
