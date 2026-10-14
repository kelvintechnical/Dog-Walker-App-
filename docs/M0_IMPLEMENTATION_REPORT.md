# M0 implementation report

Initial implementation: 2026-10-13; preservation review: 2026-10-14. Dedicated branch: `codex/m0-auth-ownership`, based on `8d9b7ad83bc6e87caf4c2231a9e1e071f2a26424`. No commit/push was performed during the initial implementation. The owner subsequently authorized preservation by committing/pushing this branch on 2026-10-14. No main-branch merge, production database change, infrastructure deployment, paid service activation or live Stripe operation was performed. The eight approved audit/design documents remain in place. No multi-species, eligibility/matching or Connect implementation was added.

## Outcome and completion gates

M0 build/authentication/ownership implementation is available for review. The retained .NET/C#/MAUI/ASP.NET Core/EF/SQL Server architecture builds for API and Android debug. **Do not call the full M0 acceptance gate complete yet:** the real SQL Server rehearsal now passes, but provider-specific browser login/PKCE/refresh/recovery needs the owner's provider decision before it can be integrated and verified. iOS requires a suitable Mac/Xcode runner. No M1 implementation is authorized or started.

## Build repairs and compatibility

Three confirmed API defects repaired without replacing the architecture: import Requests in IMessageService; reference EF InMemory 8.0.5 for the existing development fallback; replace Stripe's invalid DateTime/Unix/null-coalescing capture expression with actual processing UTC time. SendGrid 9.29.0 does not exist on the registry; explicitly select the previously resolved 9.29.1 instead of leaving NU1603 drift.

Native validation found additional existing build blockers: non-existent CommunityToolkit.Maui 8.0.2 (use nearest published 8.0.1), missing explicit MAUI Controls/Compatibility packages (8.0.100), missing Logging.Debug dependency, wrong named-argument case/Core namespace, inline XAML namespace resolution, invalid Shadow value and AncestorType syntax. Focused repairs retain MVVM/XAML screens and targeting. Enable nullable analysis and align converter signatures instead of suppressing warnings. FwfAndroidOnly selects Android in this project only, without incorrectly propagating TargetFrameworks into Core. SDK pinned at 8.0.425. Version lifecycle/store compatibility, particularly unsupported MAUI 8, still requires an approved supported-toolchain upgrade before release; these debug builds do not prove store eligibility.

## Authentication and account protection

`DogWalkerApi/Security/AuthenticationSetup.cs` replaces permissive JWT placeholders with HTTPS OIDC discovery and RS256/ES256, issuer/audience/signature/expiry enforcement, short clock skew, required subject and a fallback authenticated-user policy. Outside Development, absent authority/audience or SQL configuration stops startup. Development without a provider has no default signing key, credential or token endpoint. Query tokens are accepted only for SignalR transports. Disabled accounts are rechecked per HTTP request; long-lived hub connections close on authentication expiry.

`Account` binds exact `(issuer, subject)` to nullable ClientId/WalkerId with unique indexes and restricted foreign keys. SQL identity columns use binary collation; an ordinal equality check also prevents SQL trailing-space padding from matching a different subject. No email-based claims or token-supplied profile/admin privileges. New authenticated owners create their own profile and account atomically; duplicate claims conflict. Existing profiles require a verified administrative binding. Initial administrator authority must be provisioned out of band by a trusted database operator; no bootstrap/default administrator endpoint exists. Binding logs include acting administrator/account/profile IDs, without tokens or contact details. Account metadata is available at `/api/accounts/me`.

Owners read/write only their client and dogs. Assigned caregivers read only relevant profiles and explicitly booked dogs for pending/confirmed/in-progress work, not every animal in a household; cancellation removes that care-profile visibility. Booking reads require owner/assigned-caregiver membership. Owner-scoped booking history was added. Creation verifies authorized owner, dog-owner relationship, real caregiver existence and valid service/time. Customer status changes are limited to permitted cancellations; operational status changes require assigned caregiver/admin and valid existing transitions. Route/media writes require assignment. Payment initiation requires booking ownership and the stored booking amount; refunds require admin. This is minimal authorization hardening, not the future marketplace payment/state-machine design.

Messages verify actual owner/caregiver relationships, optional booking pairing and sender direction; SenderId is server-derived. Anonymous hub access/arbitrary group joins/spoofed status writes are denied. BookingBroadcaster selects current non-disabled participant accounts at delivery time, so old group membership does not retain delivery permission. Unauthorized/missing resource responses use generic 401/403/404 and expected conflict errors use 409, not database/private exception contents. Browser CORS requires explicit allowed origins.

## Mobile boundary

SecureStorage-backed token provider and HTTPS bearer handler apply authenticated API transport; a 401 clears the expired access token and requests reauthentication. SignalR also receives the secure access token. Packaged API settings now determine request destination. Fixed customer/caregiver IDs and random booking IDs were removed; `/api/accounts/me`, real dog selection and a real selected booking supply context. Quick-book opens the existing form; messages/tracking navigate with real booking IDs. Business default caregiver can be configured as an existing ID until discovery is implemented. New-client action collects actual contact input instead of persisting fabricated contacts. Secret-bearing shared/mobile config fields and the committed sample SQL credential were removed; rotate the old credential if it was ever used.

**There is no claimed working production/native sign-in yet.** The handler deliberately refuses to invent tokens. Provider-specific authorization-code PKCE/browser callback, secure refresh/logout/recovery and device validation are blocked on provider approval. Options and tradeoffs are in `M0_AUTHENTICATION_DECISION.md`. No provider-specific dependency, contract, service or cost was selected.

## Database handling

`DogWalkerApi/Data/Scripts/M0_AddAccounts.sql` is an additive reviewed-by-owner upgrade candidate for existing Clients/Walkers tables; it was executed twice against a disposable real SQL Server Developer test database, never against production. It creates account bindings/indexes/FKs only and never auto-claims records. Back up and verify actual legacy schema before applying. Do not drop account data to undo M0: roll back code/config with secured compatibility, preserving bindings. No automatic production EnsureCreated/Migrate is added. A no-action Booking.Messages relationship avoids SQL Server's existing multiple-cascade-path issue when creating new databases; changes to actual pre-existing constraints need separate schema reconciliation, not guessed DDL. Unit/HTTP tests use a disposable SQLite database with test-only UTC-tick conversion because SQLite cannot order native DateTimeOffset. Real SQL tests validate production mapping, case-sensitive subjects, trailing-space rejection, unique owner bindings/FKs and repeat execution of the additive script while retaining legacy owner rows. They create/delete only a random test-owned database.

Positive-path relational tests also exposed existing Guid collection inserts treated as updates. Dog creation and route/media append now explicitly mark new entities Added; the related successful-write tests verify persistence. Metadata-only media handling is preserved; private upload/storage security remains future work, not claimed implemented.

## Verification evidence

- API/Core Release: build succeeded, zero warnings/errors.
- Android net8.0-android Debug: succeeded, zero warnings/errors; `com.friendswithfur.walker-Signed.apk` generated using a local debug key. No emulator/device/store release or iOS build was run.
- Full xUnit suite: **48 discovered, 48 executed and passed, 0 failed, 0 skipped**, including real SQL Server. Final regression TRX: `/workspace/fwf-m0-final.a9H85W/TestResults/m0-complete.trx`. Earlier install-script results with skipped SQL coverage are historical, superseded by this run. Coverage includes anonymous/invalid issuer/audience/signature/expiry/missing subject, disabled and unbound accounts, forged admin/profile claims, isolated client/dog/history/booking reads, foreign-pet/owner booking writes, real valid booking/dog/route/media inserts, operational actor/status restrictions, message impersonation, explicit/unique admin binding, actual SignalR membership/revocation, and mobile HTTPS/token expiry transport. Test keys are generated in memory; production authentication is NOT replaced by a fake test scheme.
- Live development Kestrel smoke: clients/dogs/accounts return 401 anonymously; generated Swagger schema returns 200 and includes account endpoints. This is a local security/startup check, not provider-login or payment proof. The smoke server was stopped afterward.
- .NET SDK checksum validated against Microsoft SHA-512 metadata; JDK verified against official Adoptium SHA-256; Android SDK artifacts checked against Google's published checksums. Linux workload installer reported signature checking skipped, so artifacts were not used until separate `dotnet nuget verify --all` verified 49 packages with zero errors. Certificate revocation endpoints were unavailable and produced revocation-status warnings; no TLS/signature/checksum or revocation setting was disabled.
- Repeatable local tooling helper executed successfully. Saved install script uses a disposable validation copy because the repository tracks obj files; original generated files/IDE databases remain untouched.
- `git diff --check` and helper syntax checks pass. CI TRX guard rejected the earlier skipped SQL suite and now accepts all 48 passed tests; no thresholds/assertions were bypassed to claim completion.

The SQL image pull previously received 403 on redirected `centralus.data.mcr.microsoft.com`. The narrow requested access is now effective: Docker downloaded the official image, the helper started a local Developer instance with verified TLS, and all SQL assertions passed. Review additionally reproduced an administrator binding edge case caused by SQL trailing-space comparison, added an exact ordinal guard, and verified the full suite afterward. See `M0_SQL_SERVER_VERIFICATION.md` for digest, limits, network scope and reproduction. No further network broadening was performed. Current GitHub Actions execution is distinct from these local results and is checked after the authorized push where access allows.

## CI and reproducibility

`.github/workflows/m0-validation.yml` runs on push/PR/manual dispatch with contents-read permissions: Release API build, all required tests including disposable SQL Server Developer, nonzero/all-passed TRX enforcement, result artifacts, container cleanup; separate Android debug build with .NET/JDK17/Android34/workload verification. Secrets are generated for the disposable SQL container and kept in protected runner temp settings, not committed. The SQL helper configures a generated localhost certificate, verifies its hostname/chain, and supplies a temporary CA bundle via SSL_CERT_FILE; it does not disable TLS validation. Its real container startup and SQL validation have now passed locally. These definitions do not deploy or purchase services. Hosted CI execution may consume the repository's existing Actions allowance when eventually pushed; no run was triggered here.

Reusable cloud install/start draft captures tool activation, writable caches, validation-copy build/tests/Android commands and remaining gates. Saving is not publication or a new-task restoration test. Review/save changes in environment settings and publish the development environment only when appropriate; this is distinct from deploying the application.

## Remaining gates and next-milestone readiness

Before M1: review any actual deployed database schema (the disposable SQL test now passes), approve a concrete OIDC provider/integration without unapproved cost, implement/validate native PKCE/session lifecycle, and confirm baseline iOS/device behavior on an appropriate runner. Backend ownership foundations and Android compilation are prepared; commercial/authenticated device readiness is not certified. Legacy unbound data intentionally stays inaccessible until verified binding. No approved data is deleted or auto-reassigned.

Outside M0: availability/race-proof scheduling, complete payment/webhook/refund reconciliation, private media, robust GPS/offline delivery, real-time message fan-out, rate limits/support administration and multi-species remain in the approved backlog. No Stripe Connect, matching or pet-domain modernization was implemented. Stop here; M1 requires separate explicit authorization after M0's remaining acceptance gates are resolved.

## Files changed or added for M0

- `DogWalker.Core/Abstractions/IBookingService.cs`
- `DogWalker.Core/Abstractions/IMessageService.cs`
- `DogWalker.Core/Configuration/AppEnvironmentOptions.cs`
- `DogWalkerApi/Controllers/BookingsController.cs`
- `DogWalkerApi/Data/DogWalkerDbContext.cs`
- `DogWalkerApi/DogWalkerApi.csproj`
- `DogWalkerApi/Hubs/WalkHub.cs`
- `DogWalkerApi/Program.cs`
- `DogWalkerApi/Services/BookingService.cs`
- `DogWalkerApi/Services/ClientService.cs`
- `DogWalkerApi/Services/DogService.cs`
- `DogWalkerApi/Services/MessageService.cs`
- `DogWalkerApi/Services/PaymentService.cs`
- `DogWalkerApi/Validators/CreateBookingRequestValidator.cs`
- `DogWalkerApi/appsettings.Development.json`
- `DogWalkerApi/appsettings.json`
- `DogWalkerApp/AppShell.xaml`
- `DogWalkerApp/AppShell.xaml.cs`
- `DogWalkerApp/Converters/CollectionEmptyConverter.cs`
- `DogWalkerApp/Converters/MessageAlignmentConverter.cs`
- `DogWalkerApp/DogWalkerApp.csproj`
- `DogWalkerApp/MauiProgram.cs`
- `DogWalkerApp/Resources/Raw/appsettings.json`
- `DogWalkerApp/Services/Api/IDogWalkerApi.cs`
- `DogWalkerApp/Services/AppSettingsProvider.cs`
- `DogWalkerApp/Services/Messaging/RealTimeMessagingService.cs`
- `DogWalkerApp/Services/Storage/SecureStorageService.cs`
- `DogWalkerApp/ViewModels/BookingsViewModel.cs`
- `DogWalkerApp/ViewModels/ClientsViewModel.cs`
- `DogWalkerApp/ViewModels/DashboardViewModel.cs`
- `DogWalkerApp/ViewModels/MessagesViewModel.cs`
- `DogWalkerApp/ViewModels/WalkTrackerViewModel.cs`
- `DogWalkerApp/Views/BookingsPage.xaml`
- `DogWalkerApp/Views/ClientsPage.xaml`
- `DogWalkerApp/Views/DashboardPage.xaml`
- `DogWalkerApp/Views/MessagesPage.xaml.cs`
- `DogWalkerApp/Views/WalkTrackerPage.xaml`
- `DogWalkerApp/Views/WalkTrackerPage.xaml.cs`
- `DogWalkerEntrepreneur.sln`
- `.github/workflows/m0-validation.yml`
- `.gitignore`
- `DogWalker.Core/DTOs/AccountProfileDto.cs`
- `DogWalker.Core/Models/Account.cs`
- `DogWalkerApi/Controllers/AccountsController.cs`
- `DogWalkerApi/Data/Scripts/M0_AddAccounts.sql`
- `DogWalkerApi/Security/AccountAccess.cs`
- `DogWalkerApi/Security/AccountUserIdProvider.cs`
- `DogWalkerApi/Security/AuthenticationSetup.cs`
- `DogWalkerApi/Security/BookingBroadcaster.cs`
- `DogWalkerApi/Security/ResourceAccessException.cs`
- `DogWalkerApp/Services/Authentication/AccessTokenProvider.cs`
- `DogWalkerApp/Services/Authentication/AuthenticatedApiHandler.cs`
- `DogWalkerApp/Services/IAppSettingsProvider.cs`
- `DogWalkerApp/Services/Storage/ISecureStorageService.cs`
- `global.json`
- `scripts/check-test-results.py`
- `scripts/setup-android-tools.py`
- `scripts/start-test-sql.sh`
- `scripts/verify-workload-packages.py`
- `tests/DogWalkerApi.Tests/ApiFactory.cs`
- `tests/DogWalkerApi.Tests/AuthenticationUnitTests.cs`
- `tests/DogWalkerApi.Tests/DogWalkerApi.Tests.csproj`
- `tests/DogWalkerApi.Tests/MobileAuthenticationTests.cs`
- `tests/DogWalkerApi.Tests/ResourceIsolationTests.cs`
- `tests/DogWalkerApi.Tests/SqlServerAccountTests.cs`

Additional M0 documents: `docs/M0_AUTHENTICATION_DECISION.md` and this report. The eight prior audit/planning documents are retained unchanged during implementation.
