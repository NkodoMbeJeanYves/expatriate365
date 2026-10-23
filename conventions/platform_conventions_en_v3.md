# Platform — Additional Conventions and Decisions v3 (EN)

## Changes extracted from the specification

### School Management
- A school can be created by cloning the configuration of another school (template/copy mode).

### Academic Day Model
- The system must validate that generated periods and breaks fit within the configured academic day range.
- When configuring the first day, the system may propose applying the same schedule configuration to other days of the week.
- Any update to the academic day model must regenerate periods.
- If existing timetables are already in use, modifications must be blocked or require an explicit reset process.

### Payments
- Supported payment methods also include: cash.

## DB-CONF Convention — Database Provider Strategy
- MySQL is the only supported provider following the product decision of 2026-10-22; SQLite is no longer part of the project.
- Production target: MySQL 8.0.46 with `utf8mb4` and `utf8mb4_unicode_ci`.
- Required SQL modes: `ONLY_FULL_GROUP_BY,STRICT_TRANS_TABLES,NO_ZERO_IN_DATE,NO_ZERO_DATE,ERROR_FOR_DIVISION_BY_ZERO,NO_ENGINE_SUBSTITUTION`.
- Domain and Application layers must remain database-agnostic.
- EF Core migrations must be tested on MySQL for fresh installations and upgrades with historical data.
- Connection settings must be externalized.
- Database-specific code must be isolated in the Infrastructure layer.
- MySQL-specific persistence code must not leak into Domain or Application.

## Final Technical Conventions
- All source code must be written exclusively in English.
- Backend: .NET conventions (PascalCase, interfaces prefixed with I).
- Frontend: Angular conventions (kebab-case file naming).
- JSON API payloads: snake_case.
- Database: snake_case tables and columns.
- DTOs: Request/Response naming pattern.
- REST endpoints: plural resource names.
