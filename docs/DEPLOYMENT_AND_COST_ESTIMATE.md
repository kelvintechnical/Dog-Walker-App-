# Deployment and cost estimate

Planning estimates in USD/month as of 2026-10-13; not live provider quotations. No infrastructure deployed, paid resources provisioned, mobile accounts purchased or live payments initiated. `DogWalkerApi/Program.cs` and app platform/configuration files are starting points, not operational deployment. Pricing, supported versions and store rules must be rechecked before purchase.

## Minimum realistic topology

One ASP.NET Core API/worker host with durable outbox; SQL Server-compatible managed database; private object storage; approved OIDC identity provider; TLS domain/secret management; authorized SignalR on API initially; FCM/APNs push and optional email; structured centralized logs/errors/uptime checks; automated database backups and tested restore. Retain SQL Server; do not silently switch to PostgreSQL/SQLite to quote cheaper hosting. EF InMemory is ephemeral test-only, not deployable persistence. A managed SQL offering with limited pilot capacity plus small API host is a reasonable affordable baseline; serverless cold start and database storage/connection constraints need measurement. Avoid Kubernetes/service mesh/enterprise tiers for a small pilot.

Single API instance can host SignalR pilot connections; restart/reconnect must rejoin authorized groups and replay persisted messages. Multiple replicas require supported backplane/managed SignalR strategy and shared scheduling/idempotency SQL state; sticky sessions alone do not deliver broadcasts across nodes. Media goes to object storage, not API disk. GPS is deferred from MVP, reducing constant writes and battery/privacy burden. Use private SQL networking where budget/provider supports it; secure TLS/least-privilege database credentials always required. APIs must use reviewed supported .NET releases before launch.

## Assumptions and monthly ranges

Active user means unique monthly account, not concurrent connection. Each active user averages two $60 visits/month, five 1 MB photos/visit, 60-day hot media retention, light REST traffic and bursts of messages, no video/livestream. Photos exclude generated thumbnails overhead; allow contingency. 100 users ≈200 bookings/$12,000 GMV/2 GB new photos monthly; 1,000 ≈2,000/$120,000/20 GB; 10,000 ≈20,000/$1.2m/200 GB. Caregiver vs owner proportions and actual booking frequency can change these materially. Backup/log retention, retries and bandwidth included approximately in bands, not guaranteed capacity.

| Item | Small minimum pilot | 100 active | 1,000 active | 10,000 active |
|---|---:|---:|---:|---:|
| API + worker/real-time hosting | $20–50 | $25–60 | $60–180 | $250–800 |
| Managed SQL Server-compatible DB/backups | $15–50 | $25–70 | $80–250 | $300–1,200 |
| Private media/storage/egress | $2–10 | $3–15 | $15–75 | $100–400 |
| Auth/email/push | $0–20 | $0–25 | $10–100 | $80–500 |
| Logging/monitoring | $0–20 | $5–25 | $20–100 | $80–300 |
| Domain allocation | $1–3 | $1–3 | $1–3 | $1–3 |
| Monthly hosting total | **$38–153** | **$59–198** | **$186–708** | **$811–3,203** |

These totals exclude engineering, staff/customer support, legal/compliance, insurance, background/identity checks, SMS, taxes, payment fees, refunds/disputes, acquisition and Apple build machines. Free tiers may lower a prototype bill but are not dependable commercial capacity. Example card processing assumption US domestic 2.9% + $0.30: $2.04 per $60 payment, so approximately $408/$4,080/$40,800 monthly at 100/1,000/10,000 users under visit assumptions. International cards/currency conversion/Connect account/payout/dispute fees are additional. Obtain current regional Stripe Connect pricing and determine whether platform or provider bears charges. Planning verification allowance $10–75 per newly onboarded provider depending on checks/region; insurance/support must be separately quoted. Never advertise coverage based on this allowance.

## Release blockers and native distribution

Current API/Core cannot compile unchanged; development selects missing InMemory provider. No migration history, SQL runtime validation, identity, authoritative payment lifecycle, background worker, storage or tests/CI exists. Base SQL sample credentials/TLS configuration must be replaced securely. MAUI Refit localhost differs from API launch ports and cannot reach host from a real phone. Packaged secret fields are unsafe. Runtime version lifecycle and dependency upgrades require a controlled compatibility milestone.

Android: install compatible MAUI Android workload, JDK/Android SDK and target current Play API requirements; verify branded assets (both referenced icon files exist), signing keystore/secure CI, permissions/backup policy, privacy/data-safety disclosures, camera/location device tests and release package. Review necessity of background location/storage/audio permissions; GPS support is incomplete. Google Play enrollment commonly budgeted as ~$25 one-time, subject to current regional terms.

iOS: Mac/Xcode-compatible workload/signing/provisioning/App Store account; simulator and actual device tests; privacy/permissions/notification entitlements; supported OS/SDK and store review. iOS Info.plist includes camera/location but no microphone rationale despite video capture; background location mode is not configured. Apple enrollment commonly budgeted as ~$99/year, subject to current terms. No Mac/Xcode runner available in this Linux environment; installed SDK alone cannot validate iOS. Mac runner cost/hardware is extra. Branding/assets, screenshots, accessibility, account deletion and public privacy/support policies needed for stores. Store admission is not guaranteed.

## Scaling and launch gates

Measure API latency/errors, SQL query/lock time, reservation contention, webhook queue age, outbox failures, connection/reconnect counts and storage growth. Index owner/pet history and caregiver/time queries, paginate all lists, use bounded uploads and quotas, archive location data on approved retention. Load test actual concurrent bookings and connections; active-user count alone does not select VM size. Upgrade DB tier/replicas only against measured pressure; multi-replica SignalR and worker dispatch require explicit design. Rehearse restore, migration, webhook replay and provider payout reconciliation before launch. Ship to invited owners/verified caregivers in one region first; expand only after operational safety and unit economics evidence.
