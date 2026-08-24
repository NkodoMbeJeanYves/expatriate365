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

## Final Technical Conventions
- All source code must be written exclusively in English.
- Backend: .NET conventions (PascalCase, interfaces prefixed with I).
- Frontend: Angular conventions (kebab-case file naming).
- JSON API payloads: snake_case.
- Database: snake_case tables and columns.
- DTOs: Request/Response naming pattern.
- REST endpoints: plural resource names.
