# M0 verification and preservation — 2026-10-14

Owner-approved scope: review, verify and commit/push existing M0 implementation/tests/CI/documentation to `codex/m0-auth-ownership`; compare providers; test local SQL; document mobile gates. M1, provider activation, paid resources, deployment, architectural replacement and merging main are not authorized. Commit/push identifiers are supplied in the final product-owner report and can be read from this branch's Git history; a commit cannot include its own final SHA in its contents.

## Final verification evidence

- API/Core Release: zero warnings/errors.
- Android Debug net8.0-android: zero warnings/errors; debug APK generated. This is not a device or store-authentication result.
- Full xUnit result: **48 executed, 48 passed, 0 failed, 0 skipped**, including the previously blocked SQL Server test. Final TRX: `/workspace/fwf-m0-final.a9H85W/TestResults/m0-complete.trx`; finish timestamp `2026-10-13T19:59:28Z` (15:59 on 10/13 in America/New_York). Today the tested controller/test copies were verified to match the current source. The older 47-pass/one-skipped result is superseded.
- Real SQL Server Developer container used with a random password and verified localhost certificate. No production database, hosted trial, Azure SQL, paid resource or live payment was involved. Only a random test-owned database was created/deleted; the container and temporary connection settings/keys were removed after verification.
- The original SQL assertions passed. Review then added a trailing-space administrative-binding regression that failed with the old guard; the focused exact issuer/subject comparison fixed it and the complete 48-test suite passed. No assertion was weakened.
- Changed/new source, configuration, helper, tests and docs were reviewed; secret-pattern scanning covers private keys, cloud/service tokens and literal passwords. Test tokens and identities are explicitly synthetic; real keys/passwords/certificates remain outside Git. Existing binary IDE caches/generated metadata were not newly added or modified. This scan concerns commit candidates, not a forensic guarantee about all inherited historical blobs.
- `git diff --check`, helper syntax and project XML pass. API reads remain authenticated/scoped, SQL identities are exact, and no future domain milestone code was added.

## SQL and network conclusion

The exact previously requested `centralus.data.mcr.microsoft.com` access now permits the required Microsoft SQL Server image download. Microsoft's official MCR/MAR firewall guidance documents the regional `*.data.mcr.microsoft.com` family. No additional wildcard or broad permissions were introduced during this verification. Hostname policy cannot enforce access to only one image/repository/path; digest pinning and a controlled mirror/path-aware policy would be needed for stronger artifact restrictions. Cached downloads reduce further egress needs. See `M0_SQL_SERVER_VERIFICATION.md` for image digest and limitations.

## Authentication recommendation and cost

Recommend **Microsoft Entra External ID external customer tenant + MSAL system-browser PKCE** for approval. Official Microsoft documentation states the first **50,000 core MAU are free**, so core estimates at 100/1,000/10,000 MAU are **$0/month**, excluding premium/SMS/add-on and unrelated hosting/distribution costs. Billing/subscription linkage and tenant activation require approval. Live paid pricing pages were inaccessible; no unverified paid price is represented as a current quote. The comparison includes Auth0, Keycloak and first-party ASP.NET Identity plus an OIDC server, with compatibility/refresh/complexity/scale/lock-in tradeoffs. See `M0_AUTHENTICATION_DECISION.md`.

No authentication service, account, paid tier, subscription resource or deployment has been activated. Reusing an already approved OIDC tenant is preferable if it meets the necessary mobile/API requirements. Commercial Keycloak hosting is not free merely because its license is free. No paid cloud service is required for the completed local verification; Apple distribution and optional premium identity features are future, separately approved costs.

## Remaining M0 work

1. Owner approves a concrete identity provider, region/subscription linkage and feature/cost boundaries before activation.
2. Implement provider-specific browser PKCE, delegated API access-token acquisition, callback routing, secure persistence, silent refresh/rotation, logout/recovery and account-switch handling. Current token-reading/401-clearing transport is not this full lifecycle.
3. Validate exact issuer/audience/native-client/scopes from the approved tenant; keep SQL-based customer/caregiver/admin authority and verified legacy profile binding.
4. Test real Android browser/callback/cache/network behavior on an emulator and physical device; debug compilation alone is insufficient.
5. Provide a compatible Mac/Xcode/iOS workload, simulator, callback configuration and Keychain entitlements; validate a real iOS device with appropriate signing/provisioning. App Store/TestFlight distribution typically needs paid Apple membership; simulator checks do not constitute distribution.
6. Review and approve a supported .NET/MAUI platform upgrade before commercial launch. MAUI 8 currently compiles but is outside support; no major upgrade was made here.
7. Review the actual existing database schema and backup/rollback before applying any additive script outside the disposable test database.
8. Observe the GitHub Actions run after push if access permits; local success is distinct from a hosted-CI result. GitHub API access is currently denied even though scoped Git read/write access may work; no credential value should be requested in chat.

M1 remains stopped and requires a separate explicit authorization. No main merge or production deployment is included in preservation.
