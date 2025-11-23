# Frontend Blueprint — Angular

Last updated: 2025-11-20

This document describes the frontend architecture and implementation plan for the NammaOoru app using Angular + TypeScript.

## Goals
- Build a single-page Angular app for Citizens, Officers, and Admins.
- Provide auth (OTP + login), report creation (with photo upload), report listing/detail, officer dashboard (status updates & assignment), and basic admin views.
- Integrate with the existing backend (JWT auth, role checks, REST endpoints).

## Technology choices (recommended)
- Framework: Angular (latest stable, e.g. Angular 17+)
- Language: TypeScript
- UI: Angular Material (preferred for speed & accessibility)
- State: Services + RxJS; add NgRx only if complexity grows
- Testing: Jasmine/Karma (unit), Cypress or Playwright (E2E)

## Folder structure (recommended)

frontend/
- README.md
- package.json
- angular.json
- src/
  - app/
    - core/                # singletons: services, interceptors, guards, models
      - services/
        - auth.service.ts
        - api.service.ts
        - report.service.ts
        - upload.service.ts
      - interceptors/
        - auth.interceptor.ts
        - error.interceptor.ts
      - guards/
        - auth.guard.ts
        - role.guard.ts
      - models/            # DTOs matching backend: ReportResponse, ReportPhotoResponse, UserDto, LoginResponse
    - features/
      - auth/              # login, register, otp-verify
      - reports/           # create, list, detail, photos
      - officer/           # dashboard, assign, status-update
      - admin/             # optional: email queue viewer
    - shared/
      - components/        # ReportCard, PhotoGallery, FileUploader
      - validators/        # file type/size validators
    - app-routing.module.ts
    - app.component.ts
  - assets/
  - environments/
    - environment.ts
    - environment.prod.ts

## Routes (examples)
- `/auth/register` — Register and send OTP
- `/auth/verify` — Verify OTP / receive token
- `/auth/login` — Email/password login
- `/reports/new` — Create report (form + attachments)
- `/reports` — Reports list (paginated & filterable)
- `/reports/:id` — Report detail with photos
- `/officer/dashboard` — Officer-only list and quick actions

## Core services
- AuthService
  - register(), sendOtp(), verifyOtp(), login(), logout()
  - store token in localStorage; expose currentUser BehaviorSubject
- ApiService
  - centralized HttpClient wrapper; base url from environment; handles query params
- UploadService
  - uploadPhoto(reportId, File): uses FormData; returns progress observable
- ReportService
  - createReport(), getReports(), getReportById(), updateStatus(), assignReport()

## HTTP interceptors & guards
- AuthInterceptor: attach Authorization: Bearer <token> to requests
- ErrorInterceptor: global error handling and toast/snackbar notifications
- AuthGuard: protect routes needing authentication
- RoleGuard: ensure user has required role(s)

## File upload flow
1. User selects image files (client validates type & size: JPEG/PNG, <=5MB)
2. Show preview thumbnails and optional captions
3. Create report first (POST /reports) to get an id OR upload immediately and store returned URLs
4. Call POST /reports/{id}/photos with FormData: `{ File, Caption, IsPrimary }` — track progress
5. On success, display uploaded photo(s) in report detail

## UX & accessibility
- Use Angular Material components (forms, dialogs, snackbar)
- Ensure alt text for images and accessible labels
- Responsive layout for mobile & desktop

## Testing strategy
- Unit tests for services and components (Jest or Karma)
- E2E tests (Cypress/Playwright) covering: register→verify→create report→upload photo→officer status update

## Env & configuration
- Use `src/environments/environment.ts` for local `API_BASE_URL` (e.g., `http://localhost:5077`)
- Provide `.env.example` for any build-time variables

## Security & best practices
- Keep JWT in localStorage (simple) or use HttpOnly cookie if you change backend later
- Validate files client-side, but rely on server-side validation as well
- Move secrets to environment/user-secrets for production

## Developer commands (Angular CLI)
- Install CLI (if needed): `npm install -g @angular/cli`
- Create new app (locally): `ng new frontend --routing --style=scss --strict`
- Add Material: `ng add @angular/material`
- Run dev server: `ng serve`

## Minimal acceptance criteria (MVP)
1. Register + OTP verification + token stored and used for authenticated requests
2. Citizen can create a report and upload at least one photo; photo visible on detail
3. Officer can view reports list and update status (status update enqueues email on backend)

## Next steps I can take for you
- Provide starter skeleton files (AuthService, ApiService, UploadService, AuthInterceptor, RoleGuard, basic components/templates)
- Or scaffold the Angular workspace and generate the files directly in `frontend/` if you want me to.

Please tell me whether you want a full scaffold created inside this repo (`frontend/`) or just starter files to paste into your local Angular project.
