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
