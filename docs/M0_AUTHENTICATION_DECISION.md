# M0 authentication decision — provider approval pending

M0 implements an OIDC/OAuth2 bearer-token resource server using the existing JwtBearer dependency. No authentication provider/account/service has been selected, activated or purchased. Provider-neutral standards are the implementation boundary: HTTPS discovery, RS256/ES256 signatures, exact issuer/audience, expiry, and SQL account binding by `(issuer, sub)`. The API does not invent passwords, mint unsigned/test tokens, trust email as identity, or accept caller-supplied role/profile claims. Development without configuration rejects tokens; non-development fails startup without issuer/audience and SQL configuration.

Options for product-owner approval:

| Option | Benefit | Cost/lock-in/operations |
|---|---|---|
| Self-hosted standards-based OIDC (e.g. Keycloak) | Control, portable protocol, no per-user license assumed | Extra secured service/DB, patching/backups/MFA/email/admin operations; hosting costs |
| Managed OIDC (e.g. Microsoft Entra External ID/Auth0) | Managed recovery/MFA/availability, faster onboarding | Regional features/tiers/MAU pricing and migration constraints must be checked; no free-plan durability assumed |
| First-party ASP.NET Core Identity + standards-based authorization server | .NET alignment and database control | Identity alone is not an OIDC server; substantial credential/recovery/security engineering and possibly licensing |

Recommendation: choose a managed or existing trusted OIDC tenant only after current pricing, supported mobile PKCE/logout/refresh and data-residency review. Do not buy/activate now. Approval is required before provider-specific integration. Native login requires authorization-code + PKCE in system browser with nonce/state, secure refresh-token handling and server access tokens; not an embedded login webview, ID token or arbitrary token-entry UI.

Account permissions live in SQL, not external role strings. Authenticated new customers may create their own profile atomically. Existing profiles are NEVER claimed by email, caller ID or first login: an authorized operator verifies identity and explicitly binds issuer/sub to existing ClientId/WalkerId. A trusted database operator provisions initial administrator status out of band; there is no bootstrap HTTP endpoint or default admin/password. Administrators may bind profiles through the restricted accounts endpoint; ordinary users cannot. Unique bindings prohibit two accounts claiming one profile. Existing dog/client/booking routes stay but now require authentication and scoped access (intentional security behavior change).

Only ephemeral RSA tokens generated inside tests are used for validation, with a test-only static discovery configuration; production has no testing authentication scheme/key endpoint. Native sign-in is not claimed complete until provider approval and device validation. See M0 implementation report for executed evidence and limitations.

## Provider comparison and recommendation — reviewed 2026-10-14

**Recommendation: Microsoft Entra External ID, using an external customer tenant and browser-based authorization-code + PKCE through MSAL.NET.** This is a recommendation for approval, not an activated provider. Keep the current ASP.NET Core bearer validation and SQL account ownership model. Do not use an employee/workforce tenant as a substitute for customer identity, and do not start a new Azure AD B2C deployment.

The decisive benefits for a small commercial .NET marketplace are an official MAUI/MSAL sample supporting Android/iOS, managed credential recovery and signing-key rotation, standard OAuth2/OIDC access tokens, and a documented **first 50,000 monthly active users free for core authentication**. Microsoft explicitly states that premium add-ons do not share this free tier and SMS second-factor verification costs extra. Basic External ID billing requires an Azure subscription; creating/linking it requires owner approval even when forecast core usage is free. Consumer and administrative MFA needs, supported federation, regional availability and account-recovery requirements must be checked before enabling the chosen flow. Do not assume all required security features are included free.

| Provider | .NET MAUI / iOS / Android | API validation, sessions and refresh | Free tier / projected cost | Implementation / scale / lock-in |
|---|---|---|---|---|
| **Entra External ID** | Official MAUI + MSAL.NET sample; system browser, platform callback setup and secure platform token cache | Standard JWT/OIDC; API-specific delegated access token, exact issuer/audience and signing keys; MSAL silent acquisition/refresh, interactive recovery and explicit logout | Core first 50,000 MAU free, verified in Microsoft documentation; excludes SMS, premium add-ons and unrelated Azure services | Medium: tenant/user flows, native client and API registrations/scopes, platform callbacks/cache. Managed scale; moderate Microsoft configuration/SDK lock-in, portable wire protocol |
| **Auth0** | Official Auth0.OidcClient.MAUI SDK and MAUI quickstart; system browser and registered callback/logout URLs | Standard OIDC/RS256 API access tokens; offline-access/refresh configuration and rotation where supported by plan; secure native cache remains application responsibility | A Free offering is available, but the current quota, MFA entitlement and paid tiers could not be verified because pricing-site access failed. Forecast $0 only if its current commercial Free terms cover actual MAU/features; obtain a quote before choosing a paid tier | Low–medium developer complexity, strong hosted login experience and managed scale. Moderate lock-in through Actions, connections, user metadata and plan-specific capabilities |
| **Keycloak (self-hosted)** | Standards-based browser PKCE with a compatible .NET OIDC client; no need to change MAUI or ASP.NET Core | JWT/JWKS/issuer/audience, configurable sessions/refresh/revocation; operator owns upgrades, recovery, availability and storage security | $0 software license for local testing. Illustrative small production allowance **$20–80/month additional hosting**, excluding staff/backup/HA costs; not a vendor quote or a free production hosting claim | High operations burden; scalable with additional infrastructure. Lower vendor lock-in; Microsoft SQL Server is a documented database option, with appropriate JDBC driver/version review |
| **ASP.NET Core Identity + an OIDC server** | MAUI can use standards-based browser PKCE; ASP.NET Core Identity alone does not issue a complete OIDC authorization service | First-party password/MFA/recovery lifecycle plus a properly implemented authorization server, key rotation, grants, refresh/revocation and secure native cache | Existing app hosting may absorb some compute; real cost is engineering/operations/email/security review. Authorization-server license terms must be reviewed separately; do not assume every server is free | Highest implementation/security responsibility. .NET/SQL alignment and potentially lower provider lock-in, but unnecessary initial marketplace complexity; constitutes a separate architectural decision |

An already approved, trustworthy OIDC tenant with the necessary mobile/API features is worth reusing before creating any new provider. Otherwise Entra External ID is the recommended first choice. Auth0 is the alternative when its hosted UX/features materially reduce delivery work and its verified plan cost is acceptable. Keycloak is justified when data-control/hosting requirements outweigh the additional operational responsibility. First-party credential hosting is not recommended for this MVP.

### Core identity cost projections (USD/month)

| Unique interactive MAU | Entra External ID core estimate |
|---:|---:|
| 100 | $0 |
| 1,000 | $0 |
| 10,000 | $0 |
| 50,000 | $0 |
| Above 50,000 | Billable excess at the current regional rate; verify before crossing the threshold |

These are identity-provider core charges, not total application operating costs. MAU is authenticated unique users/month, not registered accounts or peak concurrent users. Microsoft combines eligible MAU across linked tenants/subscriptions according to its billing rules. SMS/MFA add-ons, machine-to-machine add-ons, identity verification, Azure application/database hosting, email customization/support and Apple distribution can add costs. Budget alerts are monitoring, not a hard spending cap. No paid feature or resource has been activated.

### Approval and integration boundaries

1. Obtain explicit approval for Entra External ID (or the chosen alternative), region, billing/subscription linkage and required security features before activation.
2. Register a public native client and a separate API resource with delegated scopes. A native app must not contain a client secret. Use access tokens for API calls; an ID token is not an API credential. Keep local SQL permissions authoritative.
3. Configure discovery authority, exact issuer, API audience and permitted client/scope semantics from the real tenant metadata. Friendly browser authority and token issuer can differ; if necessary introduce separately validated settings rather than disabling issuer/audience checks.
4. Implement a maintained MSAL/OIDC adapter, platform callbacks, secure persistent cache, silent refresh/rotation, cancellation/recovery and explicit sign-out. The current AccessTokenProvider only reads/clears an access token; full refresh and browser login are not implemented.
5. Validate migration/binding by exact issuer+subject. Changing providers requires deliberately verified rebindings and possibly password resets/federation migration; do not auto-link matching emails.
6. Test with a development tenant and synthetic users, then devices and restricted support/admin roles. Approve any runtime major-version upgrade separately; current official MAUI samples target newer supported .NET, so their compatibility cannot be assumed for MAUI 8.

### Sources and pricing verification limits

Official content consulted through the vendors' public GitHub documentation because several live pricing websites were inaccessible from this environment:

- [Microsoft External ID customer FAQ: 50,000-MAU core free tier and add-on exclusions](https://github.com/MicrosoftDocs/entra-docs/blob/main/docs/external-id/customers/faq-customers.md).
- [Microsoft External ID billing model and subscription linkage](https://github.com/MicrosoftDocs/entra-docs/blob/main/docs/external-id/external-identities-pricing.md).
- [Microsoft official MAUI/MSAL CIAM sample](https://github.com/Azure-Samples/ms-identity-ciam-dotnet-tutorial/tree/main/1-Authentication/2-sign-in-maui).
- [Auth0 official .NET/mobile SDK, including MAUI](https://github.com/auth0/auth0-oidc-client-net).
- [Keycloak official database configuration, including SQL Server](https://github.com/keycloak/keycloak/blob/main/docs/guides/server/db.adoc).
- Current pricing pages to verify before activation: [Entra External ID](https://azure.microsoft.com/en-us/pricing/details/microsoft-entra-external-id/) and [Auth0](https://auth0.com/pricing). Live paid-tier prices were **not independently verified**; no stale list price is presented as a current quote.
