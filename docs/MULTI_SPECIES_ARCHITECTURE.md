# Multi-species architecture (proposed, approval required)

Keep .NET MAUI/C#/ASP.NET DogWalker.Core/EF DogWalker.Core/SQL Server/SignalR/Stripe/MVVM. Evolve the current three-project modular monolith; do not add microservices, AI matching or a separate database technology. Existing evidence: `DogWalkerApi/Program.cs`, `Data/DogWalkerDbContext.cs`, `DogWalker.Core/Models/Booking.cs`, `DogWalkerApp/MauiProgram.cs`. No proposed class below exists unless explicitly identified as current.

## Domain boundaries and schema

Account identity is distinct from Client (owner profile) and Walker (caregiver profile). Bind an authenticated subject to these profiles; allow both roles on one account without treating an arbitrary caller-supplied Guid as identity. Keep Walker naming internally initially for compatibility. Add Household(owner/client FK, address, time zone, restricted emergency instructions), HouseholdMember only if sharing is later approved.

Pet: existing Guid Id retained for migrated dogs; OwnerId references Client; HouseholdId; Name (bounded), SpeciesId, BreedOrVariety, DateOfBirth (date), Sex enum including unknown, optional WeightKg decimal, private ProfilePhotoAssetId, Temperament, BehavioralNotes, FeedingInstructions, restricted MedicalNotes, emergency/veterinarian contact references, CreatedAt/UpdatedAt UTC and RowVersion. Weight is optional for fish/birds; do not fabricate values. Limit profiles, text lengths, upload size and booking count through configurable abuse protections while permitting multiple pets.

SpeciesCategory and Species are curated catalog rows with stable codes, support status, policy version and effective dates. Categories organize UI; eligibility uses exact supported species/variant policies. Species activation requires admin approval, care schema, service compatibility, qualifications and tests. Dogs, cats, rabbits, companion birds, permitted reptiles, fish, hamsters, guinea pigs and ferrets are candidates, not automatically enabled. Start cats/birds/dogs with drop-ins and dog walks; expand only after policy/provider readiness. Dangerous animals, livestock and veterinary procedures are disabled.

CareRequirementDefinition: stable code/version, species/service applicability, allowed value kind (text, boolean, integer, decimal, time-of-day, enum, bounded list), units/range, required flag, sensitivity, owner-facing label, qualification implications. PetCareProfile references Pet and schema version. PetCareRequirement references profile/definition with typed value columns and a CHECK requiring exactly the permitted value type. API discriminated DTO contracts reject unknown codes, inconsistent units, unsupported definitions and oversized text. No unrestricted JSON or inheritance tree per species. Some common fields stay on Pet; structured care details use composition. Preserve historical definition versions.

| Species | Profile/tasks | Eligibility considerations |
|---|---|---|
| Dog | Walk/leash/reactivity/exercise/feeding | Handling restrictions, group capacity, approved walking service |
| Cat | Food/litter/hiding/indoor-only/escape precautions | Cat experience and safe entry procedure |
| Bird | Diet/cage hygiene/handling/socialization/environment hazards | Species/handling capability; never default free-flight handling |
| Reptile | Owner temperature/humidity/lighting/feeding/enclosure targets | Explicit permitted species; habitat-care qualification |
| Fish/aquarium | Habitat identity, feeding/equipment/owner water targets/approved maintenance | Feeding/observation distinct from qualified maintenance |
| Rabbit/small mammals | Hay/feeding/enclosure/enrichment/temperature/handling | Relevant species experience; no dog exercise assumptions |

Habitat is a separate owned resource (aquarium/enclosure) linked through PetHabitat; aquarium work can target a habitat without duplicating feeding per fish. Household visit includes pets and explicitly selected habitats. No task provides diagnosis/treatment advice. Medication recordkeeping is enabled only under approved policy and eligible provider qualifications.

BookingPet(BookingId,PetId) unique composite key plus owner-verified FK, instruction snapshot version, per-pet price attribution and report linkage. BookingHabitat and BookingTask capture shared habitat tasks. Booking refers to Household and one caregiver for MVP. Every pet/habitat must belong to the booking owner. No silent partial eligibility when one animal fails.

CaregiverSpeciesCapability, CaregiverServiceOffering, Qualification, QualificationEvidence and AdminVerification separate self-report from verified claims. Record evidence reviewer/date/expiry/status and revocation. AvailabilityWindow, TimeOff, Reservation and configurable species/capacity limits support scheduling. Reviews require completed eligible bookings; administration moderates incidents/reviews. Identity/background/insurance badges require actual verified evidence and expiration checks.

ServiceDefinition, SpeciesServicePolicy, PriceRule and CancellationPolicy replace runtime enum assumptions while retaining legacy enum mappings. Backend produces immutable Quote/QuoteLine/PolicyVersion in integer minor currency units; fixed currency for MVP. Fees are configurable/versioned. Validate duration, units, discounts, add-ons and tip caps; client submits quote ID, not prices. Recurrence starts as a later milestone, not a complex scheduling engine in launch MVP.

## Deterministic eligibility and reservation

Proposed IEligibilityService.EvaluateAsync examines every pet/species, selected service/tasks, valid verified qualifications, radius/location, duration, time, provider capacity, animal count and policy restrictions. Return EligibleCaregiverDto with reason codes/evidence dates and quote summary; return ineligible reason categories without exposing personal evidence or home address. Rank eligible candidates by explicit experience/distance/price preferences; no paid ranking or AI initially. Self-report alone cannot satisfy verified-required policy. Missing location or qualifications fails explicitly.

Recheck at booking request and acceptance; an earlier search result cannot authorize confirmation. Use UTC instants plus household/provider time zone identifier; validate daylight-saving gaps/ambiguities in UI. Store half-open [start,end) reservations including travel buffers. SQL Server serializable transaction with per-caregiver scheduling lock and indexed overlap query prevents two concurrent overlapping requests; ordinary rowversion alone cannot protect insert phantoms. Never hold DB transactions across Stripe calls. Reservations have expiry, revalidation and idempotent request keys.

Keep existing BookingStatus numeric values for legacy records. Introduce v2 transition records rather than renumbering: Draft → Requested → AcceptedAwaitingPayment → Confirmed → InProgress → Completed; Requested → Declined/Expired; cancellation available under versioned policy. Payment, refund, dispute and payout are separate states: Refunded legacy bookings map explicitly. Only owner can request/cancel own booking; assigned caregiver accepts/starts/completes; admin exception requires reason/audit. Confirmation requires eligibility, reservation and successful approved payment condition. Completion requires per-pet reports/tasks or documented exception. No-show/dispute have explicit audited actions. Rowversion + transactional history/outbox enforce race-safe transitions; asynchronous payment reconciliation handles failures.

## MAUI experience

Preserve brand: Friends with Fur — Pet care for every kind of companion. Adapt `AppShell.xaml`, existing page/ViewModel/Refit patterns incrementally. Customer tabs: Discover Care, My Pets, Bookings, Messages, Profile. Caregiver tabs: Dashboard, Booking Requests, Schedule, Care Reports, Earnings. Shared session controls role navigation; authorization always remains server-side. Species-first profile renders only supported typed care fields. A multi-select booking view shows every selected pet, required qualifications, itemized authoritative quote and clear failure states. Never show leash controls to fish.

Keep busy/error patterns but sanitize errors, refresh command state, add accessible labels, semantic focus/errors, scalable fonts, contrast, keyboard/screen-reader tests and empty/retry states. Offline drafts may persist encrypted/minimized local data, but acceptance/payment/booking mutation requires server confirmation and idempotent retry. No local queue silently confirms booking/payment. Media shows pending uploads and resumes only authorized operations. Remove production secrets from packaged configuration; environment URLs and publishable Stripe keys only.

## MVP boundary

First complete identity, supported pet profiles and legacy preservation; then qualification/admin/availability, multi-pet bookings and pricing; then Connect sandbox, messages, checklists/reports and controlled release. Success is an integrated two-cats/one-bird scenario across devices/API/SQL/Stripe sandbox with negative security tests. Defer social/livestream/AI/advanced analytics/white-label/farm/veterinary/advanced SaaS. See backlog for gates; platform feasibility is strong but current implementation is not production ready.
