# Friends with Fur current-state audit

Audit date: 2026-10-13. Baseline: `8d9b7ad83bc6e87caf4c2231a9e1e071f2a26424` on main. Scope: all three projects, all source/configuration/XAML/platform files, solution files, resources, tracked generated metadata and IDE-file inventory. Binary fonts and IDE databases were inventoried, not interpreted as application logic. No AGENTS.md, tests, CI workflows, migrations, model snapshot, container definition, global.json, package lock, or gitignore were found. No production database or running deployment was supplied. Documentation describes observed source separately from proposals; source, tests and dependency declarations remain unchanged.

## Architecture and inventory

`DogWalkerEntrepreneur.sln` contains net8.0 `DogWalker.Core`, net8.0 `DogWalkerApi`, and `DogWalkerApp` targeting Android/iOS/MacCatalyst plus conditional Windows. Core shares mutable persistence models and record contracts; API uses controllers → scoped service interfaces → EF DbContext, AutoMapper, FluentValidation, Serilog, Swagger and SignalR. MAUI uses CommunityToolkit MVVM, XAML, Refit and DI. There is no separate domain-policy or identity layer.

All entities are in `DogWalker.Core/Models/`:

| Model | Existing role/relationships | Gap |
|---|---|---|
| BaseEntity | Guid Id; DateTime CreatedUtc/UpdatedUtc | No automatic update auditing or concurrency token |
| Address | Owned Client.Address and Walker.ServiceAreaCenter; nullable coordinates | No normalized household/geocoding policy |
| Client | Contact/address; lists Dogs, Bookings, MessageThreads; activity/spend metadata | Not an authenticated account; no tenant isolation |
| Dog | ClientId; name/breed/birth/weight/special needs; muzzle/reactivity/vet contact | No other species; VetContact absent from create/DTO |
| Walker | Contact, bio, center/radius, AssignedBookings, GPS flag | No onboarding, qualifications, availability, verification or payout account |
| Booking | ClientId, WalkerId, one DogId; times/service/status; Payment, RoutePoints, Media, Messages | No multi-pet join; no eligibility or transition guards |
| Payment | BookingId, Stripe IDs, amount/tip/currency/status/timestamps | No fee ledger, webhook events, Connect or idempotency |
| MessageThread | ClientId, WalkerId, optional BookingId; Messages | No membership enforcement/uniqueness |
| Message | ThreadId, SenderId, direction/body/read/delivery/time | SenderId never assigned by MessageService |
| WalkRoutePoint | BookingId; position/time/distance/speed/source | No validation; GPS client reports zero distance |
| MediaAsset | BookingId; URL/type/caption/time/shared flag | Metadata only, no upload/storage authorization |

DTOs: `BookingDto`, `ClientDto`, `DogDto`, `MessageDto`, `PaymentDto` in `DogWalker.Core/DTOs/`. Requests: `CreateBookingRequest`, `CreateClientRequest`, `CreateDogRequest`, `MediaUploadRequest`, `RoutePointRequest`, `SendMessageRequest`, `UpdateBookingStatusRequest`; additionally API `PaymentRequest` and mobile `CreatePaymentIntent` both carry amount/tip. No walker, availability, species, qualification, task or report DTOs exist. Interfaces: IBookingService, IClientService, IDogService, IMessageService, IPaymentService. IMessageService references SendMessageRequest without importing its namespace: a compile defect to verify.

Enums in `DogWalker.Core/Enums/`: ServiceType has six fixed services (two walk lengths, DogSitting, PuppyVisit, GroupWalk, Boarding); BookingStatus has Draft/PendingConfirmation/Confirmed/InProgress/Completed/Cancelled/Refunded; PaymentStatus has Pending/Authorized/Captured/Refunded/Failed; MessageDirection and EnvironmentTier. Preserve stored enum numeric values. `Class1.cs` is unused scaffold. `Configuration/AppEnvironmentOptions.cs` includes secret-bearing fields inappropriate for shared mobile configuration.

`DogWalkerApi/Data/DogWalkerDbContext.cs` exposes nine sets and owned addresses. Explicit cascading exists for booking routes/media and thread messages. Other relationships depend on EF conventions; scalar DogId alone does not establish a dog FK. Client/Walker collection relationships may infer FKs; generated SQL/model must be inspected after compile repair. SQL Server multiple-cascade-path behavior is untested. No database initialization/migration execution occurs in Program.

## Backend findings

`Program.cs` registers five scoped services and two validators. Blank SQL configuration selects UseInMemoryDatabase, but `DogWalkerApi.csproj` does not reference its provider. Development configuration is blank; base configuration contains a committed sample SQL password and TrustServerCertificate. Treat that password as compromised if ever used; do not reuse it. No verified operational secrets were found in runtime variable presence checks. Placeholder Stripe/SendGrid/maps strings are not functioning integrations.

| Source | Implemented path | Completeness/security |
|---|---|---|
| Controllers/ClientsController.cs; Services/ClientService.cs | List/get/create clients, append dog | Real EF operations; not full CRUD, no auth/ownership; exception→500 on missing owner |
| Controllers/DogsController.cs; Services/DogService.cs | List all dogs/by client | Real queries; global data exposure |
| Services/BookingService.cs CreateAsync | Persist request; server calculates fixed price | No FK existence/owner/qualification/availability checks; double money; unknown enum falls back to $25 |
| BookingService.UpdateStatusAsync | Arbitrary status assignment | No transition/actor/concurrency control |
| Controllers/BookingsController.cs UpdateStatus | Save status, broadcast, then capture if Completed | Failure leaves completed booking despite unsuccessful payment; ignores AutoChargeEnabled |
| Controllers/BookingsController.cs route/media | Append position/URL metadata | No size, coordinate, URL, ownership or task validation |
| Services/PaymentService.cs | Stripe intent create/update/capture/refund methods | Caller amount trusted; no Connect, webhook, keys/idempotency; no manual-capture setting, payment UI/client secret path absent |
| Services/MessageService.cs | Fetch and persist messages | Caller chooses identities/direction; no SignalR MessageReceived broadcast; thread creation race |
| Hubs/WalkHub.cs | Join arbitrary booking group; route/status broadcasts | Anonymous group joins and spoofable broadcasts |
| Mapping/MauiDomainProfile.cs | Five mappings; client address/count mapping | Mapping behavior untested; no AssertConfigurationIsValid checks |
| Validators/*.cs | Booking IDs/time; client required contact fields | No dog/payment/media/message/status validators; timestamp minimum fixed at validator construction |

JWT middleware is a placeholder: issuer/audience/signing-key validation flags disabled, no authority/signing keys configured, HTTPS metadata disabled. No `[Authorize]` or fallback policy protects endpoints/hub. This is not usable login or authorization. CORS permits all origins/methods/headers. No rate limits, centralized ProblemDetails, audit log, admin support, job worker/outbox or health endpoints.

## Mobile findings

`AppShell.xaml` exposes Dashboard, Bookings, Clients, Live Walk, Messages, oriented to a walker business. Each page resolves a ViewModel through `Helpers/ServiceHelper.cs` and most load in OnAppearing. DI registrations in `MauiProgram.cs` are reusable, but the service locator can fail before the MAUI handler exists (device validation needed). Refit URL is hardcoded localhost:5001; packaged options do not configure it. API launch profile uses 7237/5119, so defaults disagree; device localhost is not the API host.

- `BookingsViewModel.LoadDataAsync` fetches real dogs globally, not an owner-scoped list. README's Goldie placeholder description is stale. History actually fetches only 20 upcoming/recent walker bookings, not historical owner bookings. A single dog picker exists. Named request arguments use lower-case names although record parameters are PascalCase: static compile concern. Time zone offset uses today's offset rather than the selected date; all non-30-minute services last one hour.
- `DashboardViewModel.QuickBookAsync` invents client/dog IDs and uses a fixed walker. Prices displayed differ from backend sitting/boarding prices.
- `ClientsViewModel.LoadAsync` fetches real clients; CreateClientAsync persists fabricated contact data rather than a form. Empty state exists, but checks all Clients rather than search results.
- `WalkTrackerViewModel.BeginWalkAsync` invents a booking ID. Photo capture sends a device path rather than bytes. Completion calls API on that fabricated booking. `GpsTrackerService` fire-and-forgets a loop; no Android foreground service/iOS background mode, robust cancellation, unit conversion or distance computation. Location.Speed is passed as SpeedMph without conversion.
- `MessagesViewModel` uses fixed identities, joins Guid.Empty and listens for an event the server never sends. REST history/send logic exists; real-time end-to-end messaging is incomplete.
- `BaseViewModel.SafeExecuteAsync` provides busy/error alerts; errors expose raw exception text. CanExecute refresh on IsBusy is absent from BaseViewModel, so buttons may remain stale. XAML provides indicators, fonts, colors and reusable cards; accessibility labels and comprehensive empty/error/retry states are absent. Dashboard Shadow="Medium" requires MAUI compile validation.
- SecureStorageService wraps MAUI storage but no login/token refresh, authorization header or offline data store uses it. No payment, pet-edit, caregiver-discovery, account-registration, care-report, earnings, admin or notification screens exist.

## Functional completeness methodology

Estimate **12.5%, rounded to 13%, of the requested marketplace MVP structurally complete**. This is planning credit, not verified working behavior. Sixteen equally weighted capability areas, scored 0 absent, .25 scaffold, .5 partial meaningful implementation, .75 integrated but incomplete, 1 tested end-to-end. Customer identity 0; caregiver identity 0; species profiles .25 (dogs only); multi-pet 0; eligibility 0; availability 0; catalog .5; booking .5; authoritative pricing .25 (booking price server-derived but payment bypass); sandbox payment .25; messaging .25; checklists 0; reports 0; owner history 0; essential admin/safety 0; deployment/test readiness 0. Sum 2/16. Changing weights changes the number; no mobile or payment success claim follows from this score. Proven marketplace end-to-end scenarios: zero.

## Reuse and complexity

Retain stack, project split, MVVM approach, Refit contract pattern, cancellation-token usage, model IDs, useful owned-address pattern, fonts/colors/converters and media-capture wrapper where appropriate. None is certified unchanged-and-working without native validation. Adapt clients/dogs/bookings/messages/DTOs/controllers/EF mappings and role navigation incrementally. Replace placeholder JWT configuration, fabricated identity flows, client-priced payment orchestration, arbitrary state updates and local-path upload mechanism. Reuse GPS only for dog walking after permissions/reliability repair; defer live tracking from MVP.

Conversion is high complexity because commerce/identity/availability are missing foundations, not because pet fields are difficult. No deployment should precede ownership enforcement, database migration rehearsal, payment reconciliation and native builds. Generated obj and .vs files are tracked; preserve now and propose cleanup separately. .NET 8 and MAUI 8 lifecycle/store compatibility require a supported-runtime upgrade assessment before launch; audit uses the original target without source changes.

## Validation evidence

See the appended validation results below. There is no existing executable test suite; zero tests is not a pass. Database, mobile-device, Stripe, SendGrid, notifications and storage integrations remain unverified. README's turnkey/production-ready claims are unsupported by repository evidence.

### Executed results

- Git read access: `git ls-remote origin HEAD` passed; matched baseline HEAD. Initial checkout clean; final only eight new docs, no tracked modifications.
- Installed SDK 8.0.425 to `/workspace/.dotnet`, SHA-512 checked against Microsoft HTTPS release metadata. Channel installer tried unavailable `ci.dot.net` (403); explicit version on `builds.dotnet.microsoft.com` succeeded. CLI initialization initially failed on read-only home; corrected with DOTNET_CLI_HOME and NUGET_PACKAGES under /workspace. No TLS/signature checks disabled.
- Fresh restore of unchanged DogWalker.Core/API in `/workspace/fwf-validation` passed with NU1603: declared SendGrid 9.29.0 unavailable; NuGet selected 9.29.1. No lockfile exists; this is not frozen dependency reproduction.
- Unchanged API build FAILED: CS0246 at `DogWalker.Core/Abstractions/IMessageService.cs:8`, missing SendMessageRequest import.
- Diagnostic build with an external MSBuild Using item (no source edits) compiled Core, then FAILED: CS1061 at `DogWalkerApi/Program.cs:44` (missing EF InMemory provider) and CS0019 at `DogWalkerApi/Services/PaymentService.cs:92` (Stripe intent.Created is DateTime, code treats it as nullable Unix seconds). Diagnostic Core success does not mean unchanged Core passed. These are confirmed repository defects.
- Android-targeted build attempt FAILED NETSDK1147: android/maui-android workloads absent. TargetFrameworks override propagated to Core in this diagnostic attempt; future native build should isolate target settings properly. No costly workload installation attempted after confirmed shared-code compile failure. Android device/build and iOS Mac/Xcode/signing remain blocked/unrun. Native named-argument, JSON enum deserialization (Tier string without enum converter), XAML Shadow conversion and service-locator lifecycle findings remain static concerns pending native tests.
- Resource existence check passed for referenced app icon/foreground/splash SVGs; do not classify filenames as missing.
- Existing test projects: none. Tests executed/passed/failed: 0/0/0; no functional-test pass claimed. API startup and representative REST requests blocked by confirmed compile defects; SQL migrations/constraints, SignalR sessions, Stripe sandbox, media/notifications and native flows unrun.

### Environment status

The SDK and API dependency cache support audit/build diagnosis. Application development runtime is **not ready** because source repairs require the owner's audit approval. Saved reusable install/start instructions must preserve tracked generated files, use existing checkout, and retain these blockers. No live services were started and no new-task restoration or publication was verified.

Reusable configuration: install_script and start_skill saved successfully as a draft. Repeat execution of the saved install script passed Core/API restore with the same SendGrid warning. Saved startup instructions explicitly report runtime compile blockers; no application startup is claimed. Review/save in environment settings and publication remain user actions; neither has been verified. No network/secret requirements added: current package/Git access sufficed, and live provider credentials are not needed for this approval-stage audit.
