# Design: Per-Provider Signature + Reusable Signature Editor

**Date:** 2026-06-26
**Branch:** clinic-app
**Status:** Approved (brainstorming)
**Amends:** Patient Reports sprint plan (`~/.claude/plans/jolly-bouncing-crescent.md`), task R12 and ripple into R6/R8/R11.

## Problem

The Patient Reports sprint originally specced **R12** as a per-report canvas draw-pad
(`SignaturePad`): a clinician draws a fresh signature each time they sign a report, stored on the
`PatientReport`. That is not how BackChart works and not what we want.

In BackChart, a signature is a **property of the user/provider**. Administration → Users shows a
signature preview with a "Change Signature Image" button that opens a dialog with an **image editor**
(`ImageCropperDialog` → `Cropper`): the user uploads an image and crops/resizes it to the signature
box (~180×30 px), and the cropped image becomes that user's reusable signature
(`UserSettings.signaturePath`). When a report is signed, the provider's saved signature is applied.

We want clinic-app to follow this model, with one addition: the editor should support **both**
uploading-and-cropping an image **and** drawing a signature on a canvas.

## Goals

1. A signature is stored **per provider/clinician** (on the Administration `Provider` entity), set once
   via a reusable editor in the provider's admin record.
2. The reusable editor is a dialog supporting **two modes**: Upload+crop and Draw. Either produces a
   PNG. It is purely presentational — it emits a base64 PNG and the parent persists it.
3. When a report is **signed** (R6) or **review-signed** (R8), the system **snapshots** the relevant
   provider's saved signature image onto the report, so the report holds an immutable record of the
   signature as it was at signing time.
4. Respect module boundaries (golden rule #1): the Patient module reads the provider's signature only
   through `Modules.Administration.Contracts`, never the Administration runtime project.

## Non-goals

- No per-report ad-hoc signature override. The signature used on a report is always the provider's
  saved signature (or none).
- The editor is **not** surfaced in the report Sign flow — only in the provider admin edit dialog.
- No change to the report attestation/workflow semantics beyond where the signature image comes from.
- Everything in the Patient Reports plan other than R6/R8/R11/R12 is unchanged.

## Decisions (resolved during brainstorming)

| Question | Decision |
|---|---|
| Where does the signature live / how used? | **Saved per provider, reused on sign.** Stored on `Provider`; snapshotted onto the report at sign time. |
| Editor capabilities | **Upload + crop AND draw** (two tabs). |
| Signature required at sign? | **Optional.** Signing succeeds (attestation only) even if the provider has no saved signature. |
| Where does the editor appear? | **Provider admin edit dialog only** (edit mode — needs a saved provider id; uploads immediately, like BackChart). |
| Cross-module read | Patient Sign handler sends `GetProviderByIdQuery` via Mediator; Patient runtime project gains a reference to `Modules.Administration.Contracts`. |

## Architecture

### Backend — Administration module (new work, inserted before R11)

**A1 — Provider signature storage**
- `Provider` domain entity: add `SignatureImagePath` (string?, nullable) and methods
  `SetSignature(string path)` / `ClearSignature()`.
- EF: `ProviderConfiguration` maps the column (nullable, reasonable max length, e.g. 512).
- Migration `AddProviderSignature` into `FSH.Starter.Migrations.PostgreSQL/Administration`.
- Contracts `ProviderDto`: add `SignatureImagePath` (string?) and a derived `SignatureImageUrl`
  (string?, built via `IStorageService.BuildPublicUrl(path)` in the query handler — null when no path).
- `GetProviderByIdQuery` and `ListProvidersQuery` project both new fields.

**A2 — Set / clear signature commands + endpoints**
- `SetProviderSignatureCommand(Guid ProviderId, string ImageBase64)`:
  - Validator: `ProviderId` non-empty, `ImageBase64` non-empty.
  - Handler: decode the base64 PNG (strip any `data:image/png;base64,` prefix), wrap in a
    `FileUploadRequest`, call `IStorageService.UploadAsync(req, FileType.Image, ct)` with key
    `administration/providers/{ProviderId}/signature.png`, set `Provider.SignatureImagePath`, save.
    Returns the stored path (or the public URL).
- `ClearProviderSignatureCommand(Guid ProviderId)`:
  - Handler: if a path exists, `IStorageService.RemoveAsync(path, ct)`; `Provider.ClearSignature()`; save.
- Permission: reuse `AdministrationPermissions.Providers.Update` (no new permission).
- Endpoints, wired in the Administration endpoint group:
  - `PUT  /api/v1/administration/providers/{id}/signature`
  - `DELETE /api/v1/administration/providers/{id}/signature`
  - Literal `/signature` sub-routes registered before any generic `/{id}` routes.

### Backend — Patient module (revisions)

**R6 — Sign command (revised)**
- `SignPatientReportCommand(Guid ReportId)` — **no `SignatureImageBase64` parameter**.
- Handler:
  - Resolves signing user from `ICurrentUser` (unchanged: `SignedByUserId`, `SignedByName`,
    `SignedOnUtc`, `WorkflowStatus = Signed`).
  - If `report.ProviderId` is set, sends `GetProviderByIdQuery(report.ProviderId)` via Mediator and
    snapshots `ProviderDto.SignatureImagePath` onto `PatientReport.SignatureImagePath`. If the provider
    has no signature, leaves `SignatureImagePath` null. Signing still succeeds.
- `Modules.Patient` runtime project gains a project reference to `Modules.Administration.Contracts`.
  No new Mediator assembly marker is required — the `GetProviderByIdQuery` handler lives in the
  already-registered Administration module.

**R8 — Review-sign command (revised)**
- `ReviewSignReportCommand(Guid ReportId)` — same pattern, using `report.ReviewerProviderId` to
  snapshot `ReviewSignatureImagePath`. Reviewer attestation from `ICurrentUser`.
- `RequestReportReviewCommand` unchanged.

The `PatientReport` domain `Sign(...)` / `ReviewSign(...)` method signatures change from accepting a
caller-supplied `imagePath?` to accepting the snapshot path resolved by the handler (semantically the
same — the handler now sources the path from the provider rather than the client).

### Frontend — dashboard

**R12 — Reusable `SignatureEditor` dialog component**
- File: `clients/dashboard/src/components/ui/signature-editor.tsx`.
- A Radix `Dialog` with two tabs:
  - **Upload**: file `<input type=file accept="image/*">` → crop/resize box targeting ~180×30 px,
    aspect kept → export PNG base64.
  - **Draw**: canvas pad with a **Clear** button → export PNG base64.
- Props:
  ```ts
  type SignatureEditorProps = {
    open: boolean;
    onOpenChange: (open: boolean) => void;
    value?: string | null;        // current signature image URL, for preview
    onSave: (pngBase64: string) => void;  // emits "data:image/png;base64,..." or bare base64
    targetWidth?: number;          // default 180
    targetHeight?: number;         // default 30
  };
  ```
- Purely presentational: it does not call the API. The parent owns persistence.
- Library choice (recommendation): `react-cropper` (cropperjs wrapper) for the Upload tab;
  a small custom canvas implementation (or `signature_pad`) for the Draw tab. Final lib choice is
  confirmed at plan/implementation time; both are small and Vite-compatible.

**R11 (extended) — admin wiring**
- `clients/dashboard/src/api/administration.ts`:
  - Add `signatureImagePath` and `signatureImageUrl` to `ProviderDto`.
  - Add `setProviderSignature(providerId, imageBase64)` → `PUT .../providers/{id}/signature`.
  - Add `clearProviderSignature(providerId)` → `DELETE .../providers/{id}/signature`.
- `clients/dashboard/src/pages/administration/providers.tsx`, `ProviderEditorDialog`:
  - In **edit mode only**, add a **Signature** field: a preview `<img>` (from
    `provider.signatureImageUrl`) + an "Add Signature Image" / "Change Signature Image" button (label
    depends on whether a signature exists) that opens `SignatureEditor`.
  - On `onSave`, call a `setProviderSignature` mutation (passing the base64 via `mutate(arg)`, never
    closed-over state — frontend golden rule #9), then invalidate the providers query / refetch the
    provider so the preview updates. Optional "Remove" action calls `clearProviderSignature`.

The report editor / view (R14/R16) renders `report.signatureImageUrl` (built from the snapshotted
`SignatureImagePath`) read-only — no editor there.

## Data flow

```
Admin sets signature:
  Provider edit dialog
    -> SignatureEditor (upload+crop | draw) -> PNG base64
    -> setProviderSignature(providerId, base64)
    -> PUT /providers/{id}/signature
    -> IStorageService.UploadAsync(Image, administration/providers/{id}/signature.png)
    -> Provider.SignatureImagePath set

Sign a report:
  PUT /reports/{id}/sign  (no image in body)
    -> SignPatientReportCommand handler
    -> ICurrentUser -> SignedBy*
    -> GetProviderByIdQuery(report.ProviderId)  [cross-module via Contracts]
    -> snapshot ProviderDto.SignatureImagePath -> PatientReport.SignatureImagePath
  Report view renders report.signatureImageUrl read-only.
```

## Error handling

- `SetProviderSignatureCommand`: invalid/empty base64 → validation failure (400). Upload size/extension
  enforced by `FileTypeMetadata` for `FileType.Image` (5 MB cap). PNG only is expected; the editor
  always exports PNG.
- `ClearProviderSignatureCommand`: idempotent — no-op if no signature; tolerate a missing blob on
  `RemoveAsync`.
- Sign with a provider that has no signature → succeeds with `SignatureImagePath = null`.
- Sign with `ProviderId = null` → succeeds with `SignatureImagePath = null` (no cross-module call).
- Cross-module query failure (provider not found / deleted) → sign still succeeds with null signature;
  do not fail the sign over a missing signature.

## Testing

- **Backend:** validator + handler coverage for `SetProviderSignatureCommand` /
  `ClearProviderSignatureCommand`; Sign handler test asserting the provider's `SignatureImagePath` is
  snapshotted onto the report (and null-safe when absent). Follows `testing.md` conventions
  (`public sealed`, `ValueTask`, validators present).
- **Frontend (R18):** Playwright route-mocked flow extended to cover: set a provider signature in admin
  → create/fill a report → sign → report view shows the provider's signature image.
- **Verification additions:** in the plan's manual walkthrough, add "Administration → Providers → edit a
  provider → set a signature (upload+crop and draw both work) → sign a report under that provider →
  report shows that provider's signature."

## Module boundary check

- Patient → `Modules.Administration.Contracts` project reference is the **sanctioned** cross-module
  path (golden rule #1; `Architecture.Tests` only forbids referencing the runtime project).
- No new Mediator markers and no `moduleAssemblies` changes — sending an existing query from a new
  consumer does not require registering anything new.

## Impact on the plan task list

- **Insert** A1, A2 (Administration) before R11.
- **Revise** R6, R8 (drop per-report image; snapshot provider signature via Contracts query).
- **Revise** R11 (add provider-signature API fns + `ProviderDto` fields + provider-dialog wiring).
- **Replace** R12 (draw-only `SignaturePad` → upload-crop-or-draw `SignatureEditor`).
- **Unchanged:** R1–R5, R7, R9, R10, R13–R18 (R14/R16 simply render the snapshotted signature URL).
- **Docs/changelog** (golden rule #10): note provider signatures + reusable editor.
