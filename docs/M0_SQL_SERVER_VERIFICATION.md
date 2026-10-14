# M0 SQL Server verification and registry access

Review completed 2026-10-14; initial passing SQL execution and the subsequent identity-padding regression check are timestamped 2026-10-13 in their TRX records. This document distinguishes those executed results from today's preservation/reporting work.

## Free local test strategy and result

A local SQL Server 2022 **Developer** container was started using `scripts/start-test-sql.sh` solely for non-production tests. Developer licensing is appropriate to development/testing, not production commercial hosting. Docker was already available. No Azure SQL, VM, trial subscription, paid temporary environment or remote infrastructure was provisioned.

The helper generates a random SA password, temporary localhost TLS certificate/trust bundle and protected settings outside the repository. SQL Server runs as its normal non-root user after a short container-only certificate bootstrap. Database clients use encryption with certificate/hostname validation; `TrustServerCertificate=False`. Credentials, private keys and settings are not committed or printed. The database test overrides the supplied database name with a random `FwfM0Tests_<guid>` and drops only that test-owned database afterward.

Verified:

- API/Core Release build, zero warnings/errors.
- **48 tests passed, zero failed, zero skipped**, including the actual SQL Server test. TRX: `/workspace/fwf-m0-final.a9H85W/TestResults/m0-complete.trx`.
- The production SQL Server EF model can create the disposable schema, including the multiple-cascade-path correction.
- The additive `M0_AddAccounts.sql` executes twice without replacing legacy clients; existing owner ID is retained.
- Case-sensitive identity subjects remain distinct; duplicate issuer/subject, duplicate client binding and missing-owner FKs are rejected.
- SQL's trailing-space padding cannot authorize a mismatched token subject.
- Review found that administrator binding also needed exact ordinal subject comparison. The new SQL regression failed on the old implementation, the focused guard repaired it, and the complete suite passed afterward. This is M0 ownership verification, not a new domain milestone.

This is representative disposable-database proof. It does not validate an unknown deployed production schema, its migrations/history or backups. Applying the additive script to an existing real database still requires actual schema review, backup/restore evidence and operator authorization.

## Why the network domain is necessary

Microsoft publishes SQL Server images under `mcr.microsoft.com/mssql/server`. Registry requests discover a manifest and content hashes, then redirect layer/config downloads to regional data endpoints. `centralus.data.mcr.microsoft.com` is one such endpoint in the official Microsoft-owned `*.data.mcr.microsoft.com` family; it is not an authentication provider or paid application service.

[Microsoft's official MCR/MAR client firewall documentation](https://github.com/microsoft/containerregistry/blob/main/docs/client-firewall-rules.md) explicitly lists `mcr.microsoft.com` for discovery and `*.data.mcr.microsoft.com` for data delivery. It recommends the wildcard for regional failover. For this scoped M0 work, only the already requested exact Central US hostname was needed; no wildcard/unrestricted policy was added. The actual image pull now succeeded without further network edits, so the old access blocker is resolved in this running instance.

Successful download digest: `sha256:cf8f0ab2ebe61feec2eed2629061a07d247f791c2a8ddda74a4ac9f06a291718` for `mcr.microsoft.com/mssql/server:2022-latest`. Docker verified content hashes. The moving tag may change later; use an approved digest to pin reproducibility when needed rather than trusting an unverified replacement.

## Scope and limitations of restriction

- This environment's domain allowlist can restrict hostnames; it cannot restrict HTTP paths/repository/image names. Allowing this hostname therefore permits other Microsoft artifacts delivered by that same regional host; it is not an image-specific authorization rule.
- The SQL image manifest and each blob are content-addressed; digest verification provides artifact identity/integrity, not network path restriction.
- Exact-host access is narrower than Microsoft's recommended regional wildcard but may fail if routing changes. Diagnose the new official redirect and request only the additional required host rather than broadening permissions automatically.
- Once the image is cached, repeated local tests do not need fresh downloads until the image changes/is removed. A snapshot or preloaded digest-pinned image can avoid persistent download access in future tasks; host policy review/removal is a separate explicit environment-settings choice.
- True repository-specific egress restrictions require a proxy/firewall supporting suitable path/registry policy or a controlled artifact mirror; do not claim the current hostname tool enforces this.

## Safe reproduction

From the checkout, run `bash scripts/start-test-sql.sh /tmp/fwf-test.connection` only if that output path/container is unused. Use the protected connection file for FWF_TEST_SQL_CONNECTION and its generated TLS `ca-bundle.pem` for SSL_CERT_FILE; do not print their contents. Build/test in a fresh validation copy to preserve tracked obj files. Run `scripts/check-test-results.py` on the current TRX so zero/skipped tests cannot count as CI success. Remove only the created test container and generated settings after completion. Reuse neither these credentials nor this one-day certificate for production.
