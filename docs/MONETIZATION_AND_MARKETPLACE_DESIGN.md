# Monetization and marketplace design

Current product is independent dog-walker management scaffolding (`README.md`, `DogWalkerApp/AppShell.xaml`, fixed walker IDs in ViewModels). `Walker.cs` has no marketplace qualification/account/payout attributes; `PaymentService.cs` only orchestrates ordinary Stripe intents/refunds. No true discovery, provider onboarding, fee allocation, subscription or team billing exists.

## Model decision

| Model | Build cost/time | Acquisition | Operations | First revenue considerations |
|---|---|---|---|---|
| A: broad Rover-style marketplace | Highest; full identity/trust/supply/payments plus geographic density | Acquire both owners/providers; compete locally | Verification, emergencies, disputes, payouts, coverage | Delayed until sufficient eligible local supply and safety controls |
| B: professional caregiver SaaS | Lower initial commerce/discovery burden, but still needs tenant isolation/reliable scheduling | Sell to a narrower provider segment | Support and data controls; avoid assuming platform payment responsibility disappears | Potentially earlier paid pilots with existing client books; subscription features are not currently built |
| C: staged hybrid | Controlled marketplace pilot first; postpone SaaS until demand evidence | Recruit a small invited verified provider cohort and households in one region | Marketplace responsibilities still apply from first transaction | Recommended for stated vision: assisted supply recruitment/manual admin verification, functioning marketplace software, later optional professional tools |

Recommendation C does not mean building both products simultaneously. If discovery demand cannot be validated, product owner may choose B as a separately approved scope change. Do not silently substitute SaaS for the requested marketplace. Validate supply/owner demand with operational pilots before expanding species/geography.

## Fees and unit economics

Backend FeePolicy(version,effective date,region,currency,percentage,minimum/maximum as approved) and QuoteLine itemize provider subtotal, add-pet/duration/species/add-ons, platform fee/tax and tip. Decide whether commission is deducted from provider proceeds or charged to owner; present full total and net earnings clearly. For illustrative $60 service subtotal, $0 tip/tax, commission deducted from provider, fee rates 10/15/20% generate platform gross $6/$9/$12 and provider gross $54/$51/$48. Example US card rate assumption 2.9% + $0.30 costs $2.04, leaving $3.96/$6.96/$9.96 platform contribution before Connect, identity checks, refunds, support, hosting, insurance and acquisition. These are assumptions, not quoted negotiated/provider prices. Tips are excluded from fee by proposed policy; owner and provider must approve final commercial terms. All stored amounts use minor units and explicit rounding; historical fee versions do not change.

Do not hardcode rates in code or accept client amount overrides. Refunds and partial cancellations need original quote allocations and fee/transfer reversal policy. Tax reporting/collection and regional tax rules require separate assessment. Record immutable charge/refund/transfer/payout ledger entries and reconcile regularly; UI lifetime spend/earnings is derived, not authoritative manually updated money.

## Stripe Connect evaluation

Use Connect test mode and verify supported platform country, connected-account countries, business types, care activity, settlement currencies and cross-border rules before selecting charge design. Prefer hosted/embedded supported onboarding to avoid collecting identity documents directly. Account model/controller responsibilities must be reviewed against current Stripe capabilities; not all countries support all combinations. Required features: account create/onboarding links, account.updated capability state, payouts readiness, PaymentIntent confirmation, platform fee allocation, refunds/transfer reversals, disputes, failed payouts, deauthorization and reconciliation. Return verified account capability state, never assume completing a redirect means onboarding passed.

Destination charges can simplify one-caregiver bookings with platform application fees but determine liability/disputes and regional requirements. Separate charges/transfers support delayed provider settlement but require balance/reconciliation and region support. Direct charges shift account relationships and reporting. Choose with Stripe documentation/legal review; do not implement all three. MVP uses one caregiver/booking, one supported currency/region and an approved charge/transfer strategy.

Current intent creates automatic-payment-methods without manual capture; CaptureAsync is not a complete lifecycle. Do not rely on card authorizations lasting until far-future visits. Define charge timing/cancellation policy and reauthorization/customer consent handling. No live payments were initiated; Stripe sandbox and native checkout remain unverified. A vetted MAUI native wrapper/hosted checkout decision needs device review before adding dependencies. Card data never enters application database/logs. Webhooks require signature verification, unique event IDs, durable retries and out-of-order guards.

## Later professional SaaS

P2 only after marketplace MVP approval: provider dashboard/client CRM, recurring schedules, invoicing, reporting, team membership and subscription entitlements. Keep tenant/account memberships, permissions and marketplace ledger isolated; optional subscription billing uses a separate product/price/entitlement lifecycle from visit payments. No AI, livestream/social, white-label or advanced analytics in initial release.
