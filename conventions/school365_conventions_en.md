# SCHOOL365 — Project Conventions and Decisions (EN)

**Project:** School365  
**Type:** School, academic and financial management platform  
**Convention version:** 1.0 consolidated  
**Date:** 2026-07-02

---

## Table of contents

1. Definitive technical conventions
2. Database and provider convention
3. Design principles
4. Naming conventions
5. Target technical architecture
6. Business rules
7. Security and RBAC
8. REST API contract
9. Development recommendations

---

## 1. Definitive technical conventions

- All source code must be written **exclusively in English**.
- Backend: .NET conventions — PascalCase, interfaces prefixed with `I`.
- Frontend: Angular conventions — kebab-case file names.
- API JSON payloads: snake_case throughout.
- Database: tables and columns in snake_case.
- DTOs: `Request` / `Response` naming pattern.
- REST endpoints: plural resources.
- API routes versioned with prefix `/api/v1`.
- All endpoints secured with JWT Bearer.
- Endpoint documentation via Swagger / OpenAPI / Scalar.
- Add all EndPoints Api Response Documentation (Sample Response).
- API JSON Response: snake_case throughout.
- Add Log using serilog, in every methods.

---

## 2. Database and provider convention (DB-CONF)

- Both SQLite and MySQL are supported.
- **SQLite is the default provider.**
- The provider is selected via the configuration file — no business code change is required to switch from SQLite to MySQL.
- The **Domain** and **Application** layers must not depend on the database engine.
- EF Core migrations must remain compatible with both SQLite and MySQL.
- Connection parameters must be externalized.
- Provider-specific code must be isolated in the **Infrastructure** layer.
- SQLite and MySQL may coexist in the solution, but only the configured provider is active at runtime.
- The project must be delivered with a working default SQLite configuration.

---

## 3. Design principles

| Code   | Title                       | Description                                                                                                     |
| ------ | --------------------------- | --------------------------------------------------------------------------------------------------------------- |
| PC-001 | MVP Simplicity              | The MVP must prioritize essential features and avoid unnecessary complexity.                                    |
| PC-002 | Modularity                  | Each domain must be isolated to allow progressive evolution.                                                    |
| PC-003 | Multi-school ready          | Even if the MVP starts with a limited number of schools, tables must include a school reference where relevant. |
| PC-004 | Auditable data              | Core entities must contain: `created_at`, `updated_at`, `created_by`, `updated_by`, `is_active`.                |
| PC-005 | Automatic period generation | Course periods must never be created manually. They are generated from the academic day template.               |
| PC-006 | SQLite-to-MySQL readiness   | Column types, constraints and names must remain compatible with a future MySQL migration.                       |

---

## 4. Naming conventions

### 4.1 Backend C#

- Classes, methods, properties: **PascalCase**
- Interfaces: prefix `I` + PascalCase

```csharp
public class Student
{
    public Guid Id { get; set; }
    public string RegistrationNumber { get; set; }
}

public interface IStudentRepository { }
```

### 4.2 Database

- Tables: snake_case, **plural**
- Columns: snake_case
- Primary keys: `id` (UUID stored as TEXT)
- Foreign keys: `<entity>_id`

```sql
-- Valid name examples
students
academic_years
school_id
created_at
```

### 4.3 API JSON

All API requests and responses use **snake_case**.

```json
{
  "student_id": "uuid",
  "registration_number": "STD-2026-0001",
  "first_name": "Jean",
  "last_name": "Nkodo"
}
```

### 4.4 Frontend Angular

- Component, service, and module files: **kebab-case**
- TypeScript interfaces: aligned with API DTOs
- HTTP calls centralized in dedicated services

---

## 5. Target technical architecture

### 5.1 Frontend (Angular)

Technologies:

- Angular 20+, Angular Router, Angular Reactive Forms
- Angular Material or equivalent
- Signals API for local state management
- Typed HTTP services
- Role-based authentication guards
- HTTP interceptor for JWT token injection
- Domain-driven modular architecture

```text
frontend/
├── src/
│   ├── app/
│   │   ├── core/
│   │   ├── shared/
│   │   ├── features/
│   │   │   ├── administration/
│   │   │   ├── academic/
│   │   │   ├── students/
│   │   │   ├── teachers/
│   │   │   ├── attendance/
│   │   │   ├── grades/
│   │   │   ├── billing/
│   │   │   └── dashboards/
│   │   └── layouts/
│   └── environments/
```

### 5.2 Backend (ASP.NET Core)

Technologies:

- ASP.NET Core 9 (or latest stable version)
- Entity Framework Core
- SQLite (MVP) / MySQL (evolution)
- JWT Bearer Authentication
- Clean Architecture (Domain / Application / Infrastructure / Api)

```text
backend/
├── School365.Api/
├── School365.Application/
├── School365.Domain/
├── School365.Infrastructure/
└── School365.Tests/
```

---

## 6. Business rules

### 6.1 School management

| Code      | Rule                                                                                         |
| --------- | -------------------------------------------------------------------------------------------- |
| RG-A1-001 | The school name is mandatory.                                                                |
| RG-A1-002 | The school code must be unique.                                                              |
| RG-A1-003 | A deactivated school must no longer allow new enrollments.                                   |
| RG-A1-004 | A school must have an academic day template before creating a timetable.                     |
| RG-A1-005 | A school can be created by copying the configuration of another school (template/copy mode). |

### 6.2 Academic day template

| Code      | Rule                                                                                                                      |
| --------- | ------------------------------------------------------------------------------------------------------------------------- |
| RG-JA-001 | The day start time must be earlier than the day end time.                                                                 |
| RG-JA-002 | A period duration must be strictly greater than 0.                                                                        |
| RG-JA-003 | A break must fall within the academic day.                                                                                |
| RG-JA-004 | Two breaks must not overlap.                                                                                              |
| RG-JA-005 | A period must not overlap a break.                                                                                        |
| RG-JA-006 | Generated periods are read-only.                                                                                          |
| RG-JA-007 | Any modification to the academic day template must trigger period regeneration.                                           |
| RG-JA-008 | If timetables already exist and are in use by the school, modifications must be blocked or require an explicit reset.     |
| RG-JA-009 | The total of generated period durations and breaks must be consistent with the defined day range (end time − start time). |
| RG-JA-010 | When configuring the first day, the system may offer to apply the same configuration to the other days of the week.       |

**Functional example:**

```text
Day start: 08:00  |  Day end: 16:50  |  Period duration: 50 min
Break 1: 10:30 (15 min)  |  Break 2: 12:25 (35 min)  |  Break 3: 15:30 (10 min)

P1: 08:00 - 08:50    P2: 08:50 - 09:40    P3: 09:40 - 10:30
Break 1: 10:30 - 10:45
P4: 10:45 - 11:35    P5: 11:35 - 12:25
Break 2: 12:25 - 13:00
P6: 13:00 - 13:50    P7: 13:50 - 14:40    P8: 14:40 - 15:30
Break 3: 15:30 - 15:40
P9: 15:40 - 16:30

The slot 16:30 - 16:50 does not generate a period (shorter than 50 min).
```

### 6.3 Academic years

| Code      | Rule                                                                           |
| --------- | ------------------------------------------------------------------------------ |
| RG-A2-001 | Only one academic year can be active per school at a time.                     |
| RG-A2-002 | The start date must be earlier than the end date.                              |
| RG-A2-003 | A closed academic year can no longer receive new grades or attendance records. |

### 6.4 Academic reference (cycles, levels, classes, subjects)

| Code      | Rule                                                                                |
| --------- | ----------------------------------------------------------------------------------- |
| RG-B1-001 | A cycle belongs to a school.                                                        |
| RG-B1-002 | A cycle can contain multiple levels.                                                |
| RG-B4-001 | A classroom group belongs to an academic year.                                      |
| RG-B4-002 | A classroom group belongs to a school.                                              |
| RG-B4-003 | A student can only be enrolled in one active classroom group per academic year.     |
| RG-B4-004 | Maximum capacity must not be exceeded without explicit administrator authorization. |
| RG-B5-001 | A subject code must be unique within a school.                                      |
| RG-B5-002 | A subject coefficient must be greater than or equal to 0.                           |
| RG-B5-003 | The number of periods per session must be greater than 0.                           |

### 6.5 Enrollments

| Code      | Rule                                                                                                 |
| --------- | ---------------------------------------------------------------------------------------------------- |
| RG-C1-001 | A student's registration number must be unique.                                                      |
| RG-C1-002 | A student must be linked to a school.                                                                |
| RG-C2-001 | A student must have at least one responsible contact (primary and secondary school context).         |
| RG-C3-001 | A student cannot have two validated enrollments in two different classes for the same academic year. |
| RG-C3-002 | A validated enrollment must create or update the student's academic record.                          |
| RG-C3-003 | A validated enrollment may trigger automatic generation of enrollment fees.                          |

### 6.6 Timetables

| Code      | Rule                                                                                                    |
| --------- | ------------------------------------------------------------------------------------------------------- |
| RG-E1-001 | A course must use only generated periods.                                                               |
| RG-E1-002 | A course may span multiple consecutive periods.                                                         |
| RG-E1-003 | A teacher cannot be assigned to two courses at the same time.                                           |
| RG-E1-004 | A class cannot have two courses at the same time.                                                       |
| RG-E1-005 | A room cannot be used by two courses at the same time.                                                  |
| RG-E1-006 | A course must not cross a break.                                                                        |
| RG-E1-007 | If a course requires 2 periods, the system must verify that both periods are consecutive and available. |

### 6.7 Attendance

| Code      | Rule                                                                                              |
| --------- | ------------------------------------------------------------------------------------------------- |
| RG-F1-001 | An attendance session is linked to a date, a class, a subject, and optionally a scheduled course. |
| RG-F1-002 | A student can only have one attendance status for the same course.                                |
| RG-F1-003 | A late arrival may be associated with a duration in minutes.                                      |
| RG-F1-004 | An excused absence must include a reason or a supporting document.                                |

Possible statuses: **present**, **absent**, **late**, **excused**.

### 6.8 Grades and evaluations

| Code      | Rule                                                                                                       |
| --------- | ---------------------------------------------------------------------------------------------------------- |
| RG-G2-001 | An evaluation must be linked to a class and a subject.                                                     |
| RG-G2-002 | The maximum score must be greater than 0.                                                                  |
| RG-G2-003 | The coefficient must be greater than 0.                                                                    |
| RG-G3-001 | A grade cannot exceed the evaluation's maximum score.                                                      |
| RG-G3-002 | A grade cannot be negative.                                                                                |
| RG-G3-003 | A locked grade can only be modified by an authorized profile.                                              |
| RG-G4-001 | Average calculation must account for evaluation coefficients.                                              |
| RG-G4-002 | General average calculation must account for subject coefficients.                                         |
| RG-G4-003 | Absences from evaluations must be handled according to a configurable rule: ignored / zero score / retake. |

**Calculation formulas:**

```text
subject_average = sum(normalized_score × evaluation_coefficient) / sum(evaluation_coefficients)
general_average = sum(subject_average × subject_coefficient)     / sum(subject_coefficients)
```

### 6.9 Report cards

| Code      | Rule                                                                                   |
| --------- | -------------------------------------------------------------------------------------- |
| RG-H1-001 | A report card can only be generated if the required grades are available.              |
| RG-H1-002 | A validated report card must be archived.                                              |
| RG-H1-003 | A validated report card cannot be modified without reopening by an authorized profile. |

### 6.10 Payments and billing

| Code      | Rule                                                                                     |
| --------- | ---------------------------------------------------------------------------------------- |
| RG-I1-001 | A fee type belongs to a school.                                                          |
| RG-I1-002 | A fee type may be mandatory or optional.                                                 |
| RG-I2-001 | An invoice must be linked to a student.                                                  |
| RG-I2-002 | An issued invoice must have a unique number.                                             |
| RG-I2-003 | Invoice total equals the sum of lines minus discounts.                                   |
| RG-I2-004 | A paid invoice cannot be deleted.                                                        |
| RG-I3-001 | A payment amount must be strictly greater than 0.                                        |
| RG-I3-002 | A payment cannot exceed the remaining invoice balance unless the system allows advances. |
| RG-I3-003 | Every validated payment must generate a receipt.                                         |
| RG-I3-004 | A receipt must have a unique number.                                                     |

**Supported payment methods:** cash, bank transfer, card, mobile money, cheque.

---

## 7. Security and RBAC

### 7.1 Authentication

- JWT Bearer Authentication is mandatory on all protected endpoints.

### 7.2 Initial roles

```text
super_admin   school_admin   director   registrar
teacher       accountant     parent     student
```

### 7.3 RBAC rules

| Code        | Rule                                                                               |
| ----------- | ---------------------------------------------------------------------------------- |
| RG-RBAC-001 | A user may only access data from their own school, except the super administrator. |
| RG-RBAC-002 | A teacher may only enter grades for their assigned classes and subjects.           |
| RG-RBAC-003 | A parent may only view data related to their associated students.                  |
| RG-RBAC-004 | An accountant may manage invoices and payments but cannot modify grades.           |

### 7.4 Initial permissions

```text
schools.read          schools.create        schools.update
academic_years.manage students.manage       teachers.manage
attendance.manage     grades.manage         invoices.manage
payments.manage       reports.read
```

---

## 8. REST API contract

### 8.1 General principles

- REST + JSON, snake_case in all payloads
- Versioned routes: `/api/v1`
- JWT-secured, documented via OpenAPI / Swagger

### 8.2 Endpoints

#### Administration

```text
GET    /api/v1/schools
POST   /api/v1/schools
GET    /api/v1/schools/{school_id}
PUT    /api/v1/schools/{school_id}
DELETE /api/v1/schools/{school_id}
```

#### Academic day template

```text
GET  /api/v1/schools/{school_id}/academic-day-template
POST /api/v1/schools/{school_id}/academic-day-template
PUT  /api/v1/academic-day-templates/{template_id}
POST /api/v1/academic-day-templates/{template_id}/generate-periods
GET  /api/v1/academic-day-templates/{template_id}/generated-periods
```

#### Academic years

```text
GET  /api/v1/schools/{school_id}/academic-years
POST /api/v1/schools/{school_id}/academic-years
PUT  /api/v1/academic-years/{academic_year_id}
POST /api/v1/academic-years/{academic_year_id}/activate
POST /api/v1/academic-years/{academic_year_id}/close
```

#### Students

```text
GET  /api/v1/students
POST /api/v1/students
GET  /api/v1/students/{student_id}
PUT  /api/v1/students/{student_id}
```

#### Enrollments

```text
GET  /api/v1/enrollments
POST /api/v1/enrollments
GET  /api/v1/enrollments/{enrollment_id}
POST /api/v1/enrollments/{enrollment_id}/validate
POST /api/v1/enrollments/{enrollment_id}/cancel
```

#### Attendance

```text
GET  /api/v1/attendance-sessions
POST /api/v1/attendance-sessions
GET  /api/v1/attendance-sessions/{attendance_session_id}
POST /api/v1/attendance-sessions/{attendance_session_id}/records
PUT  /api/v1/attendance-records/{attendance_record_id}
```

#### Grades

```text
GET  /api/v1/evaluations
POST /api/v1/evaluations
GET  /api/v1/evaluations/{evaluation_id}
POST /api/v1/evaluations/{evaluation_id}/grades
PUT  /api/v1/grades/{grade_id}
POST /api/v1/evaluations/{evaluation_id}/lock
```

#### Payments

```text
GET  /api/v1/invoices
POST /api/v1/invoices
GET  /api/v1/invoices/{invoice_id}
POST /api/v1/invoices/{invoice_id}/payments
GET  /api/v1/payments/{payment_id}/receipt
```

---

## 9. Development recommendations

### 9.1 Angular

- Create one module per functional domain.
- Use TypeScript interfaces aligned with API DTOs.
- Centralize HTTP calls in services.
- Create smart/dumb components when appropriate.
- Use Reactive Forms for complex forms.
- Use role-based guards.
- Use an interceptor to inject the JWT token.
- Design for responsiveness.

### 9.2 ASP.NET Core

- Separate Domain, Application, Infrastructure, and Api layers.
- Use DTOs for API contracts — never expose EF entities directly.
- Use FluentValidation or equivalent.
- Use EF Core migrations.
- Centralize error handling.
- Use domain-scoped application services.
- Document all endpoints via Swagger.
- Write unit tests for critical rules: period generation, average calculation, balance calculation.

---

_End of document — school365_conventions_en.md_
