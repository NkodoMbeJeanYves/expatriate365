# CommunityHub SaaS — Complete Functional & Technical Specification

**Version:** 1.0 | **Date:** 2026-08-20 | **Classification:** Confidential

---

# 1. Executive Summary

## Vision

CommunityHub is a cloud-native, multi-tenant SaaS platform purpose-built for diaspora associations, expatriate communities, cultural organizations, alumni networks, religious congregations, and professional societies. It consolidates member management, contribution tracking, financial transparency, event coordination, and governance into a single, mobile-first platform — eliminating the fragmented use of spreadsheets, WhatsApp groups, and manual bank reconciliation that plagues most community organizations today.

## Objectives

| # | Objective | Success Metric |
|---|-----------|----------------|
| 1 | Digitize contribution collection end-to-end | 100% of payments traceable with receipt |
| 2 | Enforce financial transparency | Real-time treasury dashboard accessible to all members |
| 3 | Reduce administrative overhead | 70% reduction in manual bookkeeping time |
| 4 | Improve member retention | Engagement score tracked monthly |
| 5 | Scale from 50 to 50,000 members per community | Zero degradation at scale |
| 6 | Enable multi-currency, multi-country operations | Support for 50+ currencies and mobile money |

## Target Users

- **Primary:** Community presidents, treasurers, secretaries of diaspora associations
- **Secondary:** Ordinary members paying contributions and attending events
- **Tertiary:** Auditors, regional coordinators, event managers
- **Platform Operator:** The SaaS operator managing subscriptions and tenants

## Expected Benefits

- Eliminate contribution fraud through immutable payment audit trails
- Provide members with self-service portals for payment history and certificates
- Enable data-driven governance with election management and vote tracking
- Reduce non-payment rates through automated reminders and defaulter reports
- Attract new members with digital membership cards and professional community image

---

# 2. User Roles

## 2.1 Super Admin (Platform Operator)

**Responsibilities:** Manages the entire SaaS platform across all tenants.

**Permissions:**
- Create, suspend, and delete community tenants
- Access billing and subscription management
- View cross-tenant analytics (anonymized)
- Manage feature flags per subscription tier
- Access platform-level audit logs
- Configure global system settings

**Restrictions:** Cannot access community-level member personal data unless requested for support.

---

## 2.2 Community President

**Responsibilities:** Legal representative of the community. Strategic leadership.

**Permissions:**
- Full read/write access to all community modules
- Approve or reject executive committee decisions
- Sign off on financial reports
- Initiate elections
- Approve welfare assistance requests
- Send community-wide announcements

**Restrictions:** Cannot modify platform subscription billing directly.

---

## 2.3 Treasurer

**Responsibilities:** Manages all financial operations of the community.

**Permissions:**
- Record income and expenses
- Validate and confirm payments
- Generate financial reports and statements
- Manage budget lines
- Approve waivers for contribution late fees
- Export financial data to CSV/PDF

**Restrictions:** Cannot modify member personal data; cannot approve welfare requests over a configured threshold without President co-approval.

---

## 2.4 Secretary

**Responsibilities:** Administrative operations, communications, minutes.

**Permissions:**
- Manage member registration and profile updates
- Publish announcements and news
- Manage event creation and invitations
- Record meeting minutes and action items
- Generate membership certificates and official letters
- Manage document archive

**Restrictions:** No access to treasury or financial ledger.

---

## 2.5 Committee Member

**Responsibilities:** Participates in governance decisions and community activities.

**Permissions:**
- View member directory (limited PII)
- Participate in votes and elections
- View financial summary (not full ledger)
- Create and manage events assigned to them
- View meeting agendas and minutes

**Restrictions:** Cannot approve payments, modify member data, or access welfare request details beyond summary.

---

## 2.6 Auditor

**Responsibilities:** Independent financial oversight and compliance verification.

**Permissions:**
- Read-only access to full financial ledger
- View all payment records and receipts
- View audit logs (financial events only)
- Generate and export audit reports
- Flag discrepancies for review

**Restrictions:** No write access to any module. Cannot approve payments or modify data.

---

## 2.7 Regional Coordinator

**Responsibilities:** Manages a geographic sub-group of members.

**Permissions:**
- View and manage members in their assigned region
- Send announcements to regional members
- Record regional event attendance
- Generate regional contribution reports

**Restrictions:** No access to members outside their region; no access to full treasury.

---

## 2.8 Event Manager

**Responsibilities:** Plans and manages community events.

**Permissions:**
- Create, publish, and cancel events
- Manage invitations and RSVPs
- Track event attendance
- Manage ticket sales and pricing
- Generate event reports

**Restrictions:** No access to financial ledger or member personal financial data.

---

## 2.9 Member

**Responsibilities:** Active community participant paying contributions.

**Permissions:**
- View and update own profile
- Pay contributions via any supported payment method
- View personal payment history and download receipts
- RSVP to events
- Submit welfare assistance requests
- View community announcements and documents
- Vote in elections (if eligible)
- Download own membership certificate

**Restrictions:** Cannot view other members' financial data; cannot access governance or admin modules.

---

## 2.10 Guest

**Responsibilities:** Prospective member browsing community information.

**Permissions:**
- View public community profile
- View public events
- Submit membership application

**Restrictions:** No access to any internal modules until application is approved.

---

# 3. Complete Functional Modules

---

## MODULE 1: Identity & Administration

### Business Overview
Provides secure authentication, role-based authorization, and complete audit trail for all user actions across the platform. Foundation of all other modules.

### Objectives
- Secure access with MFA
- Enforce RBAC across all modules
- Maintain immutable audit logs for compliance

### Features
- Email/password authentication with bcrypt hashing
- Social login (Google, Facebook) via OAuth 2.0
- Multi-Factor Authentication (TOTP, SMS OTP)
- JWT access tokens (15-minute expiry) + Refresh tokens (30-day expiry)
- Role assignment per community (a user can be Member in Community A and President in Community B)
- Session management with device tracking
- Password reset via signed time-limited email link
- Account lockout after 5 failed attempts (30-minute lockout)
- Audit logs: every CREATE, UPDATE, DELETE action logged with userId, timestamp, IP, and changed values (before/after diff)

### User Stories

| ID | As a... | I want to... | So that... |
|----|---------|--------------|------------|
| US-001 | Member | Log in with email and password | I can access my community portal |
| US-002 | Member | Enable TOTP MFA | My account is protected |
| US-003 | President | Assign roles to members | The right people have the right access |
| US-004 | Auditor | View audit logs | I can trace every financial change |
| US-005 | Super Admin | Suspend a community tenant | I can enforce terms of service |

### Business Rules
- BR-001: A user must verify their email before accessing any module
- BR-002: MFA is mandatory for Treasurer, President, and Auditor roles
- BR-003: Refresh tokens are invalidated on password change
- BR-004: Audit logs are immutable; no role can delete them
- BR-005: A user can belong to multiple communities with different roles per community

### Process Flow
```
Register → Verify Email → Set Password → (MFA Setup if required) → Login
→ JWT Issued → Access Module → Action Logged → Token Refresh as needed
```

### Security Rules
- Passwords: minimum 10 chars, 1 uppercase, 1 digit, 1 special char
- All tokens transmitted over HTTPS only
- JWT signed with RS256 (asymmetric)
- Refresh tokens stored as httpOnly secure cookies
- Rate limiting: 10 login attempts per 15 minutes per IP

### Acceptance Criteria
- AC-001: Login succeeds with valid credentials in < 500ms
- AC-002: Failed MFA blocks access and logs the attempt
- AC-003: Audit log entry created within 1 second of any data mutation
- AC-004: Account locks after 5 failed logins

---

## MODULE 2: Community Management

### Business Overview
Each community is a fully isolated tenant. This module manages the community's identity, structure, leadership, and operational settings.

### Features
- Community profile (name, logo, description, country, currency, languages)
- Sub-region/chapter management
- Executive committee roster with mandate dates
- Mandate tracking (start date, end date, renewal alerts)
- Community bylaws document upload
- Public community landing page
- Community settings (payment methods enabled, contribution schedules, late fee rules)
- Subscription tier management (linked to SaaS billing)

### User Stories

| ID | As a... | I want to... |
|----|---------|--------------|
| US-010 | President | Update our community profile and logo | Members see a professional identity |
| US-011 | Secretary | Define our executive committee structure | Roles and mandates are formally recorded |
| US-012 | Super Admin | Onboard a new community tenant | The community can start using the platform |

### Business Rules
- BR-010: Each community is isolated; no cross-tenant data access
- BR-011: Community must have exactly one President at any time
- BR-012: Executive mandates must have defined start and end dates
- BR-013: Mandate expiry triggers an alert 60 and 30 days in advance

### Database Tables
`Communities`, `Chapters`, `ExecutiveCommittee`, `Mandates`

---

## MODULE 3: Member Management

### Business Overview
Core registry of all individuals belonging to the community. Supports complex family structures, member categories, and lifecycle management.

### Features
- Member registration (self-service or admin-initiated)
- Member profile: personal info, photo, contact details, address, profession
- Family member and dependent tracking (spouse, children)
- Membership categories: Regular, Honorary, Student, Senior, Diaspora, Corporate
- Membership number auto-generation (configurable format)
- Membership status: Pending, Active, Suspended, Expired, Deceased
- Member search and filter
- Bulk import via CSV
- Member card generation (digital + printable PDF with QR code)
- Member directory (searchable, with PII access control)
- Deactivation workflow with reason recording

### User Stories

| ID | As a... | I want to... |
|----|---------|--------------|
| US-020 | Guest | Submit my membership application | I can join the community |
| US-021 | Secretary | Approve a membership application | New members are properly onboarded |
| US-022 | Member | Update my address and profession | My profile stays current |
| US-023 | President | View a list of all active members | I have an accurate community census |
| US-024 | Member | Download my digital membership card | I can prove my membership |

### Business Rules
- BR-020: Membership number is unique within a community
- BR-021: A member application must be approved by Secretary or President
- BR-022: Suspended members cannot pay contributions or vote
- BR-023: Deceased members are archived, not deleted (preserves payment history)
- BR-024: A member must be Active to be eligible for welfare assistance
- BR-025: Family members inherit the primary member's community ID

### Member Categorization

| Category | Contribution Rate | Voting Rights | Welfare Eligible |
|----------|------------------|---------------|-----------------|
| Regular | 100% | Yes | Yes |
| Honorary | 0% | No | No |
| Student | 50% | Yes | Yes |
| Senior | 25% | Yes | Yes |
| Diaspora | 100% | Yes | Yes |
| Corporate | 200% | No | No |

### Validation Rules
- Email: unique across the community
- Phone: E.164 format validated
- Date of birth: cannot be in the future; must be > 16 years for adult membership
- Photo: max 5MB, JPG/PNG only, face detection recommended

### Acceptance Criteria
- AC-020: Membership application processed in < 2 business days (SLA alert after 48h)
- AC-021: Member search returns results in < 300ms for communities up to 50,000 members
- AC-022: Digital membership card QR code verified in < 1 second

---

## MODULE 4: Contributions & Membership Fees

### Business Overview
Defines the contribution structure for the community and tracks what each member owes versus what they have paid.

### Features
- Fee schedule definition (monthly, quarterly, annual)
- Multiple fee types per community (regular dues, special levy, building fund, etc.)
- Per-category fee rates (students pay 50%, seniors pay 25%)
- Prorated fees for mid-year joiners
- Late payment penalties (configurable rate, grace period)
- Fee waiver management (with approval workflow)
- Exceptional/one-time contributions (funeral levy, emergency fund)
- Arrears calculation and aging report
- Contribution ledger per member (debit/credit view)
- Automatic charge generation at billing cycle start

### User Stories

| ID | As a... | I want to... |
|----|---------|--------------|
| US-030 | Treasurer | Define a monthly contribution of $20 for regular members | The system knows what to charge |
| US-031 | Treasurer | Grant a waiver to a member facing hardship | They are not penalized during difficulty |
| US-032 | Member | See exactly how much I owe | I can pay the correct amount |
| US-033 | Treasurer | Apply a late penalty after the grace period | Members are incentivized to pay on time |
| US-034 | President | Levy a one-time exceptional contribution | We can raise funds for a specific cause |

### Business Rules
- BR-030: Late penalty applies after a configurable grace period (default: 15 days)
- BR-031: Waivers require Treasurer approval; waivers > 3 months require President approval
- BR-032: Prorated fees calculated as: (monthly_fee / days_in_month) × remaining_days
- BR-033: Exceptional contributions must have a defined purpose and target amount
- BR-034: A member in arrears > 3 months is auto-flagged as "Defaulter"
- BR-035: Honorary members generate $0 contribution charges

### Fee Schedule Example

| Fee Type | Frequency | Regular | Student | Senior | Due Day |
|----------|-----------|---------|---------|--------|---------|
| Monthly Dues | Monthly | $20 | $10 | $5 | 1st of month |
| Annual Levy | Annual | $100 | $50 | $25 | January 1 |
| Building Fund | One-time | $200 | $100 | $50 | Defined |

---

## MODULE 5: Payment Management

### Business Overview
Records and reconciles all payments made by members against their contribution obligations. Supports multiple payment channels with full receipt generation.

### Features
- Cash payment recording (by Treasurer with witness confirmation)
- Bank transfer recording with reference matching
- Mobile money (M-Pesa, MTN MoMo, Orange Money, Wave) via API integration
- Card payment (Stripe/PayDunya/Flutterwave integration)
- Online payment link generation (shareable per member)
- Bulk payment recording (multiple members at once)
- Payment receipt generation (PDF with unique receipt number)
- Payment reconciliation dashboard
- Partial payment support with remaining balance tracking
- Payment reversal workflow (with mandatory reason and approval)
- Duplicate payment detection

### User Stories

| ID | As a... | I want to... |
|----|---------|--------------|
| US-040 | Member | Pay my dues online with a card | I don't need to come in person |
| US-041 | Treasurer | Record a cash payment received | It's officially documented |
| US-042 | Member | Receive a PDF receipt immediately after payment | I have proof of payment |
| US-043 | Treasurer | Reconcile mobile money payments against bank statement | I know what's arrived |
| US-044 | Treasurer | Reverse an erroneous payment | Mistakes can be corrected with a trail |

### Business Rules
- BR-040: Every payment must be assigned to a specific contribution charge
- BR-041: Cash payments require Treasurer + one witness confirmation
- BR-042: Payment reversals require President approval for amounts > configurable threshold
- BR-043: A payment cannot be reversed after 90 days
- BR-044: Receipt numbers are sequential and immutable
- BR-045: Online payments are auto-reconciled upon webhook confirmation from payment gateway

### Payment Status Flow
```
Pending → Processing → Confirmed → (Reversed if error, requires approval)
```

### Supported Payment Methods by Region

| Method | Africa | Europe | Americas |
|--------|--------|--------|---------|
| Card (Stripe) | ✓ | ✓ | ✓ |
| Mobile Money | ✓ | — | — |
| Bank Transfer | ✓ | ✓ | ✓ |
| PayDunya | ✓ (FCFA) | — | — |
| Flutterwave | ✓ | — | ✓ |
| Cash | ✓ | ✓ | ✓ |

---

## MODULE 6: Financial Management

### Business Overview
Full double-entry bookkeeping for community finances. Tracks all income and expenditure with budget controls and treasury oversight.

### Features
- Chart of accounts (configurable categories)
- Income recording (contributions, donations, event revenue, grants)
- Expense recording (operations, events, welfare, admin)
- Budget definition per fiscal year
- Budget vs. actual variance reporting
- Treasury dashboard with live balances
- Multi-account support (checking, savings, petty cash)
- Foreign currency handling with exchange rate recording
- Month-end and year-end close process
- Financial period locking (prevents backdating after close)

### User Stories

| ID | As a... | I want to... |
|----|---------|--------------|
| US-050 | Treasurer | Record an expense for event catering | It's tracked against the event budget |
| US-051 | President | See the current treasury balance | I know the community's financial health |
| US-052 | Auditor | View all transactions for Q3 | I can verify financial integrity |
| US-053 | Treasurer | Define a budget for the annual gala | Spending has approved limits |

### Business Rules
- BR-050: Every expense must have a category, approver, and supporting document (receipt/invoice)
- BR-051: Expenses > configurable threshold require President approval
- BR-052: Financial periods can only be closed by Treasurer with President co-signature
- BR-053: Budget overruns generate an alert but do not block transactions (configurable)
- BR-054: All financial figures are stored in the community's base currency; foreign amounts stored with FX rate at time of transaction

---

## MODULE 7: Accounting Reports

### Business Overview
Standardized financial reports for internal governance and external compliance.

### Features

**Member Financial Reports:**
- Individual member statement (contributions charged, payments made, balance)
- All-member balance summary (sortable by balance, name, region)
- Defaulters report (members with arrears > N months, configurable)
- Paid-in-full report for a given period
- Collection rate by period and by member category

**Community Financial Reports:**
- Income statement (P&L equivalent)
- Balance sheet
- Cash flow statement
- Budget vs. actual report
- Contribution collection summary (by month, by fee type)
- Expense breakdown by category
- Audit trail report

**Delivery:**
- Download as PDF (formatted, branded with community logo)
- Download as Excel/CSV
- Schedule automated email delivery (weekly, monthly, quarterly)
- Public financial summary (configurable — some communities publish to all members)

---

## MODULE 8: Assistance & Welfare

### Business Overview
Manages the community solidarity fund — emergency requests, medical aid, funeral contributions, and structured assistance programs.

### Features
- Welfare request submission (with type, amount requested, justification, supporting documents)
- Welfare request types: Emergency, Medical, Funeral/Bereavement, Educational, Repatriation
- Multi-level approval workflow (Committee review → Treasurer → President)
- Welfare fund balance tracking (dedicated sub-account)
- Disbursement recording with recipient confirmation
- Welfare contribution levy management (special collections for a specific member's need)
- Community solidarity campaigns (crowdfunding-style within the community)
- Welfare history per member (to prevent abuse)
- Anonymized welfare reporting to full membership

### User Stories

| ID | As a... | I want to... |
|----|---------|--------------|
| US-060 | Member | Submit an emergency assistance request | The community can support me |
| US-061 | President | Approve a funeral contribution request | The family receives timely support |
| US-062 | Treasurer | Record the welfare disbursement | The fund is accurately tracked |
| US-063 | Committee Member | Review a welfare request | I can evaluate it fairly |

### Business Rules
- BR-060: Only Active members with at least 6 months standing are eligible for welfare
- BR-061: Funeral assistance is automatically fast-tracked (max 24h approval)
- BR-062: A member cannot have two simultaneous open welfare requests
- BR-063: Welfare disbursements are capped at configurable maximums per request type
- BR-064: Welfare request details are visible only to approvers; other members see only anonymized statistics
- BR-065: Rejected requests must include a written reason visible to the applicant

### Approval Thresholds Example

| Request Type | Amount Threshold | Approvers Required |
|-------------|-----------------|-------------------|
| Emergency | ≤ $500 | Treasurer alone |
| Emergency | $500 – $2,000 | Treasurer + President |
| Medical | Any | Committee + President |
| Funeral | Any | President (fast-track) |
| Educational | Any | Full Committee vote |

---

## MODULE 9: Event Management

### Business Overview
End-to-end event lifecycle management from creation to post-event reporting.

### Features
- Event creation (name, date, location, description, cover image, capacity)
- Event types: General Assembly, Cultural Event, Funeral Support, Sports, Fundraiser, Training
- Invitation management (all members, selected members, regional members)
- RSVP tracking (attending, maybe, not attending)
- Waitlist management when capacity is reached
- Ticket management (free, paid, VIP tiers)
- QR code check-in at event venue
- Attendance recording (manual + QR scan)
- Event expense tracking (linked to financial module)
- Post-event report (attendance rate, revenue, expenses, net)
- Recurring event support

### User Stories

| ID | As a... | I want to... |
|----|---------|--------------|
| US-070 | Event Manager | Create the annual gala with ticketing | Members can register and pay |
| US-071 | Member | RSVP to the monthly meeting | The organizer knows to expect me |
| US-072 | Event Manager | Scan member QR codes at the door | Attendance is recorded instantly |
| US-073 | Treasurer | See the event's financial summary | I know profit/loss for the event |

### Business Rules
- BR-070: Paid events generate a payment obligation linked to the payment module
- BR-071: Cancellation of a paid event triggers automatic refund processing
- BR-072: RSVP deadline is configurable per event
- BR-073: Attendance can only be recorded on the event date ± 1 day

---

## MODULE 10: Communication Center

### Business Overview
Multi-channel communication platform for reaching all members or targeted segments.

### Features
- Announcement board (pinned posts with rich text and attachments)
- News feed (chronological community updates)
- Email campaigns (templates, scheduling, open-rate tracking)
- SMS campaigns (via Twilio/Africa's Talking/Orange SMS API)
- Push notifications (mobile app + web push)
- Audience targeting (all, by region, by status, by category, by arrears status)
- Communication templates library
- Delivery status tracking (sent, delivered, read)
- Opt-out management (SMS/email unsubscribe)
- Automated notifications (payment reminders, event reminders, birthday greetings)

### Automated Notification Triggers

| Trigger | Channel | Timing |
|---------|---------|--------|
| Contribution due | SMS + Email | 7 days before due date |
| Payment confirmed | Email + Push | Immediately |
| Payment overdue | SMS + Email | Day 1, Day 15, Day 30 |
| Event invitation | Email + Push | On creation, 3 days before |
| Welfare decision | Email + Push | On decision |
| Membership expiry | Email | 60 days, 30 days, 7 days |

---

## MODULE 11: Documents

### Business Overview
Official document generation and community document archive.

### Features

**Auto-generated documents:**
- Membership certificate (PDF with QR verification code)
- Contribution certificate (total paid in a fiscal year)
- Payment receipt (per transaction)
- Good standing letter
- Official community letter (with letterhead)

**Community document archive:**
- Meeting minutes
- Bylaws and statutes
- Financial statements (annual)
- Election results
- Welfare reports

- Document versioning
- Document access control (public, members only, committee only)
- S3-compatible storage with CDN delivery
- Digital signature support (DocuSign or built-in e-signature)

---

## MODULE 12: Governance

### Business Overview
Formalizes the democratic and administrative governance of the community.

### Features

**Elections:**
- Election campaign creation (positions, candidates, eligibility rules)
- Candidate nomination with self-nomination or committee nomination
- Online voting with eligibility enforcement (Active, dues paid)
- Anonymous vote recording
- Live and final result display
- Election audit trail

**Motions and Votes:**
- Motion creation by any committee member
- Seconding requirement
- Discussion period
- Quorum enforcement
- Majority type selection (simple, two-thirds, unanimous)

**Decision Register:**
- Every approved motion logged with date, voters, and outcome
- Decisions searchable and exportable

### Business Rules
- BR-120: A member must be Active and dues-current to vote in elections
- BR-121: Elections are anonymous; the system stores votes without member linkage after confirmation
- BR-122: Quorum must be reached before any election vote is valid
- BR-123: Election results are published automatically at the configured deadline

---

## MODULE 13: Meeting Management

### Business Overview
Tracks all official community meetings from agenda to follow-up.

### Features
- Meeting creation (type, date, location, virtual link, agenda items)
- Meeting types: General Assembly, Executive Meeting, Extraordinary Session, Committee Meeting
- Agenda builder with item ordering and time allocation
- Invitee list and RSVP
- Attendance confirmation (quorum check)
- Minutes recording (rich text, attachments)
- Action items with assignee, due date, and status
- Minutes approval workflow (draft → reviewed → approved)
- Minutes publication to members
- Follow-up tracking on previous meeting action items

---

## MODULE 14: Mobile Application

### Business Overview
Native mobile experience for members on iOS and Android.

### Features
- Member dashboard (balance, upcoming events, announcements)
- Pay contributions (all supported payment methods)
- Payment history with downloadable receipts
- Digital membership card with QR code
- Push notifications
- Event RSVP
- Document download (certificates, receipts)
- Welfare request submission
- Community directory (with PII controls)
- Offline mode for viewing last-loaded data
- Biometric authentication (Face ID / fingerprint)

---

## MODULE 15: Analytics & Reporting

### Business Overview
Data intelligence layer for community leaders and the platform operator.

### Features

**Member Analytics:**
- Growth chart (new members per month)
- Churn analysis (resignations, suspensions)
- Age distribution, gender distribution, region distribution
- Engagement score per member (events attended, payments on time, votes cast)

**Financial Analytics:**
- Collection rate trend (monthly)
- Defaulter risk score (ML-based)
- Revenue forecast
- Expense category breakdown

**Event Analytics:**
- Attendance rate per event type
- Revenue per event

**Platform KPIs (Super Admin):**
- Total tenants, total members across all communities
- MRR (Monthly Recurring Revenue)
- Churn rate per subscription tier

---

# 4. Database Design

## Entity Relationship Description

The database is organized around five core domains:
1. **Identity** (Users, Roles, Sessions)
2. **Community** (Communities, Chapters, Members, Categories)
3. **Finance** (Contributions, Payments, Expenses, Budget)
4. **Engagement** (Events, Announcements, Welfare, Elections)
5. **System** (AuditLogs, Notifications, Documents)

---

## Table Definitions

### `tenants`
| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| id | CHAR(36) | PK, UUID | Tenant identifier |
| name | VARCHAR(200) | NOT NULL | Community name |
| slug | VARCHAR(100) | UNIQUE | URL slug |
| subscription_tier | ENUM('free','starter','professional','enterprise') | NOT NULL | |
| subscription_status | ENUM('trial','active','suspended','cancelled') | NOT NULL | |
| base_currency | CHAR(3) | NOT NULL | ISO 4217 currency code |
| country_code | CHAR(2) | NOT NULL | ISO 3166 |
| logo_url | VARCHAR(500) | | S3 URL |
| created_at | DATETIME | NOT NULL | |
| updated_at | DATETIME | NOT NULL | |

**Indexes:** `slug` (UNIQUE)

---

### `users`
| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| id | CHAR(36) | PK, UUID | |
| email | VARCHAR(255) | UNIQUE, NOT NULL | |
| password_hash | VARCHAR(255) | NOT NULL | bcrypt |
| first_name | VARCHAR(100) | NOT NULL | |
| last_name | VARCHAR(100) | NOT NULL | |
| phone | VARCHAR(20) | | E.164 format |
| email_verified_at | DATETIME | | NULL if unverified |
| mfa_enabled | BOOLEAN | DEFAULT FALSE | |
| mfa_secret | VARCHAR(64) | | TOTP secret, encrypted |
| status | ENUM('active','locked','suspended') | NOT NULL | |
| last_login_at | DATETIME | | |
| created_at | DATETIME | NOT NULL | |
| updated_at | DATETIME | NOT NULL | |

**Indexes:** `email` (UNIQUE), `status`

---

### `members`
| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| id | CHAR(36) | PK, UUID | |
| tenant_id | CHAR(36) | FK → tenants.id, NOT NULL | |
| user_id | CHAR(36) | FK → users.id, NOT NULL | |
| membership_number | VARCHAR(50) | UNIQUE per tenant | |
| category_id | CHAR(36) | FK → membership_categories.id | |
| chapter_id | CHAR(36) | FK → chapters.id | |
| status | ENUM('pending','active','suspended','expired','deceased') | NOT NULL | |
| joined_date | DATE | NOT NULL | |
| expiry_date | DATE | | |
| photo_url | VARCHAR(500) | | |
| address | TEXT | | |
| profession | VARCHAR(200) | | |
| date_of_birth | DATE | | |
| gender | ENUM('M','F','other','prefer_not') | | |
| emergency_contact_name | VARCHAR(200) | | |
| emergency_contact_phone | VARCHAR(20) | | |
| created_at | DATETIME | NOT NULL | |
| updated_at | DATETIME | NOT NULL | |

**Indexes:** `tenant_id`, `user_id`, `status`, `membership_number`, `chapter_id`
**FK:** `tenant_id`, `user_id`, `category_id`, `chapter_id`

---

### `membership_categories`
| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| id | CHAR(36) | PK, UUID | |
| tenant_id | CHAR(36) | FK → tenants.id | |
| name | VARCHAR(100) | NOT NULL | |
| description | TEXT | | |
| contribution_rate | DECIMAL(5,2) | NOT NULL DEFAULT 100.00 | Percentage of base fee |
| voting_rights | BOOLEAN | DEFAULT TRUE | |
| welfare_eligible | BOOLEAN | DEFAULT TRUE | |
| created_at | DATETIME | NOT NULL | |

---

### `family_members`
| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| id | CHAR(36) | PK | |
| member_id | CHAR(36) | FK → members.id | Primary member |
| first_name | VARCHAR(100) | NOT NULL | |
| last_name | VARCHAR(100) | NOT NULL | |
| relationship | ENUM('spouse','child','parent','sibling','other') | NOT NULL | |
| date_of_birth | DATE | | |
| created_at | DATETIME | NOT NULL | |

---

### `roles`
| Column | Type | Constraints |
|--------|------|-------------|
| id | CHAR(36) | PK |
| tenant_id | CHAR(36) | FK → tenants.id (NULL = platform role) |
| name | VARCHAR(50) | NOT NULL |
| permissions | JSON | Array of permission strings |
| is_system_role | BOOLEAN | DEFAULT FALSE |
| created_at | DATETIME | NOT NULL |

---

### `member_roles`
| Column | Type | Constraints |
|--------|------|-------------|
| id | CHAR(36) | PK |
| member_id | CHAR(36) | FK → members.id |
| role_id | CHAR(36) | FK → roles.id |
| assigned_by | CHAR(36) | FK → members.id |
| assigned_at | DATETIME | NOT NULL |
| expires_at | DATETIME | |

---

### `contribution_types`
| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| id | CHAR(36) | PK | |
| tenant_id | CHAR(36) | FK → tenants.id | |
| name | VARCHAR(200) | NOT NULL | e.g. "Monthly Dues" |
| description | TEXT | | |
| frequency | ENUM('monthly','quarterly','biannual','annual','one_time') | NOT NULL | |
| base_amount | DECIMAL(12,2) | NOT NULL | Base amount in community currency |
| late_penalty_rate | DECIMAL(5,2) | DEFAULT 0.00 | Percentage |
| grace_period_days | INT | DEFAULT 15 | |
| is_active | BOOLEAN | DEFAULT TRUE | |
| effective_from | DATE | NOT NULL | |
| effective_to | DATE | | |
| created_at | DATETIME | NOT NULL | |

---

### `contribution_charges`
| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| id | CHAR(36) | PK | |
| tenant_id | CHAR(36) | FK → tenants.id | |
| member_id | CHAR(36) | FK → members.id | |
| contribution_type_id | CHAR(36) | FK → contribution_types.id | |
| due_date | DATE | NOT NULL | |
| base_amount | DECIMAL(12,2) | NOT NULL | |
| penalty_amount | DECIMAL(12,2) | DEFAULT 0.00 | |
| waiver_amount | DECIMAL(12,2) | DEFAULT 0.00 | |
| total_due | DECIMAL(12,2) | GENERATED | base + penalty - waiver |
| amount_paid | DECIMAL(12,2) | DEFAULT 0.00 | |
| balance | DECIMAL(12,2) | GENERATED | total_due - amount_paid |
| status | ENUM('pending','partial','paid','waived','overdue') | NOT NULL | |
| created_at | DATETIME | NOT NULL | |

**Indexes:** `tenant_id`, `member_id`, `status`, `due_date`

---

### `payments`
| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| id | CHAR(36) | PK | |
| tenant_id | CHAR(36) | FK → tenants.id | |
| member_id | CHAR(36) | FK → members.id | |
| charge_id | CHAR(36) | FK → contribution_charges.id | |
| receipt_number | VARCHAR(50) | UNIQUE | Sequential per tenant |
| amount | DECIMAL(12,2) | NOT NULL | |
| currency | CHAR(3) | NOT NULL | |
| payment_method_id | CHAR(36) | FK → payment_methods.id | |
| payment_gateway | VARCHAR(50) | | stripe, flutterwave, etc. |
| gateway_transaction_id | VARCHAR(200) | | |
| gateway_reference | VARCHAR(200) | | |
| status | ENUM('pending','processing','confirmed','reversed','failed') | NOT NULL | |
| confirmed_at | DATETIME | | |
| confirmed_by | CHAR(36) | FK → members.id | NULL for auto-confirmed |
| reversed_at | DATETIME | | |
| reversed_by | CHAR(36) | FK → members.id | |
| reversal_reason | TEXT | | |
| notes | TEXT | | |
| payment_date | DATE | NOT NULL | |
| created_at | DATETIME | NOT NULL | |

**Indexes:** `tenant_id`, `member_id`, `status`, `payment_date`, `receipt_number`

---

### `payment_methods`
| Column | Type | Constraints |
|--------|------|-------------|
| id | CHAR(36) | PK |
| tenant_id | CHAR(36) | FK → tenants.id |
| name | VARCHAR(100) | NOT NULL |
| type | ENUM('cash','bank_transfer','mobile_money','card','online') | NOT NULL |
| provider | VARCHAR(50) | e.g. mpesa, stripe |
| is_active | BOOLEAN | DEFAULT TRUE |
| config | JSON | Encrypted gateway credentials |
| created_at | DATETIME | NOT NULL |

---

### `waivers`
| Column | Type | Constraints |
|--------|------|-------------|
| id | CHAR(36) | PK |
| tenant_id | CHAR(36) | FK → tenants.id |
| charge_id | CHAR(36) | FK → contribution_charges.id |
| member_id | CHAR(36) | FK → members.id |
| waiver_amount | DECIMAL(12,2) | NOT NULL |
| reason | TEXT | NOT NULL |
| approved_by | CHAR(36) | FK → members.id |
| approved_at | DATETIME | |
| status | ENUM('pending','approved','rejected') | NOT NULL |
| created_at | DATETIME | NOT NULL |

---

### `expenses`
| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| id | CHAR(36) | PK | |
| tenant_id | CHAR(36) | FK → tenants.id | |
| category | VARCHAR(100) | NOT NULL | |
| description | TEXT | NOT NULL | |
| amount | DECIMAL(12,2) | NOT NULL | |
| currency | CHAR(3) | NOT NULL | |
| fx_rate | DECIMAL(12,6) | DEFAULT 1.000000 | To base currency |
| payment_date | DATE | NOT NULL | |
| vendor | VARCHAR(200) | | |
| receipt_url | VARCHAR(500) | | S3 link |
| event_id | CHAR(36) | FK → events.id, NULL | |
| approved_by | CHAR(36) | FK → members.id | |
| approved_at | DATETIME | | |
| recorded_by | CHAR(36) | FK → members.id, NOT NULL | |
| created_at | DATETIME | NOT NULL | |

---

### `events`
| Column | Type | Constraints |
|--------|------|-------------|
| id | CHAR(36) | PK |
| tenant_id | CHAR(36) | FK → tenants.id |
| title | VARCHAR(300) | NOT NULL |
| description | TEXT | |
| event_type | ENUM('general_assembly','cultural','sports','fundraiser','funeral_support','training','other') | NOT NULL |
| start_datetime | DATETIME | NOT NULL |
| end_datetime | DATETIME | |
| location | VARCHAR(500) | |
| virtual_link | VARCHAR(500) | |
| capacity | INT | NULL = unlimited |
| is_public | BOOLEAN | DEFAULT FALSE |
| cover_image_url | VARCHAR(500) | |
| status | ENUM('draft','published','cancelled','completed') | NOT NULL |
| created_by | CHAR(36) | FK → members.id |
| created_at | DATETIME | NOT NULL |

---

### `event_tickets`
| Column | Type | Constraints |
|--------|------|-------------|
| id | CHAR(36) | PK |
| event_id | CHAR(36) | FK → events.id |
| name | VARCHAR(100) | e.g. "General", "VIP" |
| price | DECIMAL(12,2) | DEFAULT 0.00 |
| quantity | INT | |
| sold | INT | DEFAULT 0 |
| created_at | DATETIME | |

---

### `event_rsvps`
| Column | Type | Constraints |
|--------|------|-------------|
| id | CHAR(36) | PK |
| event_id | CHAR(36) | FK → events.id |
| member_id | CHAR(36) | FK → members.id |
| ticket_id | CHAR(36) | FK → event_tickets.id |
| response | ENUM('attending','maybe','not_attending') | NOT NULL |
| responded_at | DATETIME | NOT NULL |
| checked_in | BOOLEAN | DEFAULT FALSE |
| checked_in_at | DATETIME | |

**Unique constraint:** `(event_id, member_id)`

---

### `welfare_requests`
| Column | Type | Constraints |
|--------|------|-------------|
| id | CHAR(36) | PK |
| tenant_id | CHAR(36) | FK → tenants.id |
| member_id | CHAR(36) | FK → members.id |
| request_type | ENUM('emergency','medical','funeral','educational','repatriation','other') | NOT NULL |
| title | VARCHAR(300) | NOT NULL |
| description | TEXT | NOT NULL |
| amount_requested | DECIMAL(12,2) | NOT NULL |
| amount_approved | DECIMAL(12,2) | |
| amount_disbursed | DECIMAL(12,2) | |
| status | ENUM('pending','under_review','approved','rejected','disbursed') | NOT NULL |
| supporting_docs | JSON | Array of S3 URLs |
| committee_notes | TEXT | |
| approved_by | CHAR(36) | FK → members.id |
| approved_at | DATETIME | |
| disbursed_at | DATETIME | |
| disbursed_by | CHAR(36) | FK → members.id |
| created_at | DATETIME | NOT NULL |

---

### `elections`
| Column | Type | Constraints |
|--------|------|-------------|
| id | CHAR(36) | PK |
| tenant_id | CHAR(36) | FK → tenants.id |
| title | VARCHAR(300) | NOT NULL |
| description | TEXT | |
| nomination_opens | DATETIME | NOT NULL |
| nomination_closes | DATETIME | NOT NULL |
| voting_opens | DATETIME | NOT NULL |
| voting_closes | DATETIME | NOT NULL |
| status | ENUM('draft','nominations_open','voting_open','closed','results_published') | |
| quorum_percentage | DECIMAL(5,2) | DEFAULT 50.00 |
| created_by | CHAR(36) | FK → members.id |
| created_at | DATETIME | NOT NULL |

---

### `election_positions`
| Column | Type | Constraints |
|--------|------|-------------|
| id | CHAR(36) | PK |
| election_id | CHAR(36) | FK → elections.id |
| name | VARCHAR(200) | e.g. "President" |
| max_winners | INT | DEFAULT 1 |

---

### `election_candidates`
| Column | Type | Constraints |
|--------|------|-------------|
| id | CHAR(36) | PK |
| position_id | CHAR(36) | FK → election_positions.id |
| member_id | CHAR(36) | FK → members.id |
| manifesto | TEXT | |
| status | ENUM('nominated','approved','rejected','withdrawn') | |
| approved_by | CHAR(36) | FK → members.id |
| created_at | DATETIME | |

---

### `votes`
| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| id | CHAR(36) | PK | |
| election_id | CHAR(36) | FK → elections.id | |
| position_id | CHAR(36) | FK → election_positions.id | |
| candidate_id | CHAR(36) | FK → election_candidates.id | |
| vote_token | CHAR(64) | UNIQUE | One-way hash of member_id + election_id; anonymizes voter |
| voted_at | DATETIME | NOT NULL | |

---

### `announcements`
| Column | Type | Constraints |
|--------|------|-------------|
| id | CHAR(36) | PK |
| tenant_id | CHAR(36) | FK → tenants.id |
| title | VARCHAR(300) | NOT NULL |
| content | TEXT | NOT NULL |
| is_pinned | BOOLEAN | DEFAULT FALSE |
| audience | ENUM('all','committee','regional','category') | NOT NULL |
| audience_filter | JSON | e.g. {region_id: "..."} |
| published_at | DATETIME | |
| expires_at | DATETIME | |
| created_by | CHAR(36) | FK → members.id |
| created_at | DATETIME | |

---

### `notifications`
| Column | Type | Constraints |
|--------|------|-------------|
| id | CHAR(36) | PK |
| tenant_id | CHAR(36) | FK → tenants.id |
| member_id | CHAR(36) | FK → members.id |
| type | VARCHAR(100) | e.g. "payment_reminder" |
| channel | ENUM('email','sms','push','in_app') | NOT NULL |
| subject | VARCHAR(300) | |
| body | TEXT | NOT NULL |
| status | ENUM('queued','sent','delivered','failed','read') | NOT NULL |
| sent_at | DATETIME | |
| read_at | DATETIME | |
| created_at | DATETIME | NOT NULL |

**Indexes:** `tenant_id`, `member_id`, `status`, `created_at`

---

### `documents`
| Column | Type | Constraints |
|--------|------|-------------|
| id | CHAR(36) | PK |
| tenant_id | CHAR(36) | FK → tenants.id |
| name | VARCHAR(300) | NOT NULL |
| type | ENUM('membership_certificate','receipt','financial_report','minutes','bylaw','letter','other') | |
| file_url | VARCHAR(500) | NOT NULL |
| file_size_bytes | INT | |
| version | INT | DEFAULT 1 |
| access_level | ENUM('public','members','committee','admin') | NOT NULL |
| generated_for | CHAR(36) | FK → members.id, NULL |
| uploaded_by | CHAR(36) | FK → members.id |
| created_at | DATETIME | NOT NULL |

---

### `audit_logs`
| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| id | BIGINT | PK, AUTO_INCREMENT | |
| tenant_id | CHAR(36) | FK → tenants.id | |
| user_id | CHAR(36) | | The user who performed the action |
| action | VARCHAR(100) | NOT NULL | e.g. PAYMENT_CONFIRMED |
| entity_type | VARCHAR(100) | NOT NULL | e.g. payments |
| entity_id | CHAR(36) | NOT NULL | |
| changes | JSON | | {before: {...}, after: {...}} |
| ip_address | VARCHAR(45) | | IPv4 or IPv6 |
| user_agent | VARCHAR(500) | | |
| created_at | DATETIME | NOT NULL | |

**Indexes:** `tenant_id`, `user_id`, `entity_type`, `entity_id`, `created_at`
**Partitioned by:** `created_at` (monthly partitions)

---

### `meetings`
| Column | Type | Constraints |
|--------|------|-------------|
| id | CHAR(36) | PK |
| tenant_id | CHAR(36) | FK → tenants.id |
| title | VARCHAR(300) | NOT NULL |
| meeting_type | ENUM('general_assembly','executive','extraordinary','committee') | NOT NULL |
| meeting_date | DATETIME | NOT NULL |
| location | VARCHAR(500) | |
| virtual_link | VARCHAR(500) | |
| quorum_required | INT | |
| status | ENUM('scheduled','in_progress','completed','cancelled') | NOT NULL |
| minutes | TEXT | |
| minutes_status | ENUM('draft','reviewed','approved','published') | |
| created_by | CHAR(36) | FK → members.id |
| created_at | DATETIME | |

---

### `action_items`
| Column | Type | Constraints |
|--------|------|-------------|
| id | CHAR(36) | PK |
| meeting_id | CHAR(36) | FK → meetings.id |
| description | TEXT | NOT NULL |
| assigned_to | CHAR(36) | FK → members.id |
| due_date | DATE | |
| status | ENUM('open','in_progress','completed','cancelled') | |
| created_at | DATETIME | |

---

### `budget_lines`
| Column | Type | Constraints |
|--------|------|-------------|
| id | CHAR(36) | PK |
| tenant_id | CHAR(36) | FK → tenants.id |
| fiscal_year | INT | NOT NULL |
| category | VARCHAR(200) | NOT NULL |
| type | ENUM('income','expense') | NOT NULL |
| budgeted_amount | DECIMAL(12,2) | NOT NULL |
| actual_amount | DECIMAL(12,2) | DEFAULT 0.00 |
| created_at | DATETIME | |

---

### ERD Summary

```
tenants ──< members >── users
              │
              ├──< member_roles >── roles
              ├──< family_members
              │
              ├──< contribution_charges >── contribution_types
              │         │
              │         └──< payments >── payment_methods
              │         └──< waivers
              │
              ├──< welfare_requests
              ├──< event_rsvps >── events ──< event_tickets
              ├──< votes >── election_candidates >── election_positions >── elections
              ├──< notifications
              ├──< announcements
              └──< documents

tenants ──< expenses
tenants ──< meetings ──< action_items
tenants ──< audit_logs
tenants ──< budget_lines
```

---

# 5. API Design

All endpoints follow: `https://api.communityhub.io/api/v1/`

Tenant context is resolved via JWT claim (`tenant_id`). All responses follow:
```json
{
  "success": true,
  "data": {},
  "meta": { "page": 1, "per_page": 25, "total": 150 },
  "errors": []
}
```

---

## Authentication

| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/auth/register` | Register new user |
| POST | `/auth/login` | Login, returns JWT + refresh token |
| POST | `/auth/refresh` | Refresh access token |
| POST | `/auth/logout` | Invalidate refresh token |
| POST | `/auth/password/reset-request` | Send reset email |
| POST | `/auth/password/reset` | Complete password reset |
| POST | `/auth/mfa/setup` | Generate TOTP secret |
| POST | `/auth/mfa/verify` | Verify TOTP code |

**POST /auth/login — Request:**
```json
{ "email": "member@example.com", "password": "Str0ng!Pass" }
```
**Response:**
```json
{
  "data": {
    "access_token": "eyJ...",
    "token_type": "Bearer",
    "expires_in": 900,
    "user": { "id": "...", "email": "...", "communities": [] }
  }
}
```

---

## Members

| Method | Endpoint | Permission | Description |
|--------|----------|------------|-------------|
| GET | `/members` | Secretary+ | List all members (paginated, filterable) |
| POST | `/members` | Secretary+ | Create member |
| GET | `/members/{id}` | Member (own) / Secretary+ | Get member profile |
| PUT | `/members/{id}` | Member (own profile) / Secretary+ | Update member |
| DELETE | `/members/{id}` | President | Soft-delete |
| POST | `/members/{id}/suspend` | President | Suspend member |
| POST | `/members/{id}/activate` | Secretary+ | Activate member |
| GET | `/members/{id}/payment-history` | Member (own) / Treasurer+ | Payment history |
| GET | `/members/{id}/statement` | Member (own) / Treasurer+ | Contribution statement |
| GET | `/members/{id}/certificate` | Member (own) | Download membership certificate PDF |
| POST | `/members/bulk-import` | Secretary+ | Upload CSV for bulk import |

**GET /members — Query Parameters:**
```
?status=active&category_id=...&chapter_id=...&search=John&page=1&per_page=25
&sort_by=last_name&sort_dir=asc&arrears=true
```

---

## Contributions

| Method | Endpoint | Permission | Description |
|--------|----------|------------|-------------|
| GET | `/contribution-types` | Member+ | List contribution types |
| POST | `/contribution-types` | Treasurer+ | Create contribution type |
| PUT | `/contribution-types/{id}` | Treasurer+ | Update contribution type |
| GET | `/contribution-charges` | Treasurer+ | List all charges |
| GET | `/contribution-charges/my` | Member | Own charges |
| POST | `/contribution-charges/generate` | Treasurer+ | Generate monthly charges |
| POST | `/waivers` | Treasurer+ | Submit waiver |
| PUT | `/waivers/{id}/approve` | Treasurer+ / President | Approve waiver |

---

## Payments

| Method | Endpoint | Permission | Description |
|--------|----------|------------|-------------|
| POST | `/payments` | Member (own) / Treasurer+ | Record payment |
| GET | `/payments` | Treasurer+ | List all payments |
| GET | `/payments/{id}` | Member (own) / Treasurer+ | Get payment detail |
| GET | `/payments/{id}/receipt` | Member (own) / Treasurer+ | Download PDF receipt |
| POST | `/payments/{id}/reverse` | President | Reverse payment |
| POST | `/payments/online-link` | Member | Generate payment link |
| POST | `/payments/webhook/stripe` | System | Stripe webhook |
| POST | `/payments/webhook/flutterwave` | System | Flutterwave webhook |
| POST | `/payments/webhook/mpesa` | System | M-Pesa callback |

**POST /payments — Request:**
```json
{
  "charge_id": "...",
  "amount": 20.00,
  "currency": "EUR",
  "payment_method_id": "...",
  "payment_date": "2026-08-20",
  "notes": "Cash received at August meeting"
}
```

**Response:**
```json
{
  "data": {
    "id": "...",
    "receipt_number": "CMH-2026-00142",
    "status": "confirmed",
    "receipt_url": "https://s3.../receipts/CMH-2026-00142.pdf"
  }
}
```

---

## Financial

| Method | Endpoint | Permission | Description |
|--------|----------|------------|-------------|
| GET | `/financials/dashboard` | President / Treasurer | Treasury dashboard data |
| GET | `/financials/income` | Treasurer+ / Auditor | Income ledger |
| POST | `/financials/expenses` | Treasurer+ | Record expense |
| GET | `/financials/expenses` | Treasurer+ / Auditor | Expense ledger |
| GET | `/financials/budget` | Committee+ | Budget overview |
| POST | `/financials/budget` | Treasurer+ | Define budget |
| GET | `/reports/collection` | Treasurer+ | Collection report |
| GET | `/reports/defaulters` | Treasurer+ | Defaulters list |
| GET | `/reports/financial-statement` | Auditor+ | P&L / Balance sheet |

---

## Events

| Method | Endpoint | Permission | Description |
|--------|----------|------------|-------------|
| GET | `/events` | Member+ | List events |
| POST | `/events` | Event Manager+ | Create event |
| GET | `/events/{id}` | Member+ | Event detail |
| PUT | `/events/{id}` | Event Manager+ | Update event |
| DELETE | `/events/{id}` | Event Manager+ | Cancel event |
| POST | `/events/{id}/rsvp` | Member | RSVP |
| GET | `/events/{id}/attendance` | Event Manager+ | Attendance list |
| POST | `/events/{id}/checkin` | Event Manager+ | QR check-in |
| GET | `/events/{id}/report` | Event Manager+ | Post-event report |

---

## Welfare

| Method | Endpoint | Permission | Description |
|--------|----------|------------|-------------|
| POST | `/welfare/requests` | Member | Submit request |
| GET | `/welfare/requests` | Committee+ | List requests |
| GET | `/welfare/requests/{id}` | Requester / Committee+ | Request detail |
| PUT | `/welfare/requests/{id}/review` | Committee+ | Add review notes |
| PUT | `/welfare/requests/{id}/approve` | Treasurer + President | Approve |
| PUT | `/welfare/requests/{id}/reject` | President | Reject with reason |
| POST | `/welfare/requests/{id}/disburse` | Treasurer | Record disbursement |

---

## Elections

| Method | Endpoint | Permission | Description |
|--------|----------|------------|-------------|
| POST | `/elections` | President | Create election |
| GET | `/elections` | Member+ | List elections |
| GET | `/elections/{id}` | Member+ | Election detail + results |
| POST | `/elections/{id}/nominate` | Member | Self-nominate |
| PUT | `/elections/{id}/candidates/{id}/approve` | President | Approve candidate |
| POST | `/elections/{id}/vote` | Member (eligible) | Cast vote |
| GET | `/elections/{id}/results` | Member+ (after close) | Results |

---

## Analytics

| Method | Endpoint | Permission |
|--------|----------|------------|
| GET | `/analytics/members` | President+ |
| GET | `/analytics/financial-trends` | Treasurer+ |
| GET | `/analytics/events` | Event Manager+ |
| GET | `/analytics/engagement` | President+ |
| GET | `/platform/analytics` | Super Admin |

---

# 6. UI/UX Design

## Design Principles
- Mobile-first responsive design
- Angular 20 standalone components with Signals
- Angular Material 3 component library
- WCAG 2.1 AA accessibility compliance
- Support for French, English, Portuguese, Arabic (RTL)
- Dark/light mode

---

## Application Routes

```
/auth/login
/auth/register
/auth/forgot-password
/auth/mfa

/dashboard

/members
/members/new
/members/:id
/members/:id/edit
/members/:id/statement
/members/import

/contributions
/contributions/new
/contributions/charges
/contributions/charges/:id

/payments
/payments/new
/payments/:id

/finances/dashboard
/finances/income
/finances/expenses
/finances/budget

/reports/collection
/reports/defaulters
/reports/financial-statement

/events
/events/new
/events/:id
/events/:id/attendance

/welfare
/welfare/new
/welfare/:id

/elections
/elections/new
/elections/:id

/meetings
/meetings/new
/meetings/:id

/communications/announcements
/communications/campaigns

/documents

/governance/mandates
/governance/decisions

/analytics

/settings/community
/settings/contribution-types
/settings/payment-methods
/settings/roles
/settings/notifications

/admin/tenants
/admin/subscriptions
/admin/platform-analytics
```

---

## Key Page Specifications

### Treasury Dashboard (`/finances/dashboard`)

**Widgets:**
| Widget | Type | Data |
|--------|------|------|
| Total Balance | Metric card | Sum of all community accounts |
| Monthly Collection | Metric card | This month's confirmed payments |
| Collection Rate | Gauge chart | % of expected contributions received |
| Outstanding Arrears | Metric card | Total owed by defaulters |
| Income vs. Expenses | Bar chart | Last 6 months comparison |
| Contribution by Type | Pie chart | Breakdown by fee type |
| Recent Payments | Data grid | Last 10 payments with status |
| Top Defaulters | Data grid | Members with highest arrears |

**Actions:** Export PDF report, Send reminders to defaulters, Record expense

---

### Member List (`/members`)

**Data Grid Columns:**
| Column | Sortable | Filterable |
|--------|----------|------------|
| Membership # | Yes | No |
| Full Name | Yes | Yes |
| Category | No | Yes |
| Region | No | Yes |
| Status | No | Yes |
| Balance | Yes | Yes |
| Joined Date | Yes | No |
| Actions | — | — |

**Row Actions:** View, Edit, Suspend, Download Statement, Send Payment Reminder
**Bulk Actions:** Export CSV, Send Bulk Email, Generate Statements

---

### Payment Recording (`/payments/new`)

**Form Fields:**
1. Member search (autocomplete)
2. Select charge (filtered to member's open charges)
3. Amount (auto-fills from balance, editable for partial)
4. Payment method (dropdown)
5. Payment date (date picker, defaults today)
6. Reference number (for bank transfers)
7. Notes

**Validation:** Amount > 0, Amount ≤ balance, Payment date not in future. Receipt auto-generated on save.

---

# 7. Reporting Specifications

## R-001: Member List Report
- **Fields:** Membership #, Full Name, Category, Chapter, Status, Join Date, Contact
- **Filters:** Status, Category, Region, Date range
- **Format:** PDF (paginated, landscape) + CSV

## R-002: Paid Members Report
- **Fields:** Name, Period, Amount Paid, Payment Date, Method, Receipt #
- **Filters:** Period (month/year), Payment method
- **Summary:** Total collected, count of payers, average payment

## R-003: Unpaid / Partial Payment Report
- **Fields:** Name, Period, Amount Due, Amount Paid, Balance, Days Overdue
- **Filters:** Period, Months overdue (≥1, ≥2, ≥3, ≥6)

## R-004: Defaulters Report
- **Criteria:** Members with balance > 0 AND ≥ 3 months overdue
- **Fields:** Name, Total Arrears, Oldest Unpaid Period, Last Payment Date
- **Sorted:** By total arrears descending

## R-005: Collection Summary
- **Summary:** Total charged vs. collected vs. outstanding by period
- **Collection rate %:** per month, per category, per region
- **Trend chart:** 12-month sparkline

## R-006: Financial Statement
- **Income Statement:** Revenue by category, Expenses by category, Net surplus/deficit
- **Balance Sheet:** Assets (cash + receivables), Liabilities, Net assets
- **Period:** Fiscal year or custom date range

## R-007: Community Growth Report
- Monthly new members chart
- Churn analysis
- Category distribution over time
- Regional distribution map

## R-008: Event Report
- Per event: Invited, RSVPs, Attended, Attendance rate, Revenue, Expenses, Net
- Summary across events: Annual event calendar view

## R-009: Welfare Report
- Requests by type and status
- Total disbursed by period
- Average processing time
- Fund balance vs. disbursements trend

---

# 8. Workflows

## W-001: New Member Registration

```
[Guest submits application form]
    │
    ▼
System validates email uniqueness + required fields
    │
    ▼
Application status: PENDING
System notifies Secretary by email
    │
    ▼
[Secretary reviews application]
    │
    ├── Reject → Applicant notified with reason
    │
    └── Approve
            │
            ▼
        System assigns membership number
        Status → ACTIVE
        Welcome email sent (with login credentials)
        First contribution charges generated
        Digital membership card generated
```

---

## W-002: Contribution Payment (Online)

```
[Member logs in → sees outstanding balance]
    │
    ▼
Member clicks "Pay Now" on a charge
    │
    ▼
Selects payment method (card/mobile money/bank transfer)
    │
    ▼
[Card / Mobile Money]           [Bank Transfer]
    │                               │
Payment gateway processes       Member downloads
    │                           payment reference
    ▼                               │
Webhook received by API         Treasurer records manually
    │                               │
Payment status → CONFIRMED ◄────────┘
    │
    ▼
Charge status recalculated (partial/paid)
Receipt PDF generated + emailed
Audit log entry created
Member's dashboard updates in real-time
```

---

## W-003: Welfare Assistance Request

```
[Member submits request + supporting documents]
    │
    ▼
System validates eligibility:
  - Member status: ACTIVE
  - Membership duration ≥ 6 months
  - No open request of same type
    │
    ├── Not eligible → Rejected with reason
    │
    └── Eligible → Status: PENDING
            │
            ▼
        Committee notified
        Committee reviews + adds notes
            │
            ▼
        For funeral: → Fast-track to President
        For others: → Full committee vote
            │
            ▼
        Treasurer reviews financial impact
            │
            ▼
        President final approval
            │
            ├── Rejected → Applicant notified with reason
            │
            └── Approved
                    │
                    ▼
                Amount approved recorded
                Treasurer records disbursement
                Welfare fund balance updated
                Member notified of disbursement
```

---

## W-004: Event Attendance (QR Check-in)

```
[Event Manager opens Check-in screen]
    │
    ▼
Member presents digital membership card (QR code)
    │
    ▼
App decodes QR → calls POST /events/{id}/checkin
    │
    ▼
API validates: member is RSVP'd to event
    │
    ├── Already checked in → Alert "Already recorded"
    │
    └── Valid
            │
            ▼
        Attendance record created
        Screen shows member photo + name (confirmation)
        Running attendance count updates
```

---

## W-005: Executive Election

```
[President creates election + positions]
    │
    ▼
Election status → DRAFT
    │
    ▼
Nominations open (members can self-nominate)
    │
    ▼
[Nomination period closes]
    │
    ▼
President reviews and approves/rejects candidates
    │
    ▼
Election status → VOTING_OPEN
All eligible members notified (email + push)
    │
    ▼
[Member votes]
    │
    ▼
System checks eligibility:
  - Status: ACTIVE
  - Dues current (< 3 months arrears)
  - Not already voted (via vote_token check)
    │
    ▼
Vote recorded (anonymized via vote_token)
    │
    ▼
[Voting period closes]
    │
    ▼
System calculates results:
  - Quorum check (if failed → election invalid)
  - Winners determined per position
    │
    ▼
Results published
New executive committee recorded
Mandate start dates set
Community notified
```

---

# 9. Security Design

## RBAC Matrix

| Permission | Super Admin | President | Treasurer | Secretary | Committee | Auditor | Member |
|-----------|-------------|-----------|-----------|-----------|-----------|---------|--------|
| Manage tenants | ✓ | — | — | — | — | — | — |
| Approve members | — | ✓ | — | ✓ | — | — | — |
| View all members | — | ✓ | ✓ | ✓ | Partial | — | Own only |
| Record payments | — | — | ✓ | — | — | — | Own online |
| View financials | — | ✓ | ✓ | — | Summary | ✓ (RO) | — |
| Approve expenses | — | ✓ | ✓ | — | — | — | — |
| Approve welfare | — | ✓ | Partial | — | Review | — | — |
| Manage events | — | ✓ | — | ✓ | ✓ | — | RSVP only |
| Vote | — | ✓ | ✓ | ✓ | ✓ | — | ✓ |
| View audit logs | — | ✓ | — | — | — | ✓ | — |

---

## MFA Policy
- **Required for:** Treasurer, President, Auditor (enforced at login, cannot be disabled)
- **Optional for:** All other roles
- **Methods:** TOTP (Google Authenticator compatible), SMS OTP (fallback)
- **Backup codes:** 8 single-use codes generated at MFA setup

---

## Data Encryption
- **At rest:** AES-256 encryption for PII fields (date_of_birth, phone, address)
- **In transit:** TLS 1.3 minimum, HSTS enforced
- **Payment credentials:** Never stored; gateway tokens only (PCI DSS scope minimized)
- **MFA secrets:** Encrypted with application master key (stored in HSM/Vault)
- **Documents:** S3 server-side encryption (SSE-S3 or SSE-KMS)

---

## Backup Strategy

| Component | Backup Frequency | Retention | Method |
|-----------|-----------------|-----------|--------|
| MySQL database | Every 6 hours | 30 days daily, 12 months monthly | Automated mysqldump + S3 |
| S3 documents | Continuous replication | Permanent (versioned) | S3 Cross-Region Replication |
| Redis cache | Not backed up | N/A | Rebuilt from DB on restart |
| Application config | Daily | 30 days | Git + encrypted secrets backup |

**RTO:** < 4 hours | **RPO:** < 6 hours

---

## GDPR Compliance

| Requirement | Implementation |
|-------------|---------------|
| Right to access | Member can download all their data from self-service portal |
| Right to erasure | President can anonymize a member (replaces PII with placeholders, preserves financial records) |
| Data minimization | Only required fields are mandatory |
| Consent tracking | Communication opt-in/opt-out per channel, logged with timestamp |
| Data portability | Member data export as JSON |
| DPA | Data Processing Agreement generated for each community tenant on signup |
| Data residency | Tenant can select hosting region (EU, US, Africa) |

---

# 10. SaaS Architecture

## Multi-Tenancy Strategy: Shared Database, Isolated Schema

All tenants share one MySQL instance but every table has a `tenant_id` column. Row-level security is enforced at the repository layer — every query is automatically scoped by `tenant_id` derived from the authenticated JWT. No query can cross tenant boundaries. Enterprise customers on the highest tier get a dedicated DB option.

---

## Subscription Tiers

| Feature | Free | Starter | Professional | Enterprise |
|---------|------|---------|-------------|------------|
| Members | 50 | 500 | 5,000 | Unlimited |
| Payment methods | Cash only | + Bank transfer | All methods | All + custom |
| Storage | 1 GB | 10 GB | 100 GB | 1 TB |
| Email campaigns | — | 1,000/month | 10,000/month | Unlimited |
| SMS | — | — | 1,000/month | Unlimited |
| Analytics | Basic | Standard | Advanced | Advanced + AI |
| Custom domain | — | — | ✓ | ✓ |
| API access | — | — | ✓ | ✓ |
| Dedicated DB | — | — | — | ✓ |
| SLA | None | 99% | 99.5% | 99.9% |
| Price/month | $0 | $29 | $99 | Custom |

---

## Billing System
- Stripe Billing for subscription management (plans, trials, invoices)
- Usage-based overages (SMS, storage) billed monthly
- Trial period: 30 days free (all Professional features)
- Annual discount: 20% off monthly rate
- Invoices auto-generated and emailed to community billing email
- Failed payment → 3 retry attempts → Tenant suspended → 14-day grace to reactivate

---

## Feature Flags
- Feature flags per tenant based on subscription tier
- Implemented via in-memory config loaded at tenant resolution time (cached in Redis)
- Flag examples: `enable_mobile_money`, `enable_elections`, `enable_ai_assistant`, `max_members`
- Super Admin can override flags per tenant for sales demos or custom deals

---

# 11. Technical Architecture

## Stack

| Layer | Technology |
|-------|-----------|
| Frontend | Angular 20 (standalone, Signals, SSR optional) |
| UI Library | Angular Material 3 |
| State management | Angular Signals + NgRx SignalStore |
| Backend | ASP.NET Core 9 Minimal API |
| ORM | Entity Framework Core 9 |
| Database | MySQL 8.4 |
| Cache | Redis 7.2 |
| Message Queue | RabbitMQ |
| Object Storage | MinIO (S3-compatible) or AWS S3 |
| Authentication | JWT (RS256) + ASP.NET Core Identity |
| Email delivery | SendGrid / Mailgun |
| SMS | Africa's Talking / Twilio |
| Push | Firebase Cloud Messaging |
| PDF generation | QuestPDF (C# library) |
| Containerization | Docker + Docker Compose (dev) |
| Orchestration | Kubernetes (production) |
| CI/CD | GitHub Actions |
| Monitoring | Prometheus + Grafana |
| Logging | Serilog → Elasticsearch → Kibana |
| Secrets | HashiCorp Vault |

---

## Backend Folder Structure

```
src/
├── CommunityHub.API/
│   ├── Program.cs
│   ├── Endpoints/
│   │   ├── AuthEndpoints.cs
│   │   ├── MemberEndpoints.cs
│   │   ├── PaymentEndpoints.cs
│   │   ├── EventEndpoints.cs
│   │   └── ...
│   ├── Middleware/
│   │   ├── TenantResolutionMiddleware.cs
│   │   ├── AuditLogMiddleware.cs
│   │   └── ExceptionHandlingMiddleware.cs
│   └── appsettings.json
│
├── CommunityHub.Application/
│   ├── Members/
│   │   ├── Commands/
│   │   │   ├── CreateMemberCommand.cs
│   │   │   ├── CreateMemberHandler.cs
│   │   │   └── CreateMemberValidator.cs
│   │   └── Queries/
│   │       ├── GetMembersQuery.cs
│   │       └── GetMembersHandler.cs
│   ├── Payments/
│   ├── Events/
│   ├── Welfare/
│   └── Elections/
│
├── CommunityHub.Domain/
│   ├── Entities/
│   │   ├── Member.cs
│   │   ├── Payment.cs
│   │   ├── ContributionCharge.cs
│   │   └── ...
│   ├── Enums/
│   ├── ValueObjects/
│   │   ├── Money.cs
│   │   └── MembershipNumber.cs
│   ├── Events/
│   └── Exceptions/
│
├── CommunityHub.Infrastructure/
│   ├── Persistence/
│   │   ├── AppDbContext.cs
│   │   ├── Configurations/
│   │   ├── Repositories/
│   │   │   ├── MemberRepository.cs
│   │   │   └── PaymentRepository.cs
│   │   └── Migrations/
│   ├── Services/
│   │   ├── PaymentGateway/
│   │   │   ├── StripeService.cs
│   │   │   └── FlutterwaveService.cs
│   │   ├── EmailService.cs
│   │   ├── SmsService.cs
│   │   ├── StorageService.cs
│   │   └── PdfGenerationService.cs
│   ├── Caching/
│   │   └── RedisCacheService.cs
│   └── BackgroundJobs/
│       ├── ContributionChargeGeneratorJob.cs
│       ├── PaymentReminderJob.cs
│       └── MembershipExpiryAlertJob.cs
│
└── CommunityHub.Contracts/
    ├── Requests/
    └── Responses/
```

---

## Frontend Folder Structure

```
src/
├── app/
│   ├── core/
│   │   ├── auth/
│   │   │   ├── auth.service.ts
│   │   │   ├── auth.guard.ts
│   │   │   └── jwt.interceptor.ts
│   │   ├── tenant/
│   │   │   └── tenant.service.ts
│   │   └── layout/
│   │       ├── shell.component.ts
│   │       ├── sidebar.component.ts
│   │       └── topbar.component.ts
│   │
│   ├── features/
│   │   ├── members/
│   │   │   ├── pages/
│   │   │   ├── components/
│   │   │   ├── services/
│   │   │   └── store/
│   │   ├── payments/
│   │   ├── events/
│   │   ├── welfare/
│   │   ├── elections/
│   │   ├── finances/
│   │   ├── communications/
│   │   └── analytics/
│   │
│   ├── shared/
│   │   ├── components/
│   │   │   ├── data-grid/
│   │   │   ├── metric-card/
│   │   │   ├── status-badge/
│   │   │   ├── confirm-dialog/
│   │   │   └── file-upload/
│   │   ├── pipes/
│   │   │   ├── currency-format.pipe.ts
│   │   │   └── membership-status.pipe.ts
│   │   └── directives/
│   │       └── permission.directive.ts
│   │
│   └── app.routes.ts
```

---

## Clean Architecture Principles

1. **Dependency rule:** Domain ← Application ← Infrastructure ← API
2. **CQRS:** Commands mutate state; Queries read state. MediatR dispatches both.
3. **Repository Pattern:** Interfaces defined in Application layer, implemented in Infrastructure.
4. **Domain Events:** `PaymentConfirmedEvent` triggers `SendReceiptEmailHandler` and `UpdateContributionChargeHandler` independently.
5. **Validation:** FluentValidation validators per command, executed in MediatR pipeline behavior.
6. **Tenant Resolution:** `TenantResolutionMiddleware` reads JWT `tenant_id` claim, sets `ITenantContext` scoped service.

---

## Kubernetes Production Architecture

```
Internet
    │
    ▼
[Cloudflare CDN / WAF]
    │
    ▼
[Nginx Ingress Controller]
    │
    ├── /api/* → [API Service] (3 replicas min, HPA: 3–20 based on CPU)
    │
    └── /* → [Angular Frontend] (static, served from CDN)

[API Service] → [MySQL Primary] → [MySQL Read Replica ×2]
             → [Redis Cluster]
             → [RabbitMQ Cluster]
             → [MinIO / S3]

[RabbitMQ] → [Email Worker] (2 replicas)
           → [SMS Worker] (2 replicas)
           → [PDF Worker] (2 replicas)
           → [Notification Worker] (2 replicas)
```

---

# 12. Advanced Features

## AI-001: Contribution Prediction Engine
Analyzes each member's payment history (on-time rate, seasonality, partial payments) to predict likelihood of payment for the next billing period. Treasurer dashboard shows "Predicted collection rate" alongside actual. Powers targeted reminder intensity. Technology: scikit-learn regression model served via Python FastAPI microservice.

## AI-002: Defaulter Risk Score
Composite score (0–100) per member based on: months in arrears, payment frequency decline, event non-attendance, communication non-response. Score refreshed weekly. Auto-segments members into: Low Risk, Medium Risk, High Risk, Critical. Triggers escalating automated outreach based on risk level.

## AI-003: Community Engagement Score
Tracks per member: events attended / total events, votes cast / total elections, payments on time / total charges, announcements read. Normalized score 0–100. Community-level engagement trend visible to leaders.

## AI-004: Smart Reminders
ML model learns optimal time-of-day and channel per member for reminder delivery. A/B tests reminder message variants automatically. Stops reminders immediately upon payment detection.

## AI-005: Natural Language Community Assistant
Claude API-powered assistant embedded in member portal. Answers: "What do I owe?", "When is the next event?", "How do I request welfare?" Escalates to Secretary for complex questions. Available in French, English, Portuguese.

## DIGITAL-001: QR Code Membership Card
Generated as downloadable PDF and Apple/Google Wallet pass. Contains: name, photo, membership #, category, valid until date, QR code. QR code links to a publicly verifiable page showing only: active/inactive + expiry. Signed with community private key to prevent forgery.

## DIGITAL-002: Mobile Wallet Integration
Apple Wallet and Google Wallet passes for membership cards. Passes auto-update when membership status changes. Supports NFC tap for check-in at events.

## DIGITAL-003: Community Points System (Optional Module)
Members earn points for: on-time payment, event attendance, volunteer work, referrals. Points redeemable for: fee discounts, priority event seating. Creates positive behavioral incentives for engagement and timely payment.

---

# 13. Implementation Roadmap

## Phase 1 — Core Foundation (Months 1–3)
- Authentication, user management, RBAC
- Community and member management
- Basic contribution and payment recording (cash + bank transfer)
- Member self-service portal
- Receipt generation

## Phase 2 — Financial Completeness (Months 4–5)
- Online payment integration (Stripe, Flutterwave, Mobile Money)
- Financial ledger and reporting
- Defaulters tracking and automated reminders
- Welfare module

## Phase 3 — Engagement (Months 6–7)
- Event management with QR check-in
- Election module
- Meeting management
- Communication campaigns (email + SMS)
- Mobile app (Angular PWA first, then native Capacitor)

## Phase 4 — Intelligence (Months 8–10)
- Analytics dashboard
- AI risk scoring
- Engagement scoring
- Digital membership card + wallet integration
- Advanced reporting

## Phase 5 — Scale & Enterprise (Months 11–12)
- Multi-language support
- Dedicated DB option for enterprise
- White-label support
- API access for integrations
- Marketplace for community-specific add-ons

---

*This specification represents the complete blueprint for the CommunityHub SaaS platform.*
*Document owner: Product & Architecture Team | Review cycle: Quarterly*
*Version 1.0 — 2026-08-20*
