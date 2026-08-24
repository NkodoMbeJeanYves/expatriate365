# SCHOOL365 — Additional Conventions and Decisions v4 (EN)

**Convention version:** 4.0  
**Date:** 2026-07-08  
**Purpose:** This file documents conventions established implicitly in development sessions or visible in the codebase but absent from v1–v3. It does not duplicate content already covered in those files — read them first.

---

## Table of Contents

1. Precise technology stack
2. Pagination — wire format
3. API error format
4. Soft-delete and audit fields (clarification of v1)
5. DTO patterns (Create / Update / Read)
6. Code generation format
7. Angular routing conventions
8. TypeScript path aliases
9. JWT claims
10. Angular user model
11. Angular component conventions
12. i18n key structure
13. Environment configuration

---

## 1. Precise Technology Stack

The CLAUDE.md reference stack, promoted here as a binding convention:

**Frontend**
- Angular **21** (not "20+" — use the exact major version)
- PrimeNG **21** — chosen UI component library (replaces "Angular Material or equivalent")
- Tailwind CSS **4**
- `@ngx-translate` for i18n
- Angular Service Worker (PWA support)

**Backend**
- ASP.NET Core **9**
- Entity Framework Core
- SQLite (MVP) / MySQL (evolution) — see DB-CONF in v3
- Serilog for structured logging
- Scalar / Swagger / OpenAPI for documentation

---

## 2. Pagination — Wire Format

All paginated endpoints use the same envelope on the wire. Both backend and frontend types mirror this shape exactly.

**JSON response:**
```json
{
  "data": [...],
  "pagination": {
    "page": 1,
    "limit": 20,
    "total": 143
  }
}
```

**Query parameters:** `page` (default `1`), `limit` (default `20`).

**C# types** — `School365.Application.Common`:
```csharp
public class PagedResult<T>  { public IEnumerable<T> Data; public PaginationMeta Pagination; }
public class PaginationMeta  { public int Page; public int Limit; public int Total; }
```

**TypeScript types** — `core/models/pagination.model.ts`:
```ts
export interface PaginationMeta { page: number; limit: number; total: number; }
export interface PagedResult<T>  { data: T[]; pagination: PaginationMeta; }
```

**Exception — AuditLog:** The audit-log endpoint uses offset-based pagination (`{ data, total, limit, offset }`) instead of the standard page-based envelope. This is a known inconsistency and applies only to that endpoint.

---

## 3. API Error Format

All API errors return a single-key JSON object with snake_case:

```json
{ "error": "Human-readable message" }
```

**Rules:**
- Never use `{ "errors": [...] }` or `{ "message": "..." }`.
- HTTP status codes: `400` (validation), `404` (not found), `409` (conflict), `500` (server error).

**Backend pattern — `ServiceResult<T>`** (`School365.Application.Common.ServiceResult`):
```csharp
ServiceResult<T>.Success(data)
ServiceResult<T>.Failure("message", errorCode?)
```

Controller translation:
```csharp
return result.IsSuccess
    ? Ok(result.Data)
    : BadRequest(new { error = result.ErrorMessage });   // or NotFound(...)
```

---

## 4. Soft-Delete and Audit Fields (Clarification of v1)

v1 lists `created_by` and `updated_by` as required audit fields. **The actual implementation uses only these fields:**

| Field | Type | Required? | Notes |
|---|---|---|---|
| `is_active` | `boolean` | yes | sole soft-delete flag |
| `created_at` | ISO 8601 string | yes | set on create, never modified |
| `updated_at` | ISO 8601 string | no (optional) | null until first update |

**There is no** `deleted_at`, `deleted_by`, `archived_at`, `created_by`, or `updated_by` in the current implementation. `is_active = false` is the only soft-delete mechanism.

On the TypeScript side, `updated_at` is always typed as `string | undefined` (`updated_at?: string`).

---

## 5. DTO Patterns (Create / Update / Read)

### Read DTOs
- Always include: `id`, `school_id`, `is_active`, `created_at`, `updated_at?`
- May include denormalized display fields: `student_name`, `academic_year_name`, `classroom_group_name`, `registration_number`
- These denormalized fields avoid extra round-trips from the list/detail UI

### CreateRequest DTOs
- Omit: `id` (generated server-side), `school_id` (from URL path param), all audit fields
- The school context is always provided via the route, not the body

### UpdateRequest DTOs
- Include `is_active` (allows toggle)
- Omit: `id`, `school_id`, `created_at`, `updated_at`

---

## 6. Code Generation Format

Auto-generated codes follow this pattern:

```
{PREFIX}-{NNNN}
```

- Prefix: uppercase letters (e.g. `STU`, `GRP`, `ENR`)
- Sequence: 4-digit zero-padded integer (e.g. `0001`, `0023`)
- Examples: `STU-0001`, `GRP-0023`, `ENR-0142`

Implemented in: `School365.Application.Common.CodeGenerator.Generate(string prefix, int sequence)`

---

## 7. Angular Routing Conventions

- **All routes use `loadComponent`** (never `loadChildren`). All components are standalone and lazy-loaded.
- **Public routes** live at the root level: `/login`, `/forgot-password`, `/reset-password/:token`, `/verify-email`, `/resend-verification`, `/unauthorized`.
- **Protected admin subtree** lives under `/admin/` with `canActivate: [authGuard, roleGuard]`.
- **Role data** is passed as `data: { roles: ['role_name'] }` — an array of snake_case role strings matching the values defined in section 7 of v1 (e.g. `'school_admin'`, `'super_admin'`).
- **Default redirect**: each feature section has a `{ path: '', redirectTo: '<default>', pathMatch: 'full' }` child route.

---

## 8. TypeScript Path Aliases

Configured in `client/tsconfig.json` with `baseUrl: "src"`. Requires `"ignoreDeprecations": "6.0"` for TypeScript 6 compatibility.

| Alias | Resolves to |
|---|---|
| `@guard/*` | `app/core/guards/*` |
| `@models/*` | `app/core/models/*` |
| `@service/*` | `app/core/services/*` |
| `@shared/*` | `app/shared/*` |
| `@administration/*` | `app/features/administration/*` |
| `@auth/*` | `app/features/auth/*` |
| `@enrollment/*` | `app/features/enrollment/*` |
| `@users/*` | `app/features/users/*` |

**Extension rule:** Every new module created under `features/` must receive a corresponding alias `@<module>/*` → `app/features/<module>/*` in `tsconfig.json`. No relative cross-boundary imports (`../../..`) are permitted between features or from features into core/shared.

---

## 9. JWT Claims

Custom claims embedded in the access token:

| Claim | Standard? | Value |
|---|---|---|
| `sub` | yes | user UUID |
| `email` | yes | user email |
| `jti` | yes | UUID per token |
| `full_name` | **custom** | full name — not `name` |
| `school_id` | **custom** | UUID; absent if user has no school |
| `entity_type` | **custom** | e.g. `"teacher"`, `"student"` |
| `entity_id` | **custom** | UUID of the linked business entity |

**Token storage:**
- Refresh token: 64 random bytes, Base64 plaintext transmitted + SHA-256 hex stored in DB
- Verification/reset token: 32 random bytes, lowercase hex transmitted + SHA-256 hex stored in DB

---

## 10. Angular User Model

Defined in `core/models/auth.model.ts`:

```ts
interface UserInfo {
  id: string;
  email: string;
  full_name: string;
  roles: string[];
  school_id?: string;
  entity_type?: string;
  entity_id?: string;
  email_verified_at?: string;
}

interface MeResponse extends UserInfo {
  permissions: string[];
}

interface LoginResponse {
  access_token: string;
  refresh_token: string;
  expires_in: number;
  user: UserInfo;
}
```

**Pattern:** `CurrentUserService` holds `signal<MeResponse | null>` with `hasRole(role)` and `hasAnyRole(roles[])` helpers.

---

## 11. Angular Component Conventions

These rules apply to every component in the project:

| Rule | Correct | Forbidden |
|---|---|---|
| Always standalone | `standalone: true` | NgModule-based components |
| Change detection | `ChangeDetectionStrategy.OnPush` | Default change detection |
| Signal inputs | `input.required<T>()` / `input<T>(default)` | `@Input()` decorator |
| Signal outputs | `output<T>()` | `@Output() EventEmitter` |
| Derived state | `computed(() => ...)` | Calculations in templates |
| Async composition | `forkJoin`, `switchMap`, `combineLatest`, `concat` | Nested subscribes |
| Subscription cleanup | `Subject<void>` + `takeUntil(destroy$)` in `ngOnDestroy` | Unmanaged subscriptions |

---

## 12. i18n Key Structure

Translation files live in `client/public/i18n/en.json` and `fr.json`.

**Format:** `<section>.<key>` dot-notation.

**Section naming:** one section per feature domain — `nav`, `auth`, `common`, `errors`, `enrollment`, `catalog`, `administration`, etc.

**Status keys:** dynamic status values use the pattern `<section>.status_<value>`:
```
enrollment.status_draft
enrollment.status_validated
enrollment.status_cancelled
enrollment.status_rejected
enrollment.status_pending
```

**Template usage:**
```html
{{ ('enrollment.status_' + status()) | translate }}
```

This pattern allows new statuses to be added without modifying component code — only the JSON files need updating.

---

## 13. Environment Configuration

`client/src/environments/environment.ts` shape:

```ts
export const environment = {
  production: boolean,
  apiUrl: string,              // base URL without trailing slash, e.g. 'http://localhost:5000'
  quickLoginAccounts: QuickLoginAccount[],
};

interface QuickLoginAccount {
  label: string;
  email: string;
  password: string;
}
```

**Rules:**
- `apiUrl` never ends with `/`.
- `quickLoginAccounts` is always an empty array in `environment.prod.ts`.
- Dev credentials go in `quickLoginAccounts` only — never hardcoded in component logic.

---

_End of document — school365_conventions_en_v4.md_
