# Database migration and rollback plan

Design only; no migration or production operation was executed. Baseline `DogWalkerApi/Data/DogWalkerDbContext.cs` has no migration history/model snapshot in repository. Actual deployed schema/data are unknown. `DogWalker.Core/Models/Dog.cs` stores ClientId; Booking.cs stores one DogId; Payment/route/media/message relations must be preserved. Never infer production constraints solely from conventions.

## Prerequisite discovery

After authorized compile repair, generate and review the EF model/SQL on disposable SQL Server. Obtain schema, __EFMigrationsHistory, counts, keys, orphan reports, enum distributions, time/money anomalies, backup and restore proof from an authorized operator. Reconcile conventions with deployed schema, especially DogId and cascade paths. For an existing database without migrations, baseline against actual schema without executing a create-all migration. New empty databases get a separately reviewed initial migration. Do not use EnsureCreated to bypass migration history. Production read/export access is not currently supplied.

## Expand → backfill → switch → retain

1. Add Species/Category/definitions, Household, Pet and typed care tables; stable dog code. Explicit restricted/no-action FKs for historical commerce records, bounded column lengths, owner/species indexes and rowversion. Keep Dogs and all old columns/routes intact.
2. Add nullable v2 booking household/service/quote pointers and BookingPet unique(BookingId,PetId), BookingHabitat, snapshots and transition history. Add currency/decimal or minor-unit money columns alongside double columns; do not round silently. Add indexed caregiver/time/status reservations and quote/payment event tables.
3. Backfill one Pet per Dog with SAME Guid and OwnerId=ClientId, preserving dates, special needs, VetContact, muzzle/reactivity as dog-profile typed requirements. Preserve original source fields and a migration mapping/audit table. Create one household per owner only if address provenance supports it. Null/invalid dates and weights enter explicit review, not invented corrections.
4. Backfill BookingPet from each valid historical DogId. Verify booking ClientId equals dog ClientId. Quarantine mismatches/orphans for operator reconciliation; do not reassign ownership or discard history. Preserve Booking/Payment/MessageThread/Media/Route IDs and FKs. Snapshot available legacy instructions with source/version and mark missing historical information unknown. Never claim today's instructions were used on old visits.
5. Versioned server adapter accepts legacy dog endpoints for dog-only bookings; single transaction updates Dogs + Pet during compatibility period. Idempotent checkpoints permit resumable backfill and prevent dual-write divergence. Legacy readers cannot receive multi-species/multi-pet records as misleading single-dog DTOs: expose v2 and return explicit upgrade-required behavior where necessary.
6. Reconcile counts and per-owner/per-booking IDs; validate unique constraints/FKs, orphan/mismatch zero or approved quarantine, message/payment totals, money rounding report and unchanged historical prices. Disable old writers by controlled rollout; switch reads after rehearsal and monitored gates. Retain old tables for at least the approved rollback window. Dropping columns/tables is a later separately approved migration.

Money conversion: derive currency precision per supported currency (USD initially), reconcile floating-point anomalies against Stripe ledger and stored receipts, record conversion strategy and original values. Historical transaction totals must not be recalculated using new pricing. Do not modify legacy enum values. Explicit unique Stripe event IDs and payment intent IDs prevent duplicate ingestion.

## Rollback

Before v2-only writes: turn off v2 feature flags and restore old readers/writers while keeping additive tables. Backfilled rows can be ignored; do not delete history. After multi-species or multi-pet writes: legacy application cannot represent new records. Stop affected writers and keep v2 data/read access; rollback code only to a compatibility build capable of preserving new data. A destructive Down migration is not a safe rollback. Backup restore requires coordinated write freeze and external Stripe event reconciliation; never lose new payments or pretend backup restores external state. A forward fix is preferred after live v2 writes.

## Rehearsal and acceptance

Disposable SQL Server fixtures: zero rows; owner with multiple dogs/bookings/payments/routes/media/messages; null DOB; reactive/muzzled dog and vet contact; mismatched owner; orphan dog ID; legacy enum values; refunded/cancelled/completed histories; precision anomalies; duplicate/replayed backfill; interruption/resume; concurrent legacy create during transition; two cats + bird v2 record. Assert exact legacy IDs/counts/FKs/totals, preserved text, expected exceptions/quarantine, no cascade deletion, unique booking pets, v1 dog behavior and v2 authorization. Test expand/backfill twice and rollback before/after v2 writes. SQL Server integration is mandatory; EF InMemory cannot validate migrations/constraints/isolation.

Release gates: restore rehearsal passes; migration scripts reviewed; bounded batches/resources measured; no unexplained orphans; compatibility version deployed; observed reconciliation clean; operator-approved maintenance/rollback plan. Engineering estimate included in multi-species milestone, not an authorization to access production.
