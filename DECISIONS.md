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

---

## [Phase 3] No repository/service layer between endpoints and DbContext

**Context:** Minimal API handlers need to run business logic (CRUD, invoice-number generation, totals recalculation) against the database.

**Decision:** Endpoint handlers call `InvoicesDbContext` directly. No `IXRepository`/`IXService` interfaces wrap it.

**Alternatives considered:** A repository-per-entity layer — rejected because `DbContext` already implements the repository and unit-of-work patterns; wrapping it in another interface at this project's size adds indirection with no real abstraction behind it (there's only ever one implementation, Postgres via EF Core).

**Consequences:** If a second data-access technology or heavier domain logic shows up later, that's the signal to introduce a service layer — not before. Business rules that do exist (totals recalculation, invoice numbering) live as methods on the domain entities / a small dedicated service (`InvoiceNumberGenerator`), not scattered across endpoint lambdas.

---

## [Phase 3] FluentValidation wired in via a custom `IEndpointFilter`, not built-in auto-validation

**Context:** FluentValidation removed its ASP.NET Core MVC auto-validation integration in v11; Minimal APIs never had one to begin with.

**Decision:** A generic `ValidationFilter<TRequest>` (`IEndpointFilter`) resolves `IValidator<TRequest>` from DI and runs before the handler, added per-endpoint via `.AddEndpointFilter<ValidationFilter<TRequest>>()`.

**Alternatives considered:** Manual `if (!result.IsValid) return ...` checks inside every handler — rejected as repetitive and easy to forget on a new endpoint.

**Consequences:** Any new POST/PUT endpoint that wants validation must remember to both register `IValidator<TRequest>` in DI (`InvoicesModule.cs`) and attach the filter — neither happens automatically.

---

## [Phase 3] Manual DTO mapping, no AutoMapper/Mapster

**Context:** Entities (EF-tracked, with navigation properties) shouldn't be serialized directly over HTTP — risk of over-posting, circular navigation references, and leaking fields like `IsDeleted`.

**Decision:** Plain C# `record` DTOs (`CustomerDto`, `InvoiceRequest`, etc.) with hand-written `ToDto()`/`ApplyRequest()` extension methods per entity.

**Alternatives considered:** AutoMapper or Mapster for convention-based mapping — rejected as an added dependency and a layer of reflection-based indirection that isn't earning its keep at four entities; explicit mapping methods are easier to debug and just as fast to write here.

**Consequences:** `InvoiceMapping.ToDto()`/`ToSummaryDto()` require `Customer`/`Sender`/`LineItems` navigations to already be loaded (via `.Include(...)`) — calling them on a lazily-unloaded entity throws. This is a real footgun, flagged with a comment at the top of `InvoiceMapping.cs`.

---

## [Phase 3] Invoice-number race condition accepted, not solved

**Context:** `InvoiceNumberGenerator` computes "next number" by querying the current max and adding 1 — classic TOCTOU (time-of-check-to-time-of-use) race under concurrent requests.

**Decision:** Accept the race for now. The unique index on `InvoiceNumber` (from Phase 2) means a genuine collision fails loud with a DB constraint violation on the second concurrent insert, rather than silently duplicating a number.

**Alternatives considered:** A Postgres advisory lock or serializable transaction around the read-then-insert — rejected as unnecessary complexity for a single-user side project with no realistic concurrent-invoice-creation scenario.

**Consequences:** Under real concurrent load this would need revisiting (e.g. a `SELECT ... FOR UPDATE` on a per-year counter row, or a native Postgres sequence). Worth reconsidering if a Payments/multi-user module ever makes concurrent invoice creation plausible.

---

## [Phase 3] Docker Desktop daemon corruption from a transient disk-full condition

**Context:** While rebuilding the `api` image, the host disk briefly reported ~119MB free (from ~10GB normally), and Docker's build failed with `input/output error` writing to its containerd metadata store. The disk space itself recovered shortly after, but Docker's daemon was left in a state where even `docker ps` hangs and previously-running containers (Postgres, api, web) became unreachable.

**Decision:** Did not attempt automated recovery (`docker system prune`, killing/restarting the Docker Desktop process) — flagged it to the user instead, since restarting Docker Desktop stops every container on the machine, not just this project's, and is the user's call.

**Alternatives considered:** Scripting a forced Docker Desktop restart — rejected as an unrequested, disruptive action on shared machine state.

**Consequences:** Phase 3's Docker container smoke test is deferred until Docker Desktop is restarted. The code itself was already verified independently of Docker: `dotnet test` (unit + integration, the latter over real HTTP against a real Postgres instance) passed before Docker hung.

---

## [Phase 4] IronPDF for HTML-to-PDF — blocked at runtime without at least a trial key

**Context:** IronPDF is a commercial library (perpetual licenses from ~$749+/year); CLAUDE.md's tech stack names it explicitly. Buying a license isn't a decision the assistant can make on the user's behalf. Initial research (IronPDF's own marketing pages) suggested unlicensed/trial use produces a watermarked-but-functional PDF — **this turned out to be wrong.** Actually running `InvoicePdfRenderer` (package version 2026.8.1) throws `IronSoftware.Exceptions.LicensingException: Production License Required` immediately, regardless of `ASPNETCORE_ENVIRONMENT`. The "free for 7 days" grace period referenced in the error message requires first registering for a trial key at ironpdf.com (email signup, no payment) — there is no zero-config unlicensed path at all with this version.

**Decision:** Build and commit the full rendering/signing/PDF-UA pipeline anyway — the code is correct and was verified by direct compilation and by the `LicensingException` itself (which only fires *after* `RenderHtmlAsPdfUA` successfully rendered the HTML — the failure is in `PdfDocument.BinaryData`, i.e. the licensing gate, not the rendering). `IronPdf:LicenseKey` is wired through config; the corresponding test (`InvoicePdfRendererTests.Render_ProducesASignedPdf`) is marked `[Fact(Skip = ...)]` rather than left failing, since a red test here would misleadingly suggest a code defect rather than a licensing gate. The user was asked and chose to defer getting a trial key rather than register for one now.

**Alternatives considered:** Registering for a trial key immediately to unblock verification — offered to the user, declined for now. Swapping to a different PDF library — rejected, CLAUDE.md specifies IronPDF explicitly as a deliberate, informed tech choice (not something to second-guess unilaterally).

**Consequences:** `GET /api/invoices/{id}/pdf` will return a 500 (unhandled `LicensingException`) until `IronPdf:LicenseKey` is set to a real trial or paid key. This is not a bug to "fix" later — it's an external dependency the project cannot function around. Whoever picks this back up should register a trial key at https://ironpdf.com/start-free/trial/, set it in `appsettings.Development.json`, remove the `Skip` from `InvoicePdfRendererTests`, and confirm the test passes before considering Phase 4 verified.

---

## [Phase 4] PDF/UA tagging via `RenderHtmlAsPdfUA`, not a full compliance audit

**Context:** The spec requires PDF/UA compliance for long-term storage/accessibility. Full compliance validation (screen-reader testing, structure-tree auditing) is a specialized, ongoing discipline, not a one-time render-time flag.

**Decision:** Use IronPDF's `ChromePdfRenderer.RenderHtmlAsPdfUA()` (instead of `RenderHtmlAsPdf()`) and set `pdf.MetaData.Title` — this produces genuinely tagged PDF/UA-1 output (structure tree, reading order, declared title) essentially for free, as long as the source HTML uses semantic markup (`<h1>`/`<h2>`, `<table><thead><th scope="col">`, no divs-pretending-to-be-tables). The invoice template was written with exactly that in mind.

**Alternatives considered:** Skipping PDF/UA entirely and revisiting later — rejected once research showed the "good enough for now" version costs one method-name swap, not a redesign.

**Consequences:** This is best-effort structural tagging, not a certified compliance audit. If a real accessibility/audit requirement ever demands formal PDF/UA-1 or PDF/UA-2 validation, that's a dedicated task, not something this phase claims to have finished.

---

## [Phase 4] Plain string-interpolation HTML template, no Razor/Scriban

**Context:** The invoice PDF needs one HTML document assembled from an `Invoice` domain object, including a variable-length line-items table.

**Decision:** `InvoiceHtmlTemplate.Build(Invoice)` is a static method using C# raw string literals (`$$"""..."""`) with a separate loop building the repeating `<tr>` fragment. No templating library.

**Alternatives considered:** RazorLight/Scriban for proper markup/logic separation — rejected as unjustified weight at exactly one document type. Every user-controlled field (`Customer.Name`, `Sender.BankDetails`, `Notes`, line item `Description`) is passed through `WebUtility.HtmlEncode` before interpolation — unescaped user input rendered by a real browser engine (Chromium, via IronPDF) is a genuine HTML-injection risk, not a theoretical one, so this is tested explicitly (`InvoiceHtmlTemplateTests.Build_HtmlEncodesUserSuppliedFields`).

**Consequences:** If a second document type (e.g. a payment receipt) is added later, revisit this — copy-pasting a second raw-string template is the signal that a real templating layer has started earning its keep.

---

## [Phase 4] Self-signed dev certificate lives in the Api project, gitignored, copied to publish output

**Context:** Digital signatures require a certificate; Phase 0 already decided on a self-signed dev cert with a real cert swapped in for production.

**Decision:** `backend/src/InvoiceBuilder.Api/certs/invoice-signing.pfx` (`.pfx` already covered by the repo's `*.pfx` gitignore rule), referenced via `<None Include="certs\*.pfx" CopyToOutputDirectory="PreserveNewest" />` in `InvoiceBuilder.Api.csproj` so `dotnet publish` carries it into the Docker image automatically. Path/password configured via `IronPdf:SigningCertificatePath`/`SigningCertificatePassword` in `appsettings.Development.json`, resolved at runtime relative to `AppContext.BaseDirectory`.

**Alternatives considered:** A separate `certs/` folder outside any project (was the original plan) — rejected once it became clear `dotnet publish` only copies a project's own output, not sibling folders; putting the cert inside the deployable project is what makes it actually reach the Docker image.

**Consequences:** In production, this dev `.pfx` and its throwaway password (committed in plaintext in `appsettings.Development.json`, consistent with this repo's existing pattern for the dev Postgres password) must be replaced with a real certificate provisioned via a proper secret store — not baked into the image.

---

## [Phase 4] Chromium runtime dependencies added to the Api Docker image

**Context:** IronPDF renders via a headless Chromium engine. The `mcr.microsoft.com/dotnet/aspnet:10.0` base image is minimal Debian and lacks the shared libraries Chromium needs — this would work locally (macOS has them) and fail silently-until-runtime inside the container specifically.

**Decision:** Added an `apt-get install` layer to the Dockerfile's final stage installing `libnss3`, `libgbm1`, `libgdiplus`, and related packages, per IronPDF's documented Debian/Ubuntu dependency list.

**Alternatives considered:** Discovering this via a failed container at PDF-generation time — rejected in favor of fixing it proactively, since it's a well-documented, known requirement rather than a surprise.

**Consequences:** The Api image is larger and slower to build. This fix was verified as *correct* — the base image resolved to Ubuntu Noble (24.04) on this arm64 host rather than Debian, requiring `libasound2t64` instead of `libasound2` (fixed after the first build failure named the exact missing package). The apt layer itself is no longer the blocker; see the next entry for what is.

---

## [Phase 4] IronPDF has no Linux ARM64 native Chrome build — Docker PDF generation blocked on this machine

**Context:** After fixing the apt dependency and the `libasound2t64` naming, `GET /api/invoices/{id}/pdf` still failed inside the container, but with a different error: `IronSoftwareDeploymentException` — unable to locate or download `IronInterop` for `linux-arm64`. Adding an explicit `IronPdf.Native.Chrome.Linux` package reference didn't fix it either. Inspecting that package's contents directly (`~/.nuget/packages/ironpdf.native.chrome.linux/2026.8.1/runtimes/`) showed it ships **`linux-x64` only** — no `linux-arm64` build exists at all. This Mac is Apple Silicon, so Docker Desktop builds/runs native arm64 containers by default, and IronPDF's Linux Chrome engine simply doesn't support that architecture.

**Decision:** Stop pursuing a native-arm64 fix — there isn't one available from the vendor. Documented the two real options for later: (a) force the `api` service to build/run as `linux/amd64` via Docker Desktop's x64 emulation (`platform: linux/amd64` in `docker-compose.yml`), which works but requires pulling a separate set of x64 base images and Chrome binaries, or (b) accept arm64-only local `dotnet run` verification (which works fine — macOS arm64 IS supported by IronPDF) and defer containerized PDF verification to a real x64 deployment target (which is what production would use anyway). Did not attempt (a) given disk had just dropped to 1.7GB free from the preceding build attempts — user chose to leave Docker PDF verification unresolved for now rather than risk a third Docker storage corruption (see the two entries above and in Phase 3) chasing it under low disk headroom.

**Alternatives considered:** Switching to a different PDF library with better arm64 Docker support — rejected, same reasoning as the licensing decision above: IronPDF is CLAUDE.md's specified tech choice, not something to second-guess unilaterally over a workaround-able deployment-environment gap.

**Consequences:** On this specific dev machine, `dotnet run` (native macOS arm64) can render PDFs once a license key is added; the Dockerized `api` container cannot, until either `platform: linux/amd64` is configured (with adequate disk headroom to do it safely) or the container is actually deployed to an x64 host. Anyone resuming this: free real disk space first, then add `platform: linux/amd64` under the `api` service in `docker-compose.yml` before rebuilding.

---

## [Phase 5] CORS instead of an Angular dev-server proxy

**Context:** The Angular app (port 4200) and the API (port 5080) are different origins. Something has to bridge that, both for local `ng serve` iteration and for the Docker Compose scenario where the browser talks to `web` (nginx, port 4200) and `api` (port 5080) as genuinely separate containers.

**Decision:** Add an explicit CORS policy in `Program.cs` (`WithOrigins("http://localhost:4200")`, any header/method) rather than an Angular `proxy.conf.json`.

**Alternatives considered:** A dev-server proxy — rejected because it only solves the problem for `ng serve`; the Docker Compose full-stack scenario needs CORS regardless (browser-to-container requests are cross-origin there too), so CORS is the one mechanism that covers both cases instead of needing two.

**Consequences:** The allowed origin is hardcoded to `localhost:4200`. If the frontend is ever served from a different host/port (a real domain, a different dev port), this policy needs updating — worth revisiting once there's an actual deployment target.

---

## [Phase 5] Signals in services for state, not NgRx

**Context:** The Angular app needs to hold and react to server-fetched lists (customers, senders, invoices) across components.

**Decision:** Each API client service (`CustomerService`, `SenderService`, `InvoiceService`) owns a private writable `signal` per piece of state (`_customers`, `_loading`, `_error`, `_totalCount`), exposed read-only via `.asReadonly()`. Components inject the service and read the signals directly in templates — no separate store, no `@ngrx/store`.

**Alternatives considered:** NgRx (or another Redux-style store) — rejected as substantial ceremony (actions, reducers, effects, selectors) for what is, at this app's size, simple per-resource CRUD lists with no complex derived state, undo/redo, or cross-cutting concerns that would justify it. Signals plus `HttpClient` cover this fully and are the idiomatic modern-Angular answer for exactly this shape of problem.

**Consequences:** State is scattered across per-resource services rather than centralized in one store — fine at three resources; if the app's state interactions grow substantially more complex later (e.g. optimistic updates across multiple resources, undo), that's the signal (no pun intended) to reconsider.

---

## [Phase 5] Lazy-loaded standalone routes via `loadComponent`

**Context:** Three top-level routes (`/invoices`, `/customers`, `/senders`), each a standalone component with no NgModule.

**Decision:** `app.routes.ts` uses `loadComponent: () => import(...).then(m => m.X)` for each route rather than eagerly importing all three components into the main bundle.

**Alternatives considered:** Eager imports — simpler syntax, but means every route's code ships in the initial bundle even before the user navigates anywhere. The build output confirms the lazy chunks are real and separate (`invoices-page`, `customers-page`, `senders-page` each ~3KB as standalone chunks, not folded into `main`).

**Consequences:** None significant at this scale — noted mainly as the standalone-component equivalent of NgModule-based lazy loading, worth understanding since it's the default recommended pattern now.

---

## [Phase 5] Docker Desktop's VM disk corruption required a full purge; migrations must be applied manually to a fresh database

**Context:** Verifying Phase 5 against a real backend surfaced two things worth recording. First, Docker Desktop's internal storage became corrupted deeply enough that even its own containerd metadata database (`meta.db`) was unwritable — not fixable by restarting the daemon or removing individual containers, both of which also failed with I/O errors. This was the cumulative result of the disk-space crises from Phase 3/4 finally breaking Docker's VM disk image outright. Second, once Docker Desktop's own "Clean / Purge data" reset gave us a genuinely fresh Postgres volume, the API returned 500s (`relation "customers" does not exist`) until migrations were applied — there is no auto-migrate-on-startup logic in `Program.cs`, so `dotnet ef database update` has to be run by hand against any fresh database.

**Decision:** Did the full Docker Desktop purge (user-confirmed, since it wipes all local Docker state across every project) rather than trying to hand-repair the VM disk. Applied migrations manually via `dotnet ef database update --project src/InvoiceBuilder.Invoices --startup-project src/InvoiceBuilder.Api` against the fresh container.

**Alternatives considered:** Manually locating and deleting Docker Desktop's underlying VM disk file — rejected as riskier and less reversible than the built-in, supported purge path. Adding auto-migration-on-startup to `Program.cs` now — deferred rather than done reactively mid-verification; worth deciding deliberately in Phase 8 (Docker Compose integration) rather than bolting on under pressure, since auto-migrating in production is itself a real architectural choice (danger of concurrent migration races, no chance to review before schema changes apply) and not a default to reach for casually.

**Consequences:** Anyone who resets or freshly clones this project's Docker volumes must remember to run the `dotnet ef database update` command above before the API will serve any data — there is currently no automation for this. Flagged for a real decision (auto-migrate on startup vs. an explicit migration step in a deploy script) when Phase 8 is tackled.

---

## [Phase 6] Reactive Forms over template-driven forms

**Context:** Three CRUD forms are needed (Customer, Sender, Invoice), the last of which has a dynamic, variable-length list of line items with cross-field derived values (subtotal/tax/total).

**Decision:** Built all three with Angular's Reactive Forms (`FormBuilder`, `FormGroup`, `FormArray`) rather than template-driven (`ngModel`-based) forms.

**Alternatives considered:** Template-driven forms — workable for the simple Customer/Sender forms, but they have no equivalent of `FormArray` for a variable-length collection of controls, which the invoice line items require outright. Rather than mixing two form styles across the app (template-driven for two screens, reactive for one), used reactive forms everywhere for consistency. Reactive forms also keep validation logic in the component class instead of scattered across template attributes, which matters most for the invoice form's cross-field rules (due date ≥ invoice date, at least one line item).

**Consequences:** Slightly more boilerplate for the two simple forms (explicit `FormGroup` definitions instead of just `[(ngModel)]` bindings) in exchange for one consistent pattern across all three forms and a straightforward path to the invoice form's `FormArray`.

---

## [Phase 6] Client-side validation mirrors backend FluentValidation rules

**Context:** The backend already enforces validation via FluentValidation (`CustomerRequestValidator`, `InvoiceRequestValidator`, etc.) — required fields, max lengths, `Currency` matching `^[A-Z]{3}$`, `TaxRatePercent` in `[0,100]`, at least one line item.

**Decision:** Angular's built-in `Validators` (`required`, `maxLength`, `pattern`, `min`, `max`) reproduce the same constraints client-side, checked against the actual validator source files before writing the forms rather than guessing at the rules.

**Alternatives considered:** Skipping client-side validation and relying solely on the backend's 400 responses — rejected as poor UX (round-tripping to the server to learn a required field was blank). The backend validators remain the actual authority; the frontend copy is a UX convenience that can drift and would need to be re-synced if the backend rules change — worth remembering if either side is edited later.

**Consequences:** Two places now encode the same rules. Not a data integrity risk (the backend re-validates regardless), but a maintenance one: a future backend validation change (e.g., a new max length) won't automatically propagate to the frontend.

---

## [Phase 6] "View" and "Edit" collapse into one route

**Context:** The original screen spec lists "View, Edit, Delete" as three separate row actions per resource.

**Decision:** Built one form component per resource (`CustomerFormPage`, `SenderFormPage`, `InvoiceFormPage`) that serves both `/new` (empty, create mode) and `/:id/edit` (pre-populated via a `GET`, update mode). There is no separate read-only "View" page.

**Alternatives considered:** A distinct read-only detail page — rejected as pure duplication for entities this simple (a handful of scalar fields, no nested detail worth a richer read view). A pre-filled edit form already shows every field a view page would; the only difference would be disabling the inputs, which adds a mode flag and extra template branching for no real benefit at this project's current complexity. Worth revisiting if a resource later grows enough detail (e.g., an invoice's payment history) that a dedicated read view earns its keep.

**Consequences:** Row actions are just "Edit" and "Delete," not three. If a future requirement specifically needs a non-editable view (e.g., a read-only role), this decision would need revisiting.

---

## [Phase 6] Invoice live totals: `toSignal()` bridges `valueChanges` into a `computed()`

**Context:** The invoice form needs a live subtotal/tax/total panel that updates as line items, quantities, prices, or the tax rate change — a derived value over reactive-forms state, which is fundamentally RxJS (`form.valueChanges` is an `Observable`), not signals.

**Decision:** `toSignal(this.form.valueChanges, { initialValue: this.form.getRawValue() })` bridges the form's value stream into a signal once; `subtotal`, `taxAmount`, and `total` are then plain `computed()` signals derived from it — no manual subscription, no manual change detection.

**Alternatives considered:** Subscribing to `valueChanges` manually and calling a recalculation method imperatively — works, but reintroduces exactly the manual-subscription bookkeeping (unsubscribe on destroy, remembering to call it after every mutation) that signals exist to avoid. `toSignal()` handles the subscription lifecycle automatically and ties cleanly into the rest of the signal-based state already used throughout the app (Phase 5's services).

**Consequences:** None significant — this is the officially recommended interop pattern (`@angular/core/rxjs-interop`) for exactly this situation: a reactive-forms-driven derived value inside a component that otherwise uses signals. Verified live during E2E testing: two line items (2×$50, 1×$25) at a 10% tax rate correctly computed subtotal $125.00, tax $12.50, total $137.50 in the rendered panel before submission.

---

## [Phase 6 bugfix] Missing cross-field date validator caused a silent "Failed to save invoice"

**Context:** User-reported: saving an invoice sometimes just said "Failed to save invoice" with no explanation. Investigation (reproduced with a Playwright script before touching any code) found two things. First, the invoice form mirrored every backend `InvoiceRequestValidator` rule *except* `DueDate >= InvoiceDate` — that one has no client-side equivalent, so a due date picked before the invoice date sails through client validation, hits the backend's 400, and lands in the generic catch-all error handler. Second, that catch-all handler (`error: () => this.error.set('Failed to save invoice.')`, duplicated across all three forms) discarded the actual `ValidationProblemDetails` body the backend sends back, so even backend-only failures were invisible to the user. (A currency-case hypothesis — CSS `uppercase` only being cosmetic, not the real value — was also checked and ruled out: `Validators.pattern(/^[A-Z]{3}$/)` already blocks lowercase input client-side with a clear message, confirmed via the same reproduction approach.)

**Decision:** Added a `FormGroup`-level cross-field validator (`dueDateNotBeforeInvoiceDate`) to the invoice form with its own inline error message. Added a shared `extractErrorMessage()` helper (`shared/http-error.ts`) that reads the real messages out of `HttpErrorResponse.error.errors` when present, and wired it into all three forms' save-error handlers, replacing the generic strings.

**Alternatives considered:** None seriously — this was a straightforward gap-and-fix once reproduced, not a design decision with real trade-offs.

**Consequences:** Any *future* backend validation rule not yet mirrored client-side will now still show its real message instead of a dead end, because `extractErrorMessage()` is a general fallback — this class of bug shouldn't recur silently even if another mirroring gap is missed later. Confirmed fixed by rerunning the exact reproduction script: it now shows "Due date cannot be before the invoice date." before ever reaching the server.

---

## [Phase 7] PDF download flow: wired end-to-end, generation itself still blocked (as expected)

**Context:** Phase 7's scope, as originally split from Phase 4, was specifically "wire frontend Download PDF to backend" — the actual PDF rendering has been blocked since Phase 4 by two external issues (IronPDF needs a trial/license key; its Linux Chrome engine has no arm64 build), both already documented and deliberately deferred rather than worked around.

**Decision:** Built the full client-side flow regardless of the backend blocker: a "Download PDF" row action calling `InvoiceService.downloadPdf()` (already existed from Phase 5), a `downloadBlob()` helper that triggers a real browser file save from the response Blob, and per-row loading/error state. Along the way, fixed two things this surfaced:
1. The `/pdf` endpoint had no try/catch — an IronPDF failure was an *unhandled* exception, which in this environment meant the raw `DeveloperExceptionPageMiddleware` output (a multi-KB stack trace) was the literal HTTP response body. Wrapped it in `Results.Problem(...)` so any client gets clean, structured JSON instead.
2. Because the frontend request uses `responseType: 'blob'` (required to receive PDF bytes), Angular's `HttpErrorResponse.error` on failure is *also* a Blob, not parsed JSON — the existing `extractErrorMessage()` helper from the Phase 6 bugfix doesn't work on it. Added `extractBlobErrorMessage()`, an async variant that reads the Blob as text and parses it.
3. IronPDF's raw exception `.Message` for this specific failure is a genuinely enormous nested string (visibly confirmed via screenshot: several paragraphs of "Failed to locate X at Y" repeated many times). Truncated it server-side to 200 characters — accurate enough to identify the problem, without dumping an unreadable wall of red text onto the page.

**Alternatives considered:** Attempting to actually fix IronPDF's license/arm64 blockers as part of this phase — explicitly out of scope; those remain external, documented blockers to resolve separately (add a license key; or add `platform: linux/amd64` to the `api` service, per the Phase 4 entries), not something to route around under the banner of "wiring the download button."

**Consequences:** Clicking "Download PDF" today correctly shows a concise, real error ("Error while deploying IronPdf Chrome renderer: …") rather than either a silent failure or a raw stack trace — verified live via a Playwright script against the actual dockerized container (the failure takes roughly 20–25 seconds to resolve, since IronPDF retries multiple NuGet URLs before giving up; the UI's "Downloading…" state correctly holds through that whole window rather than timing out early). Once either blocker is resolved, this exact code path — no changes needed — will trigger a real PDF save in the browser.

---

## [Phase 8] Auto-migrate the database on API startup

**Context:** Flagged as deferred back in Phase 5: a fresh `docker compose up` (empty Postgres volume) left the API returning 500s until someone manually ran `dotnet ef database update`. This directly contradicts the project's own stated goal — "Docker Compose ... single `docker compose up`."

**Decision:** User-confirmed (asked explicitly, since this was flagged as worth deciding deliberately rather than defaulting silently). Added `InvoicesModule.MigrateInvoicesDatabase()` — resolves `InvoicesDbContext` from a fresh DI scope and calls `Database.Migrate()` — called once from `Program.cs` right after `app.UseCors()` and before the endpoint mappings.

**Alternatives considered:** Keeping migrations a manual/explicit step (a human or deploy script runs `dotnet ef database update` deliberately) — the safer long-term default, since auto-migrating on every boot is genuinely risky once there are multiple concurrent instances racing to apply the same migration, or once schema changes need a review window before they hit a real database. Rejected *for now*, explicitly because this project has no real deployment target yet and its own goal is single-command local convenience. **Revisit before any real production deployment** — at that point, prefer an explicit migration step in a deploy pipeline over auto-migrate-on-boot.

**Consequences:** Verified end-to-end: `docker compose down -v` (destroys the Postgres volume entirely) followed by `docker compose up -d --build` now leaves a fully-working stack with zero manual steps — confirmed by immediately creating a customer via the API and getting a real `201`, and confirming the five expected tables (`customers`, `senders`, `invoices`, `invoice_line_items`, `__EFMigrationsHistory`) exist without ever running `dotnet ef database update` by hand. Also benefits the integration test suite, which points at a separate `invoicebuilder_test` database (see `CustomWebApplicationFactory`) that previously needed the same manual migration step and now gets it automatically whenever `WebApplicationFactory<Program>` boots the app.

---

## [Phase 8] Docker Compose networking: browser-to-container vs container-to-container

**Context:** Worth writing down explicitly, since it's a common point of confusion when first working with Docker Compose. Three services exist: `postgres`, `api`, `web`. They talk to each other two genuinely different ways depending on *who* is making the call.

**Architecture Note:** Docker Compose puts all services on a shared internal network where each is reachable by its *service name* as a hostname — e.g. the `api` container's connection string is `Host=postgres;...`, and that resolves correctly because both containers are on the same Docker network, resolved via Docker's internal DNS. This is **container-to-container** traffic; it never touches the host machine's network stack at all.

The browser talking to the API is different in kind, not just address. `web`'s nginx only serves static files (the built Angular bundle) — it does not proxy API calls. The actual HTTP requests to `/api/...` are made by JavaScript running *in the user's browser*, on the host machine, entirely outside Docker's network. The browser has no idea `api` (the Docker service name) exists or how to resolve it — so `environment.ts`'s `apiBaseUrl: 'http://localhost:5080'` has to use the **host-published port** (`ports: ["5080:8080"]` in `docker-compose.yml`), not the internal service name or port. Hardcoding `http://api:8080` here would work for nothing — not local `ng serve`, not the dockerized `web` container — because in both cases the code executes in the browser, never inside the Docker network.

**Consequences:** This is why the CORS policy (`WithOrigins("http://localhost:4200")`, added in Phase 5) exists at all — from the browser's perspective, `localhost:4200` (the page it's on) and `localhost:5080` (the API it's calling) are different origins, full stop, regardless of the fact that both containers happen to live on the same Docker network under the hood.

---

## [Phase 8] Env var config: `ConnectionStrings__Default` and hardcoded local credentials

**Context:** `docker-compose.yml` sets `ConnectionStrings__Default` (double underscore) as an environment variable on the `api` service, and hardcodes Postgres credentials (`invoicebuilder` / `invoicebuilder`) directly in the compose file for both `postgres` and `api`.

**Architecture Note:** ASP.NET Core's configuration system treats `__` in an environment variable name as the nesting separator, equivalent to `:` in `appsettings.json` — so the env var `ConnectionStrings__Default` binds to the same configuration key as a `"ConnectionStrings": { "Default": "..." }` block in JSON, and env vars take precedence over `appsettings.json` by default in ASP.NET Core's provider ordering. This is how the same codebase runs with a different connection string locally (`appsettings.Development.json`, pointing at `localhost:5433`) versus inside Docker (the env var, pointing at `postgres:5432`) without any code branching.

**Decision:** Left the Postgres credentials hardcoded in `docker-compose.yml` rather than moving them to a `.env` file or secrets store.

**Alternatives considered:** A `.env` file (gitignored) or Docker secrets — the standard next step before any real deployment, where credentials in a committed YAML file would be a genuine problem. Not done now because these are throwaway local-dev-only credentials for a database that only ever runs on `localhost`, and adding secrets infrastructure for that would be unearned complexity at this stage.

**Consequences:** Fine as-is for local development. Revisit alongside the auto-migrate decision above before any real deployment — neither hardcoded credentials nor auto-migrate-on-boot belong in a production configuration.

---

## [Phase 9] Real bug found: editing an invoice's line items threw a 500

**Context:** Writing the first-ever integration test for `PUT /api/invoices/{id}` (there had been zero test coverage on any invoice endpoint besides the Playwright *create* flow from Phase 6 — updates were never exercised by anything, human or automated) immediately failed with `DbUpdateConcurrencyException: expected to affect 1 row(s), but actually affected 0 row(s)`. Reproduced independently against the live dockerized API via curl to rule out a test-infrastructure artifact — same failure, same exception, every time. **Any edit to an existing invoice was broken in production**, silently, since Phase 3.

**Root cause:** The endpoint replaced line items via `invoice.LineItems.Clear()` followed by `invoice.LineItems.Add(new InvoiceLineItem { Id = Guid.NewGuid(), ... })` for each new one — mutating the tracked navigation collection in place. Because the old and new collections happened to have the same count (2 line items in, 2 out), EF Core's automatic collection-fixup logic paired them up by position and treated it as *updating* the old rows' values to match the new ones — generating `UPDATE ... WHERE "Id" = <brand-new-guid>` for rows that don't exist yet, instead of `INSERT`. Zero rows matched, hence the concurrency exception. Confirmed by reading the actual generated SQL from the container logs, not by inspecting the code alone.

**Decision:** Rewrote the update path to go through the `InvoiceLineItems` `DbSet` directly — `db.InvoiceLineItems.RemoveRange(invoice.LineItems)` then `db.InvoiceLineItems.AddRange(newLineItems)` — which sets each entity's tracked state (`Deleted` / `Added`) explicitly rather than leaving EF to infer it from collection-membership diffing. `invoice.LineItems` is then reassigned directly (`invoice.LineItems = newLineItems`) so `RecalculateTotals()`, which reads that property, sees the new set.

**Alternatives considered:** None — once the root cause was clear from the generated SQL, this is simply the standard, correct way to replace a required one-to-many collection in EF Core. The first fix attempt (keeping `Clear()` but adding an explicit `RemoveRange` first) was tried and still failed identically, which is what led to reading the actual SQL rather than continuing to guess.

**Consequences:** This is the single most important reason to write integration tests for every endpoint, not just the ones that feel risky at the time — this bug lived in code that looked completely ordinary. Fixed and verified three ways: the new integration test passes, a direct curl repro against the live container now succeeds, and a Playwright pass through the real UI (edit an invoice's tax rate, save, confirm no error and a clean redirect) confirms the fix end-to-end.

---

## [Phase 9] xUnit runs test classes in parallel by default — disabled for this assembly

**Context:** Adding `SenderEndpointsTests` and `InvoiceEndpointsTests` (alongside the existing `CustomerEndpointsTests`) caused intermittent, non-deterministic failures — sometimes JSON parse errors, sometimes 500s — that didn't reproduce when running any single class in isolation.

**Decision:** Every integration test class wipes and recreates the entire `invoicebuilder_test` schema in `IAsyncLifetime.InitializeAsync`/`DisposeAsync`. xUnit parallelizes *across* test classes by default (though not within one), so with three classes sharing one physical database, one class's `EnsureDeletedAsync` could drop tables out from under another class's in-flight test. Added `[assembly: CollectionBehavior(DisableTestParallelization = true)]` in `CustomWebApplicationFactory.cs` to force all test classes in this assembly to run sequentially.

**Alternatives considered:** Giving each test class its own dedicated database name — more isolation, more true-to-production parallel execution, but adds real complexity (parameterizing `CustomWebApplicationFactory` per class, provisioning N databases) for a test suite that runs in a few seconds regardless of parallelism at this size. Not worth it yet; revisit if the suite grows large enough that sequential execution becomes a real time cost.

**Consequences:** The whole integration suite runs strictly sequentially now, which is slightly slower but deterministic. Any future integration test class added to this project inherits the same DB-wipe pattern safely without needing to remember this constraint.

---

## [Phase 9] Test coverage: what's covered, what's deliberately not

**Backend, added this phase:** Integration tests for `SenderEndpoints` (mirroring the existing `CustomerEndpoints` pattern) and `InvoiceEndpoints` (round-trip CRUD, totals correctness, the due-date cross-field rule, invalid-FK handling, sequential invoice numbering) — the highest-value gap, since it's what surfaced the real update bug above. Unit tests for `CustomerRequestValidator` and `SenderRequestValidator` (fast, no DB) covering required fields, max lengths, and email format edge cases that would be slow and noisy to enumerate via HTTP round-trips.

**Frontend, added this phase:** `CustomerService` and `InvoiceService` — signal-state-on-success/failure, correct URLs/params, the Blob-typed `downloadPdf()` request shape — directly validating the Phase 5 "signals in services" decision. `CustomerFormPage` — representative Reactive Forms validation coverage. `InvoiceFormPage` — the most valuable frontend test: a permanent regression guard for the due-date bug from the Phase 6 bugfix, plus verification that the `toSignal()` + `computed()` live-totals math is actually correct (125/12.50/137.50 for a known input, matching what was screenshotted during Phase 6). `Pager` — one example of testing a simple signal-input/output presentational component.

**Deliberately not duplicated, and why:** `SenderFormPage` (identical pattern to `CustomerFormPage`, same validators mirrored the same way); `SendersPage`/`CustomersPage`/`InvoicesPage` list components (identical list/paginate/delete-confirm pattern, demonstrated once); `ConfirmDialog` (a few lines of template, two outputs — not enough logic to warrant a dedicated spec); `SenderService` (identical shape to `CustomerService`). Retesting an already-demonstrated pattern adds repetition without proportionate new protection — the judgment call throughout this phase was to spend testing effort where the *pattern* was new or where a cross-field rule made a real regression genuinely possible, not to reach 100% file coverage for its own sake.

**Still not covered at all:** `Pager`/`ConfirmDialog` integration *within* a list page (only tested in isolation); the `IInvoiceNumberGenerator`'s year-rollover behavior (sequence resets each January — never exercised, since that would require manipulating the system clock in a test); any true concurrency scenario (two simultaneous edits to the same invoice); the `Users`/`Payments`/`Reports` module stubs (out of scope per the roadmap, which explicitly defers them). Worth being honest about these gaps rather than implying full coverage exists.

**Totals:** Backend went from 8 unit + 2 integration tests to 23 unit + 10 integration tests (1 skipped, unrelated — the IronPDF license/arm64 blocker from Phase 4). Frontend went from 2 tests (the app shell only) to 28 tests across 6 files.
