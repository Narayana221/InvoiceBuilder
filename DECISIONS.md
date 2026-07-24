# Decisions Log

Lightweight ADR log for Invoice Builder. One entry per significant architectural decision.

Format per entry:

```
## [Phase N] Short decision title

**Context:** What problem/situation prompted this decision.

**Decision:** What was decided.

**Alternatives considered:** What else was on the table, and why it was passed over.

**Consequences:** What this makes easier, harder, or what it locks us into.
```

---

## [Phase 0] No authentication for the initial build

**Context:** The spec lists a Users/Auth module as a stretch goal (phase 10), but the API and Angular app need to work in the meantime.

**Decision:** Build the Invoice/Customer/Sender module fully open (no auth) through the core phases. Add real authentication as its own later phase.

**Alternatives considered:** A shared API key header as a lightweight gate; pulling a minimal JWT/login flow forward into an earlier phase.

**Consequences:** Faster to build and test the core CRUD + PDF flow. The API and UI are unprotected until the Auth phase — not suitable to expose publicly before then.

---

## [Phase 0] Single flat tax rate per invoice

**Context:** The UI mockup shows one "Tax Rate (%)" field per invoice, applied to the whole subtotal, rather than per-line-item tax rates.

**Decision:** Model tax as a single rate on the Invoice entity, applied once to the summed line-item subtotal to compute the tax amount and total.

**Alternatives considered:** Per-line-item tax rates, which would allow mixed tax categories on one invoice but adds complexity to both the domain model and the summary UI that isn't required by the spec.

**Consequences:** Simpler Invoice entity and summary calculation. Revisiting this later (e.g., for multi-jurisdiction tax) would require a migration adding a tax rate/amount to line items.

---

## [Phase 0] Self-signed certificate for PDF digital signatures in development

**Context:** PDF requirements call for digital signatures to prove integrity/origin. A real CA-issued certificate isn't available yet.

**Decision:** Generate a local self-signed certificate for development/testing of the IronPDF signing flow. Document how to swap in a real CA-issued certificate for production via configuration.

**Alternatives considered:** Deferring signature support entirely until a later pass — rejected so the signing pipeline is exercised (and testable) from the PDF generation phase onward, rather than bolted on afterward.

**Consequences:** Signed PDFs in dev will show as "self-signed / not trusted" until a production cert is swapped in; the signing code path itself doesn't change between environments, only the cert source (config-driven).

---

## [Phase 2] Soft delete for Customer, Sender, Invoice

**Context:** The spec's Delete actions ("with confirmation") don't say whether records are actually removed. Customers/Senders can be referenced by existing invoices.

**Decision:** Add `IsDeleted` + `DeletedAtUtc` to Customer, Sender, and Invoice (via a shared `ISoftDeletable` interface in `InvoiceBuilder.Shared`), with an EF Core global query filter (`!IsDeleted`) excluding them by default. Invoice's FK to Customer/Sender uses `DeleteBehavior.Restrict` as a second line of defense against ever hard-deleting a party with invoice history.

**Alternatives considered:** Hard delete, which is simpler but risks orphaning or losing history for past invoices when a Customer/Sender used on them is deleted.

**Consequences:** "Deleted" rows still occupy storage and must be excluded consistently — `InvoiceLineItem` needed a matching filter (`!Invoice.IsDeleted`) once EF Core flagged the mismatch during migration generation, since it doesn't implement `ISoftDeletable` itself but is reachable independently of its parent Invoice.

---

## [Phase 2] Auto-generated invoice numbers (design decided now, generator built in Phase 3)

**Context:** The mockup shows an editable-looking "INV-2025-005" field, but per-user free text risks duplicates/inconsistent formats.

**Decision:** `InvoiceNumber` is a required, unique `varchar(50)` column now. The actual sequential-generation algorithm (e.g. per-year counter) is deferred to Phase 3 (Invoice Module API), since it's a creation-time behavior, not a schema concern.

**Alternatives considered:** User-entered free text with only a uniqueness constraint — rejected per earlier discussion in favor of guaranteed uniqueness and consistent formatting.

**Consequences:** The unique index applies globally, including soft-deleted rows — an invoice number is never reused, even after "deletion," which is the correct behavior for an audit trail.

---

## [Phase 2] Persisted totals on Invoice, computed-only totals on line items

**Context:** Invoice needs Subtotal/Tax/Total for the paginated list view; individual line items need a line total for the detail/edit view.

**Decision:** `InvoiceLineItem.LineTotal` is an unmapped computed property (`Quantity * UnitPrice`) — never persisted, never out of sync. `Invoice.SubtotalAmount/TaxAmount/TotalAmount` ARE persisted columns, recalculated via a `RecalculateTotals()` domain method whenever line items change.

**Alternatives considered:** Computing Invoice totals on the fly too (fully normalized) — rejected because the Invoices list screen would need to load and sum every invoice's line items on every page load just to render a totals column.

**Consequences:** Any code path that adds/removes/edits line items (Phase 3's API) must remember to call `RecalculateTotals()` before saving, or the persisted totals go stale. All monetary columns use `numeric(18,2)` via `HasPrecision`, never `float`/`double`, to avoid floating-point rounding errors in currency math.

---

## [Phase 2] Local Postgres port conflict — containerized Postgres moved to host port 5433

**Context:** This dev machine already has a native Postgres install listening on `127.0.0.1:5432`. Docker's port-forward for the `postgres` service also targeted `5432`, and on macOS the native install's loopback binding intercepted connections meant for the container — `dotnet ef` calls against `localhost:5432` failed with "role does not exist" even though the container was healthy.

**Decision:** Remap `docker-compose.yml`'s `postgres` service to `5433:5432` on the host side (container-internal port and inter-container networking via `Host=postgres` are unaffected). `appsettings.Development.json`'s connection string was updated to `Port=5433` for host-side tools (`dotnet run`, `dotnet ef`).

**Alternatives considered:** Stopping the user's native Postgres service — rejected as out of scope; it may be in use by other projects.

**Consequences:** Anyone running backend tooling directly on the host (not via Docker) must use port 5433, not the Postgres default 5432. This is specific to this machine's setup and worth re-checking if it causes confusion later (e.g. documented in a README).
