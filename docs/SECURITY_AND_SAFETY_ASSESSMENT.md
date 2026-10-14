# Security and safety assessment

Baseline references: `DogWalkerApi/Program.cs`, `Controllers/*.cs`, `Hubs/WalkHub.cs`, `Services/PaymentService.cs`, `Services/MessageService.cs`, `DogWalker.Core/Configuration/AppEnvironmentOptions.cs`, `DogWalkerApp/MauiProgram.cs`, platform manifests. Findings are source-observed risks; no penetration test or live provider verification occurred.

| Priority | Finding | Exploit/impact | Required mitigation/test |
|---|---|---|---|
| P0 | No API/hub authorization; incomplete JWT validation | Anonymous access/mutation to contact, bookings, route/media/payment actions | Validated OIDC tokens, fallback auth, participant policies; anonymous/expired/wrong issuer/key and cross-account tests |
| P0 | Caller supplies ClientId/WalkerId/DogId/direction | Impersonation, cross-household booking/message | Resolve principal; owner/pet and participant checks; reject forged IDs/direction |
| P0 | PaymentRequest accepts amount and service trusts it | Underpayment or arbitrary charge manipulation | Persisted server quote/minor units; sandbox tamper and currency tests |
| P0 | Arbitrary booking statuses and capture after commit | Unapproved completion/capture, inconsistent commerce | Actor/transition rules, reconciliation/outbox; replay/concurrent-state tests |
| P0 | WalkHub permits arbitrary groups and broadcasts | Position/contact exposure, spoofed service events | Booking membership at join/send; deny ended/revoked assignments |
| P0 | No qualification/availability enforcement | Unqualified care/double booking | All-animal eligibility, expiry/revocation and SQL race tests |
| P0 | No webhook signature/idempotency/Connect ledger | Duplicate side effects, unreliable funds allocation | Raw-body verification, unique events/operation keys, reconciliation |
| P0 | Base appsettings committed SQL sample password and client secret fields | Credential reuse risk; mobile secrets extractable | Rotate if used; secure runtime settings, remove secret-bearing mobile schema in approved milestone |
| P1 | URL-only media/no private storage | Unsafe content, sensitive assets exposed | Size/type/scan/private storage/grants/resource checks |
| P1 | Unlimited lists/text/messages/routes | Enumeration/DoS/storage abuse | Pagination, per-user/IP limits, bounds and quotas |
| P1 | Raw errors/weak operational audit | Leakage and poor incident attribution | Safe ProblemDetails, redaction, structured immutable audit |
| P1 | GPS background/broad mobile permissions/backup enabled | Excessive tracking, sensitive local backup | Opt-in walk-only scope, retention, runtime permission review and platform backup policy |
| P1 | Any-origin CORS and weak TLS sample configuration | Browser exposure/configuration drift | Deployment origins where applicable, verified TLS and secure SQL certs; never disable verification |

## Authorization and privacy

Roles: customer, caregiver, restricted support, verifier, administrator with least privilege. Self-registration cannot grant admin or verification claims. Customer sees own household/pets/bookings; caregiver sees care details only for accepted/active assigned visits and needed dispute retention. Search exposes approximate location and verified badges, not addresses/contact/medical notes. Support actions require reason, audit and appropriately limited access; logs must exclude secrets and private instructions. Sensitive fields have purpose/retention policy, access audit, deletion/export workflows and legal-hold exceptions for financial records. Store pet notes only to deliver care; avoid collecting human medical information. MFA for administrators and payout-account changes; protect session tokens in native secure storage and never ship private keys.

## Trust and operating policy gates

A marketplace launch needs real provider onboarding, identity verification, claim evidence, expiration/revocation, service eligibility, support coverage, dispute handling and legal/insurance review. Evaluate actual Stripe Identity/Connect capabilities and a regional background-check provider under documented consent/adverse-action requirements; no integration is assumed implemented or universally available. Display self-reported experience as self-reported. Connect KYC does not substitute for background checks or care competency. Insurance badges require reviewed evidence; no Rover-style guarantee/insurance is claimed.

Policies must define: allowed species/services/handling restrictions; geographic launch jurisdiction; home entry/key handling; incident documentation/emergency owner/vet contacts and consent; animal injury/property damage escalation; missing pets/escape; cancellation/no-show/reschedule; refund authority and dispute evidence; provider suspension; data access after cancellation; boarding/daycare local permits; consumer protection/tax/employment classification obligations. Emergency UI provides owner-approved instructions and escalation contacts, not veterinary diagnosis. Livestock, dangerous species, treatment/surgery and unapproved medication services remain excluded. Legal/insurance professional review is an external launch prerequisite, not an invented certification.

## Validation plan

Security regression suite uses distinct owner A/B, caregiver assigned/unassigned, verifier/support/admin. Test all object-ID routes and hub groups, revoked/expired qualifications, invalid species care data, uploaded object access, malicious content types, over-limit requests, booking and payment replay, privilege escalation, rate limiting and redacted errors. SQL integration verifies restrictions; Stripe test mode verifies valid/invalid signed events, duplicates/out-of-order/crash recovery/refunds. Device tests cover logout/reinstall/backup, interrupted requests and notification privacy. Record passed/failed/blocked counts per milestone; current executed test count is zero.
