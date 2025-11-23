# NammaOoru Project Blueprint

**Project Goal**: A civic problem-reporting platform where citizens report local issues (potholes, broken lights, water leaks, etc.) with photos, and municipal officers resolve them.

---

## 1. High-Level Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                     FRONTEND (React/Vue)                    │
│  - Citizen App: Report creation, status tracking            │
│  - Officer Dashboard: List reports, assign, update status   │
└──────────────────────┬──────────────────────────────────────┘
                       │ HTTP/REST API
┌──────────────────────▼──────────────────────────────────────┐
│         .NET 9 Web API (Kestrel on localhost:5077)          │
│  ┌─────────────────────────────────────────────────────────┐│
│  │ Controllers (AuthController, ReportsController)         ││
│  │  - Handle HTTP requests, routing, authorization         ││
│  └─────────────────────────────────────────────────────────┘│
│  ┌─────────────────────────────────────────────────────────┐│
│  │ Services (AuthService, ReportService, EmailService)     ││
│  │  - Business logic: auth, OTP, reporting, email sending  ││
│  └─────────────────────────────────────────────────────────┘│
│  ┌─────────────────────────────────────────────────────────┐│
│  │ Data Access (ApplicationDbContext + EF Core)            ││
│  │  - Models: User, Report, ReportPhoto, OtpVerification   ││
│  │  - Migrations: Schema changes tracked in version control││
│  └─────────────────────────────────────────────────────────┘│
└──────────────────────┬──────────────────────────────────────┘
                       │ ADO.NET / EF Core
┌──────────────────────▼──────────────────────────────────────┐
│         SQL Server Database (NammaOoruDb on SQLEXPRESS)        │
│  Tables:                                                     │
│  - Users (Id, Email, FirstName, LastName, Role, ...)      │
│  - OtpVerifications (Id, UserId, OtpCode, ExpiresAt, ...) │
│  - Reports (Id, Title, Description, Status, ...,          │
│            CreatedByUserId, AssignedToUserId, ...)         │
│  - ReportPhotos (Id, ReportId, PhotoUrl, ...)             │
└──────────────────────────────────────────────────────────────┘

External Services:
┌─────────────────────────────────────────────────────────────┐
│ Brevo (SMTP Relay) - sends OTP & notification emails       │
│ Local File System (wwwroot/uploads) - stores report photos  │
│ JWT / Bearer Tokens - stateless authentication              │
└─────────────────────────────────────────────────────────────┘
```

---

## 2. Core Data Model (Entities)

### User
```
{
  Id: int (primary key),
  Email: string (unique, email-verified flag tracked),
  FirstName: string,
  LastName: string,
  PasswordHash: string (SHA256),
  IsEmailVerified: bool,
  IsActive: bool,
  Role: string = "Citizen" | "Official" | "Moderator" | "Admin",
  CreatedAt: DateTime,
  UpdatedAt: DateTime?,
  
  // Navigation properties:
  OtpVerifications: List<OtpVerification>,  // OTPs sent to this user
  Reports: List<Report>,                    // Reports created by this user
  AssignedReports: List<Report>             // Reports assigned to this user (officer)
}
```

### OtpVerification
```
{
  Id: int (primary key),
  UserId: int (foreign key → User),
  OtpCode: string (6 digits),
  Email: string,
  ExpiresAt: DateTime (10 minutes),
  IsUsed: bool (true after verification),
  CreatedAt: DateTime,
  
  User: User (navigation)
}
```

### Report
```
{
  Id: int (primary key),
  Title: string,
  Description: string,
  Category: string (e.g., "Pothole", "BrokenLight", "WaterLeak"),
  LocationAddress: string,
  LocationJson: string? (GeoJSON with lat/lng),
  Status: ReportStatus enum (Submitted → InProgress → Resolved → Closed),
  Priority: int (1-5),
  UpvoteCount: int,
  
  CreatedByUserId: int (foreign key → User.Id),
  AssignedToUserId: int? (foreign key → User.Id, officer assigned),
  CreatedAt: DateTime,
  ResolvedAt: DateTime?,
  UpdatedAt: DateTime?,
  
  // Navigation properties:
  CreatedByUser: User,         // Citizen who created report
  AssignedToUser: User,        // Officer assigned to resolve
  Photos: List<ReportPhoto>    // Attached images
}
```

### ReportPhoto
```
{
  Id: int (primary key),
  ReportId: int (foreign key → Report),
  PhotoUrl: string (file path or CDN URL),
  FileName: string,
  ContentType: string (e.g., "image/jpeg"),
  FileSizeInBytes: long,
  Caption: string?,
  UploadedAt: DateTime,
  IsPrimary: bool (first photo shown in list),
  DisplayOrder: int,
  
  // Navigation property:
  Report: Report
}
```

### Enum: ReportStatus
```csharp
public enum ReportStatus
{
  Submitted = 0,    // Citizen just created
  InProgress = 1,   // Officer acknowledged and working on it
  Resolved = 2,     // Officer fixed the issue
  Closed = 3        // Finalized and archived
}
```

---

## 3. API Endpoints (Current & Planned)

### Authentication Endpoints
```
POST /api/auth/register
  Request: { email, password, firstName, lastName }
  Response: { success, message, user: { id, email, firstName, ... } }
  Action: Creates user account, sets role = "Citizen"

POST /api/auth/send-otp
  Request: { email }
  Response: { success, message }
  Action: Generates 6-digit OTP, saves to DB, sends via email

POST /api/auth/verify-otp
  Request: { email, otpCode }
  Response: { success, message, token: "JWT..." }
  Action: Validates OTP, marks as used, returns JWT token

POST /api/auth/login
  Request: { email, password }
  Response: { success, message, token: "JWT..." }
  Action: Email+password login (alternative to OTP)

GET /api/auth/test-auth
  Headers: { Authorization: "Bearer <token>" }
  Response: { message: "You are authenticated!", userId, email }
  Purpose: Test if token is valid (debugging)
```

### Report Endpoints
```
POST /reports
  Headers: { Authorization: "Bearer <token>" }
  Roles: Citizen, Official, Moderator, Admin
  Request: CreateReportRequest {
    title: string,
    description: string,
    category: string,
    source: string (location address),
    attachments: [ { url: string, type: string } ]
  }
  Response: { id: int, message: "Created" }
  Action: Creates report, attaches photos from URLs, saves to DB

GET /reports?skip=0&take=10&status=0&category=Pothole
  Headers: { Authorization: "Bearer <token>" }
  Roles: Citizen, Official, Moderator, Admin
  Query: skip (pagination offset), take (page size), status (filter), category (filter)
  Response: { data: [ {...report}, ... ], total: int, page: int, totalPages: int }
  Action: Officer dashboard — list all reports with filters/pagination

GET /reports/{id}
  Headers: { Authorization: "Bearer <token>" }
  Response: { id, title, description, ..., photos: [ {...}, ... ] }
  Action: Get single report with all photos

POST /reports/{id}/photos  [PLANNED - PHASE 2-A]
  Headers: { Authorization: "Bearer <token>", Content-Type: multipart/form-data }
  Roles: Citizen (own report), Official (any)
  Body: file (binary image), caption (optional)
  Response: { id: int, photoUrl: string, fileName: string }
  Action: Upload image to wwwroot/uploads, create ReportPhoto record

PUT /reports/{id}/status  [PLANNED - PHASE 2-B]
  Headers: { Authorization: "Bearer <token>" }
  Roles: Official, Moderator, Admin
  Request: { status: int (1=InProgress, 2=Resolved, 3=Closed) }
  Response: { success: bool, message: string }
  Action: Officer updates status, records UpdatedBy & UpdatedAt, queues notification

POST /reports/{id}/assign  [PLANNED - PHASE 2-C]
  Headers: { Authorization: "Bearer <token>" }
  Roles: Official, Moderator, Admin
  Request: { assignToUserId: int }
  Response: { success: bool, message: string }
  Action: Assign report to another officer, queue notification
```

---

## 4. User Journey (How It Works)

### Citizen (Reporter)
```
1. Opens frontend app → sees login/signup
2. Clicks "Register" → fills in name, email, password
3. Backend POST /api/auth/register → user created with role="Citizen"
4. Backend sends OTP to email (background task)
5. Citizen receives email with 6-digit code
6. Enters OTP in app
7. Frontend calls POST /api/auth/verify-otp
8. Backend validates OTP, returns JWT token
9. Frontend stores JWT in localStorage/cookie
10. Citizen is logged in and sees "Report Problem" form
11. Fills: Title ("Pothole on Main St"), Description, Category (Pothole), Location
12. Optionally attaches photos (or uploads post-creation)
13. Clicks "Submit Report"
14. Frontend calls POST /reports with JWT header
15. Backend creates Report with CreatedByUserId = citizen's ID, Status = Submitted
16. Report saved to DB
17. Citizen sees confirmation: "Report #123 submitted"
18. Citizen can view GET /reports (their reports) and monitor status changes

Later:
19. Officer updates report status → notification email sent to citizen
20. Citizen sees status changed in app (Submitted → InProgress → Resolved → Closed)
```

### Officer/Moderator/Admin
```
1. Registers or is given account with role="Official" (admin creates manually or via seed)
2. Logs in with email + password or OTP
3. Frontend calls POST /api/auth/login or POST /api/auth/verify-otp
4. Backend returns JWT token with role claim
5. Officer is logged in and sees Officer Dashboard
6. Dashboard calls GET /reports?skip=0&take=10
7. Officer sees list of all reports (paged, filterable)
8. Clicks on a report → calls GET /reports/{id}
9. Sees citizen's report with title, description, photos, location
10. Clicks "Take Action" → options appear:
    - Assign to another officer
    - Update status (InProgress → Resolved → Closed)
    - Add comment/internal notes (future)
11. Officer clicks "Mark In Progress"
12. Frontend calls PUT /reports/{id}/status with { status: 1 }
13. Backend updates Report.Status = InProgress, UpdatedByUserId = officer's ID
14. Backend queues notification email to citizen
15. Citizen receives email: "Your report #123 is now being worked on"
16. Officer works on the issue (e.g., fixes pothole)
17. Officer returns to app → clicks "Mark Resolved"
18. Frontend calls PUT /reports/{id}/status with { status: 2 }
19. Backend updates and sends "Your report is resolved" email
20. Citizen sees status: Resolved
21. After some time, report auto-closes or admin manually closes it
```

---

## 5. Authentication & Authorization Flow

```
Unauthenticated Request:
  GET /reports → 401 Unauthorized (no Authorization header)

Authenticated Request:
  GET /reports
  Headers: Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
  
  Backend JWT validation middleware:
    1. Extract token from "Authorization: Bearer <token>"
    2. Verify signature using Jwt:SecretKey (HMAC-SHA256)
    3. Check expiry (24 hours from issue time)
    4. Extract claims: id, email, firstName, lastName, role
    5. Create ClaimsPrincipal for this request
    6. Attach to HttpContext.User
  
  Controller action:
    [Authorize(Roles = "Citizen,Official,Moderator,Admin")]
    public async Task<IActionResult> GetReports(...)
    {
      // If user's role claim is in the allowed list, request proceeds
      // Otherwise: 403 Forbidden
    }

Token Content (JWT Payload, decoded):
  {
    "id": "42",
    "email": "citizen@example.com",
    "firstName": "John",
    "lastName": "Doe",
    "role": "Citizen",
    "iat": 1700400000,
    "exp": 1700486400  // 24 hours later
  }
```

---

## 6. Email Sending Flow (Current & Future)

### Current (Background Task)
```
1. User calls POST /api/auth/send-otp with email
2. AuthService.SendOtpAsync is called
3. OTP created and saved to DB
4. Task.Run(async () => { await EmailService.SendOtpEmailAsync(...) })
   - Non-blocking: request returns immediately
   - Background thread attempts to send
5. EmailService connects to Brevo SMTP
6. MailKit authenticates using SenderEmail + SenderPassword
7. Message sent via SMTP MAIL FROM, RCPT TO, DATA
8. If success: logged as "OTP email sent to {email}"
9. If error: logged as error but not re-tried (lost if network fails)
```

### Planned (Phase 2-D: Queue + Retry)
```
1. User calls POST /api/auth/send-otp
2. OTP created and saved to DB
3. EmailQueueItem record created (table: EmailQueue)
   { Id, RecipientEmail, Subject, Body, Attempts=0, NextRetry=now, Status="Pending" }
4. Request returns immediately
5. HostedBackgroundService runs continuously
6. Service checks DB for pending emails every 30 seconds
7. For each pending item:
   a. If Attempts < 3:
      - Try to send via EmailService
      - If success: Status="Sent"
      - If fail: Attempts++, NextRetry=now+exponential_backoff
   b. If Attempts >= 3: Status="Failed" (manual review needed)
8. Retries with delays:
   - 1st attempt: immediately
   - 2nd attempt: 5 minutes later
   - 3rd attempt: 15 minutes later
   - If still fails: mark as failed for manual intervention
9. All attempts logged with timestamps and error details
```

---

## 7. Database Schema (SQL Server)

```sql
-- Users Table
CREATE TABLE [Users] (
  [Id] INT PRIMARY KEY IDENTITY(1,1),
  [Email] NVARCHAR(255) NOT NULL UNIQUE,
  [FirstName] NVARCHAR(100) NOT NULL,
  [LastName] NVARCHAR(100) NOT NULL,
  [PasswordHash] NVARCHAR(MAX) NOT NULL,
  [IsEmailVerified] BIT NOT NULL DEFAULT 0,
  [IsActive] BIT NOT NULL DEFAULT 1,
  [Role] NVARCHAR(MAX) NOT NULL DEFAULT 'Citizen',
  [CreatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
  [UpdatedAt] DATETIME2 NULL
);

-- OtpVerifications Table
CREATE TABLE [OtpVerifications] (
  [Id] INT PRIMARY KEY IDENTITY(1,1),
  [UserId] INT NOT NULL,
  [OtpCode] NVARCHAR(6) NOT NULL,
  [ExpiresAt] DATETIME2 NOT NULL,
  [IsUsed] BIT NOT NULL DEFAULT 0,
  [Email] NVARCHAR(255) NOT NULL,
  [CreatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
  CONSTRAINT [FK_OtpVerifications_Users] FOREIGN KEY ([UserId]) 
    REFERENCES [Users]([Id]) ON DELETE CASCADE
);
CREATE INDEX [IX_OtpVerifications_UserId] ON [OtpVerifications]([UserId]);

-- Reports Table
CREATE TABLE [Reports] (
  [Id] INT PRIMARY KEY IDENTITY(1,1),
  [Title] NVARCHAR(MAX) NOT NULL,
  [Description] NVARCHAR(MAX) NOT NULL,
  [Category] NVARCHAR(100),
  [LocationAddress] NVARCHAR(MAX),
  [LocationJson] NVARCHAR(MAX),
  [Status] INT NOT NULL DEFAULT 0,  -- 0=Submitted, 1=InProgress, 2=Resolved, 3=Closed
  [Priority] INT DEFAULT 2,
  [UpvoteCount] INT DEFAULT 0,
  [CreatedByUserId] INT NOT NULL,
  [AssignedToUserId] INT NULL,
  [CreatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
  [ResolvedAt] DATETIME2 NULL,
  [UpdatedAt] DATETIME2 NULL,
  CONSTRAINT [FK_Reports_Users_CreatedBy] FOREIGN KEY ([CreatedByUserId]) 
    REFERENCES [Users]([Id]),
  CONSTRAINT [FK_Reports_Users_AssignedTo] FOREIGN KEY ([AssignedToUserId]) 
    REFERENCES [Users]([Id])
);
CREATE INDEX [IX_Reports_CreatedByUserId] ON [Reports]([CreatedByUserId]);
CREATE INDEX [IX_Reports_AssignedToUserId] ON [Reports]([AssignedToUserId]);
CREATE INDEX [IX_Reports_Status] ON [Reports]([Status]);

-- ReportPhotos Table
CREATE TABLE [ReportPhotos] (
  [Id] INT PRIMARY KEY IDENTITY(1,1),
  [ReportId] INT NOT NULL,
  [PhotoUrl] NVARCHAR(MAX),
  [FileName] NVARCHAR(MAX),
  [ContentType] NVARCHAR(100),
  [FileSizeInBytes] BIGINT,
  [Caption] NVARCHAR(MAX),
  [UploadedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
  [IsPrimary] BIT DEFAULT 0,
  [DisplayOrder] INT DEFAULT 0,
  CONSTRAINT [FK_ReportPhotos_Reports] FOREIGN KEY ([ReportId]) 
    REFERENCES [Reports]([Id]) ON DELETE CASCADE
);
CREATE INDEX [IX_ReportPhotos_ReportId] ON [ReportPhotos]([ReportId]);
```

---

## 8. Configuration & Secrets

### appsettings.json (tracked in repo, contains non-sensitive defaults)
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=LTIN647429\\SQLEXPRESS;Initial Catalog=NammaOoruDb;Integrated Security=True;Encrypt=True;TrustServerCertificate=True;"
  },
  "Jwt": {
    "SecretKey": "ThisIsAVeryLongSecretKeyThatShouldBeAtLeast32CharactersLongForHS256",
    "Issuer": "MoodlyAPI",
    "Audience": "MoodlyAppUsers",
    "ExpiryMinutes": 1440
  },
  "EmailSettings": {
    "SmtpServer": "smtp-relay.brevo.com",
    "SmtpPort": 587,
    "SenderEmail": "9b93cb001@smtp-brevo.com",
    "SenderPassword": "xsmtpsib-106294097d0287e17b2ba77eba7cd182ba13e9110821ce120fb43272b4baff33-3NlI2oUZPDlmrN3W"
  }
}
```

### What Should Be in Secrets (NOT in repo)
```
In production or via user-secrets / environment variables:
  - Jwt:SecretKey (rotate regularly)
  - EmailSettings:SenderPassword (API key for Brevo)
  - ConnectionStrings:DefaultConnection (prod DB server address)
```

---

## 9. Code Organization (Folder Structure)

```
NammaOoru/
├── Controllers/
│   ├── AuthController.cs          -- Endpoints for register, OTP, login
│   └── ReportsController.cs        -- Endpoints for reports (GET, POST, PUT)
├── Services/
│   ├── AuthService.cs             -- Login logic, JWT generation, OTP verification
│   ├── EmailService.cs            -- SMTP sending via MailKit
│   ├── OtpService.cs              -- OTP generation and validation
│   └── ReportService.cs           -- Report queries, pagination, filtering, mapping to DTOs
├── Entities/
│   ├── User.cs                    -- User model
│   ├── OtpVerification.cs         -- OTP model
│   ├── Report.cs                  -- Report model
│   └── ReportPhoto.cs             -- ReportPhoto model
├── Data/
│   └── ApplicationDbContext.cs     -- EF Core DbContext, model configurations
├── Models/
│   ├── CreateReportRequest.cs      -- DTO for creating a report (input)
│   ├── ReportResponse.cs           -- DTO for returning report (output)
│   ├── ReportPhotoResponse.cs      -- DTO for photos
│   ├── PagedResponse<T>.cs         -- Generic paged response wrapper
│   └── AuthModels.cs               -- LoginRequest, RegisterRequest, etc.
├── Migrations/
│   ├── 20251113000000_InitialCreate.cs
│   ├── 20251117095458_AddReportsAndPhotos.cs
│   └── 20251119025908_AddUserRole.cs
├── Properties/
│   └── launchSettings.json         -- Debug launch profile
├── appsettings.json                -- Configuration (non-secrets)
├── appsettings.Development.json    -- Dev-specific overrides
├── Program.cs                      -- Startup: DI, middleware, routing
└── NammaOoru.csproj                -- Project file: packages, target framework
```

---

## 10. Current Status & Phase 2 Implementation Plan

### ✅ Completed
- User registration and email + OTP authentication
- JWT token issuance with role claim
- Report creation (POST /reports)
- Report listing with pagination & filtering (GET /reports)
- Report detail retrieval (GET /reports/{id})
- Swagger UI / OpenAPI documentation
- Database schema with migrations
- Role-based authorization on endpoints

### 🚧 In Progress (Phase 2)
- **Phase 2-A**: Secure photo upload (POST /reports/{id}/photos)
  - Accept multipart/form-data
  - Validate file type (JPEG, PNG only) and size (max 5MB)
  - Store to wwwroot/uploads/
  - Create ReportPhoto record
  - Return secure URL
  
- **Phase 2-B**: Report status update (PUT /reports/{id}/status)
  - Officer changes status (Submitted → InProgress → Resolved → Closed)
  - Audit trail (UpdatedBy, UpdatedAt)
  - Queue notification email
  
- **Phase 2-C**: Report assignment (POST /reports/{id}/assign)
  - Officer assigns report to another officer
  - Queue notification email
  
- **Phase 2-D**: Email queue & retry
  - Background HostedService
  - Retry with exponential backoff (3 attempts, 5m/15m/1h delays)
  - Prevent email loss

### 📋 Future (Phase 3+)
- Unit and integration tests
- Rate-limiting for OTP endpoints
- Structured logging and ProblemDetails error handling
- Move secrets to KeyVault / environment variables
- CI/CD pipeline (GitHub Actions)
- Staging and production deployment
- Comments/internal notes on reports
- Status history tracking
- Admin dashboards and analytics
- Mobile app support

---

## 11. How to Run Locally

### Prerequisites
- .NET 9.0 SDK
- SQL Server (LocalDB or full edition)
- Git

### Steps
```bash
# 1. Clone repo
git clone https://github.com/muralikatta12/NammaOoru.git
cd NammaOoru

# 2. Update database
dotnet ef database update

# 3. Run app
dotnet run

# 4. Open Swagger UI
Open browser: http://localhost:5077
```

### Test API Flows
```bash
# 1. Register
POST http://localhost:5077/api/auth/register
Body: { "email": "test@example.com", "password": "pass123", "firstName": "Test", "lastName": "User" }

# 2. Send OTP
POST http://localhost:5077/api/auth/send-otp
Body: { "email": "test@example.com" }
# Check email or DB for OTP code

# 3. Verify OTP
POST http://localhost:5077/api/auth/verify-otp
Body: { "email": "test@example.com", "otpCode": "123456" }
# Response includes JWT token

# 4. Create Report (use JWT from step 3)
POST http://localhost:5077/reports
Headers: Authorization: Bearer <token>
Body: { "title": "Pothole", "description": "Large hole on Main St", "category": "Pothole", "source": "Main Street", "attachments": [] }

# 5. List Reports
GET http://localhost:5077/reports?skip=0&take=10
Headers: Authorization: Bearer <token>
```

---

## 12. Next Steps

**For Backend (Week 1-2)**:
1. Complete Phase 2-A (photo upload) ← START HERE
2. Complete Phase 2-B (status update)
3. Complete Phase 2-C (assignment)
4. Complete Phase 2-D (email retry)
5. Test end-to-end flows

**For Frontend (Week 2-3)**:
1. Build login/register UI
2. Build report creation form with photo upload
3. Build officer dashboard with list/detail views
4. Build status update UI
5. Integrate with backend APIs

**For DevOps (Week 3+)**:
1. Set up CI/CD pipeline
2. Deploy to staging
3. Load testing
4. Production deployment

---

## 13. Key Design Decisions

| Decision | Why |
|----------|-----|
| JWT tokens (not sessions) | Stateless, scalable, supports mobile apps |
| Email sending in background | Don't block API response on slow SMTP |
| Role-based authorization on endpoints | Granular control over who can do what |
| DTOs for API responses | Decouple API contract from DB schema |
| Pagination on report listing | Handle large datasets efficiently |
| EF Core migrations | Version control for schema changes |
| Local file storage (phase 1) | Simple, no cloud setup needed; move to blob storage later |

---

## 14. Glossary

| Term | Meaning |
|------|---------|
| Citizen | End user who reports problems |
| Officer / Official | Municipal staff who resolve reports |
| OTP | One-Time Password sent via email for verification |
| JWT | JSON Web Token; signed credential for authenticated requests |
| Bearer Token | JWT included in Authorization header as "Bearer <token>" |
| DTO | Data Transfer Object; lightweight JSON sent over API |
| EF Core | Entity Framework; .NET ORM for database operations |
| Migration | SQL schema change tracked in version control |
| SMTP | Simple Mail Transfer Protocol; used to send emails |
| Brevo | Email/SMS service provider (formerly Sendinblue) |
| wwwroot | ASP.NET folder for static files (images, CSS, JS) |

---

**Last Updated**: November 19, 2025
**Status**: MVP Backend Nearing Completion (Phase 2 In Progress)

