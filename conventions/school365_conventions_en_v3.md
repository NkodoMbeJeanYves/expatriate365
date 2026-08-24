# SCHOOL365 - Additional Conventions and Decisions (EN)

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
- SQLite and MySQL are supported.
- SQLite is the default provider.
- The provider is selected through configuration files.
- Switching from SQLite to MySQL must not require business code changes.
- Domain and Application layers must remain database-agnostic.
- EF Core migrations must remain compatible with SQLite and MySQL.
- Connection settings must be externalized.
- Database-specific code must be isolated in the Infrastructure layer.
- SQLite and MySQL packages may coexist in the solution, but only the configured provider can be activated at runtime.
- The platform must be delivered with a fully functional SQLite configuration by default.

## Final Technical Conventions
- All source code must be written exclusively in English.
- Backend: .NET conventions (PascalCase, interfaces prefixed with I).
- Frontend: Angular conventions (kebab-case file naming).
- JSON API payloads: snake_case.
- Database: snake_case tables and columns.
- DTOs: Request/Response naming pattern.
- REST endpoints: plural resource names.
