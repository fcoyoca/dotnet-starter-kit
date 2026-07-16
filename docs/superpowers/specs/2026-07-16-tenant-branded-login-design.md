# Per-Tenant Branded Login

**Date:** 2026-07-16
**Branch:** `clinic-app`
**Status:** Approved — ready for implementation plan

## Problem

Clinic staff sign in to a page branded `fullstackhero`. It should carry their own
clinic's name and logo, and the browser tab should identify their clinic rather
than the framework. The framework default name itself is wrong: it should read
**FRC Clinic Solution**.

A branding pipeline already exists and is not the gap:

- `GET /api/v1/tenants/me/branding` → `TenantBrandingDto` (`AppName`, `LogoUrl`,
  `LogoDarkUrl`, `FaviconUrl`).
- `ITenantThemeService.GetThemeAsync(tenantId, ct)` stores per-tenant brand assets.
- `BrandingProvider` (`clients/dashboard/src/components/branding/branding-context.tsx`)
  consumes it and feeds the app chrome.

**The actual gap is structural.** `GetMyBrandingEndpoint` resolves the tenant *from
the caller's token*, and `BrandingProvider` fetches with `enabled: isAuthenticated`.
Branding is therefore reachable only *after* login. A branded login page has no
token and no resolved tenant. Everything in this spec follows from closing that one
gap; the rest is wiring.

## Decisions

| Decision | Choice | Why |
|---|---|---|
| Login flow | Two-step: `/login` (code) → `/login/{code}` (branded) | User's explicit choice. |
| Enumeration posture | Serve branding, 404 on unknown, rate limit | Tenant codes treated as guessable-but-not-secret, same posture as Slack/Okta workspace URLs. Staff already know their code. |
| App scope | Dashboard branded; admin renamed only | Admin is an operator console spanning all tenants; branding it as one clinic would misrepresent it. |
| Remembered code | localStorage + "Switch clinic" escape | Two-step otherwise imposes daily friction on every sign-in. |
| Tab identity | Title **and** favicon follow tenant | `faviconUrl` already exists on the DTO and is currently unused. |
| Logo assets | Text wordmark now; images supplied later | No FRC logo exists in the repo. Cannot be invented. |
| Rate limiting | New dedicated `public-read` policy | Correct limit for a page-load endpoint. **Explicitly approved to modify `src/BuildingBlocks`** (see below). |
| Subdomains | Two-step now; subdomain as an additive follow-up | Subdomain is the better end state and the backend already supports it, but it drags in TLS, proxy, Aspire and Playwright work. The two-step page is the discovery tier that survives either way. See "Subdomain follow-up". |

### Golden-rule exception — recorded

Golden rule #4 protects `src/BuildingBlocks`. The user **explicitly approved** the
BuildingBlocks change described in "Backend — rate limiting" on 2026-07-16. The
change is strictly additive (a new named policy); no existing policy or limiter
behaviour is altered. This approval covers that change only.

## Architecture

### Backend — anonymous branding endpoint

New: `GET /api/v1/tenants/{identifier}/branding`, anonymous.

Feature folder `Modules.Multitenancy/Features/v1/GetTenantBranding/`, mirroring the
existing `GetMyBranding` slice.

- Resolves the tenant with `IMultiTenantStore<AppTenantInfo>.TryGetByIdentifierAsync`.
  This deliberately does **not** rely on Finbuckle's ambient tenant resolution — no
  tenant header exists pre-login, which is the whole point of the endpoint.
- Reuses `ITenantThemeService.GetThemeAsync` and returns the existing
  `TenantBrandingDto`. No new DTO, no entity change, no migration.
- Applies the same `AppName` fallback as `GetMyBrandingEndpoint.cs:43-48`: theme
  `AppName` → tenant `Name` → null; Root returns null (Root is the framework's own
  tenant, not a clinic).
- Unknown identifier → `404`. Inactive/expired tenant → `404` (do not brand a tenant
  that cannot be signed into).
- Returns **only** name + logo URLs. Nothing is added to this payload that
  enumeration would make more valuable — no user counts, no contacts, no status.

Cross-cutting: this endpoint must be reachable **before** tenant resolution
middleware would otherwise reject an unresolvable tenant. Verify placement against
the middleware order in `.agents/rules/architecture.md` during implementation — this
is the most likely integration snag.

### Backend — rate limiting (BuildingBlocks, approved)

The existing chained global limiter already caps anonymous traffic at 300/min per IP,
which is a weak enumeration barrier. The existing `auth` policy (10/min) is too tight:
a clinic behind a single NAT IP would 429 its own staff on the login page.

Additive change:

- `RateLimitingOptions.cs` — add `PublicRead` (`PermitLimit = 60`, `WindowSeconds = 60`,
  `QueueLimit = 0`).
- `RateLimiting/Extensions.cs` — add a `public-read` named policy partitioned by IP,
  alongside the existing `AddAuthPolicy`.
- Endpoint: `.RequireRateLimiting("public-read")`.

No existing policy, partition, or limiter behaviour changes.

### Frontend — routing and the two-step flow

| Route | Renders |
|---|---|
| `/login` | Step 1 — tenant code only |
| `/login/:tenantCode` | Step 2 — branded card, email + password |

- **Step 1 submit:** probe the branding endpoint. `200` → navigate `/login/{code}`
  (branding already in the Query cache, so step 2 paints branded with no second
  request). `404` → inline error: "We don't recognize that clinic code."
- **Deep link** to `/login/acme` works directly. Unknown code → redirect to `/login`
  carrying the error.
- **Remembered code:** `localStorage["fsh.lastTenant"]`. `/login` with a stored code
  auto-forwards to `/login/{code}`. The branded page shows "Not {AppName}? Switch
  clinic", which clears the key and returns to step 1. Written on successful login
  only — never on a failed attempt, so a typo'd code is never persisted.
- **Clearing:** cleared on explicit sign-out; **kept** on inactivity timeout (the same
  person is coming back). The existing `consumeSignedOutReason()` notice continues to
  render on step 2.
- `login()` takes the code from the route; the tenant text field disappears from the
  credentials form. `env.defaultTenant` remains the step-1 prefill.

- **Demo picker:** `DemoAccountsDialog` accounts each carry their own tenant. It stays
  on step 1 and bypasses the two-step (picking an account signs in directly), since
  each demo account already specifies its tenant. Gated on `demoMode` as today.

**Forward compatibility — single tenant-code seam.** Subdomains are a planned
follow-up (below) whose *only* difference is where the code comes from. So the code
must be read through **one** resolver — `resolveTenantCode()` — rather than each
component reaching for `useParams()` directly. Today it returns route param →
localStorage. Adding subdomains later means adding a hostname branch inside that one
function. Written this way now, that follow-up is a handful of lines; written the
obvious way, it is a refactor across every consumer.

### Frontend — branding context

Generalize `BrandingProvider` to source branding from **either** the authed endpoint
or the anonymous one keyed by the route code, exposing the same `Branding` shape so
`AuthShell` and the app chrome consume it unchanged:

- Drop `enabled: isAuthenticated`.
- Authenticated → `/tenants/me/branding` (unchanged behaviour).
- Unauthenticated with a route code → `/tenants/{code}/branding`.
- Unauthenticated with no code → FRC defaults.
- `staleTime` stays 5 minutes; `retry: false` stays.

`isDefaultName` exists solely to preserve the `fullstack`·`hero` two-tone wordmark
split. "FRC Clinic Solution" has no such split, so the flag **and** the split
rendering both retire. `DEFAULT_APP_NAME` becomes `"FRC Clinic Solution"`; the
monogram default becomes `"F"` (unchanged letter, different word).

### Frontend — tab identity

A `useDocumentBranding` hook sets `document.title` and swaps the favicon `<link>`
`href` when branding resolves.

- Title: `"{appName} - Dashboard"`, falling back to `"FRC Clinic Solution - Dashboard"`.
- Favicon: `faviconUrl` when set, else the shipped default.
- Static `<title>` in both `index.html` files → the FRC default (this is what shows
  pre-resolution, so it must not say fullstackhero).

### Rename inventory

Dashboard and admin both. Text only — image files unchanged this round.

- `dashboard/src/components/branding/branding-context.tsx` — `DEFAULT_APP_NAME` →
  `"FRC Clinic Solution"`; `DEFAULT_LOGO_URL` **removed entirely** (its only value was
  the FSH image path, and `DEFAULT_BRANDING.logoUrl` is already `null`); retire
  `isDefaultName`.
- `dashboard/src/components/auth/auth-shell.tsx` — brand lockup, the
  `.NET 10 Starter Kit` caption, and the `fullstackhero Administration` footer.
- `admin/src/components/brand-mark.tsx` — `BrandMark` + `BrandMarkXL` wordmarks.
- `admin/src/pages/login.tsx`, `admin/src/pages/auth/*.tsx` — wordmark references.
- Both `index.html` `<title>`s.
- `globals.css`, `README.md` references in both apps.

**Deferred, needs your assets:** `public/logo-fullstackhero.png`, `favicon.ico`,
`favicon-16x16.png`, `favicon-32x32.png` in **both** `clients/dashboard/public/` and
`clients/admin/public/`. Until supplied, the wordmark renders as text + monogram and
the FSH logo image is dropped from the lockup rather than shown beside the FRC name.

## Testing

**Backend** (`src/Tests/Multitenancy.Tests`, plus integration):

- Known tenant → 200 with name + logos.
- Unknown identifier → 404.
- Inactive/expired tenant → 404.
- Root → 200 with null `AppName` (stays unbranded).
- Theme `AppName` overrides tenant `Name`; tenant `Name` used when unset.
- Anonymous access succeeds (no token required).

**Frontend** (`clients/dashboard/tests/auth/login.spec.ts`, route-mocked):

- Step 1 → valid code → branded step 2 shows tenant name + logo.
- Unknown code → inline error, stays on step 1.
- Deep link `/login/acme` paints branded.
- Deep link to unknown code → redirected to step 1 with error.
- Remembered code auto-forwards; "Switch clinic" clears and returns to step 1.
- Failed login does not persist the code.
- Tab title reflects tenant; falls back to FRC default.
- Demo picker still signs in (dashboard, `demoMode: true`).

Admin's `tests/auth/login.spec.ts` needs updating only for the rename.

## Subdomain follow-up (planned, not this change)

Subdomain-per-tenant (`acme.clinic.com`) is the industry-standard end state — Slack,
Zendesk, Okta, Atlassian all use it. All of them **also** keep a root-domain discovery
page (`slack.com/signin` → "find your workspace" → redirect to `acme.slack.com`). The
two-step page in this spec **is** that discovery tier. The two are layers, not
alternatives, so nothing here is throwaway.

Findings from investigating this on 2026-07-16 (recorded so the follow-up doesn't
re-derive them):

- **No backend change is needed.** `MultitenancyModule.cs:99-110` resolves tenant via
  claim → header → query param, and the code comment is explicit that resolution is
  header-driven (`UseMultiTenant()` runs before `UseAuthentication()`, so the claim
  strategy no-ops). `api-client.ts:182-184` sources that header client-side. A
  subdomain is simply a different client-side source for a header already being sent.
  No `WithHostStrategy`, no new resolution path, no migration.
- **The anonymous branding endpoint in this spec is required either way.** A hostname
  identifies *which* tenant; it does not supply the name or logo. `acme.clinic.com`
  still fetches branding with no token before first paint.
- **One wildcard cert covers it** (`*.clinic.com`) — not per-subdomain certs. Per-host
  certs only arise for custom domains (`portal.acmedental.com`), a separate feature.
- **CORS is likely a non-issue**: `config.json` ships `apiBase: ""` (same-origin), and
  prod `AllowedHeaders` omits `tenant`, which corroborates same-origin. Confirm against
  the real deploy before relying on this — prod `AllowedOrigins` is `[]` in the repo
  and supplied by environment variables.
- **Security upside**: `localStorage` is per-origin, so subdomains isolate tokens
  between tenants on shared clinic workstations. Today all tenants share one origin and
  one token store.

Real costs, which is why it is deferred: wildcard TLS, a proxy rule serving the bundle
for any `*.clinic.com`, the Aspire local-dev story, reworking Playwright for multiple
origins, tenant-aware links in password-reset/invite emails, and the fact that tenant
codes become permanent public URLs (renaming a code breaks bookmarks).

## Out of scope

- Per-tenant branding on the admin console.
- Opt-in per-tenant branding visibility toggle.
- Tenant-specific colour palettes on the login page (branding is name + logo only).
- Producing FRC logo/favicon image assets.

## Follow-up (per golden rule #10)

This is a user-facing change (new endpoint + login flow). The separate docs repo
(`github.com/fullstackhero/docs`) and a changelog entry under
`src/content/docs/changelog/` must be updated before this is done.

## Risks

- **Middleware order** — the anonymous endpoint must sit ahead of tenant-resolution
  rejection. Most likely integration snag; verify against `architecture.md`.
- **Shared-workstation leak** — a remembered code reveals which clinic last used a
  shared machine. Accepted: the code is not a secret, and "Switch clinic" is visible.
- **Enumeration** — accepted and rate-limited, per decision above.
