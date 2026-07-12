/**
 * Per-report sessionStorage drafts — unsaved report-editor typing survives
 * switching report tabs, navigating away, and same-tab reloads (Part C).
 * Per-tab and never written to disk: drafts are gone when the browser tab
 * closes. Local-only by design (matches BackChart, which also never
 * auto-saved to the server); the draft for a report is cleared the moment a
 * Save/Sign succeeds, so a stale draft can never shadow newer server data.
 *
 * Keyed purely by reportId — deliberately NOT part of
 * patient-workspace-context.tsx (drafts don't need to know which patient
 * tab they belong to, and the workspace context shouldn't grow an
 * unrelated responsibility).
 *
 * Storage conventions match patient-workspace-context.tsx: fsh.dashboard.*
 * namespaced + versioned key, `typeof window` guard, try/catch on every
 * read/write.
 */

const STORAGE_KEY = "fsh.dashboard.reportDrafts.v1";

/** Debounce for draft writes on field changes (see report-editor-panel.tsx). */
export const DRAFT_WRITE_DEBOUNCE_MS = 500;

/**
 * Snapshot of the panel's editable form state — header fields, vitals
 * (kept as the input strings the panel holds, not parsed numbers), and the
 * per-field text map. Excludes state owned by separate flows (addendum
 * composer, reviewer picker, associated-problem checkboxes) — those save
 * through their own mutations and are not part of the Save Draft payload.
 */
export type ReportDraft = {
  reportDate: string;
  providerId: string | null;
  clinicId: string | null;
  isNoShow: boolean;
  height: string;
  weight: string;
  systolic: string;
  diastolic: string;
  pulse: string;
  temperature: string;
  /** report-field id → text (mirrors the panel's `values` map). */
  values: Record<number, string>;
};

type DraftMap = Record<string, ReportDraft>;

function isReportDraft(value: unknown): value is ReportDraft {
  if (!value || typeof value !== "object") return false;
  const v = value as Record<string, unknown>;
  return (
    typeof v.reportDate === "string" &&
    (v.providerId === null || typeof v.providerId === "string") &&
    (v.clinicId === null || typeof v.clinicId === "string") &&
    typeof v.isNoShow === "boolean" &&
    typeof v.height === "string" &&
    typeof v.weight === "string" &&
    typeof v.systolic === "string" &&
    typeof v.diastolic === "string" &&
    typeof v.pulse === "string" &&
    typeof v.temperature === "string" &&
    typeof v.values === "object" &&
    v.values !== null
  );
}

function readAll(): DraftMap {
  if (typeof window === "undefined") return {};
  try {
    const raw = window.sessionStorage.getItem(STORAGE_KEY);
    if (!raw) return {};
    const parsed = JSON.parse(raw) as unknown;
    if (!parsed || typeof parsed !== "object") return {};
    const out: DraftMap = {};
    for (const [id, draft] of Object.entries(parsed as Record<string, unknown>)) {
      if (isReportDraft(draft)) out[id] = draft;
    }
    return out;
  } catch {
    return {};
  }
}

function writeAll(map: DraftMap): void {
  if (typeof window === "undefined") return;
  try {
    window.sessionStorage.setItem(STORAGE_KEY, JSON.stringify(map));
  } catch {
    /* storage unavailable (private browsing / quota) — drafts stay in-memory only */
  }
}

/** The stored draft for a report, or null when none exists. */
export function readReportDraft(reportId: string): ReportDraft | null {
  return readAll()[reportId] ?? null;
}

export function writeReportDraft(reportId: string, draft: ReportDraft): void {
  const map = readAll();
  map[reportId] = draft;
  writeAll(map);
}

/** Remove a report's draft (called after a successful Save/Sign — the
 *  server now holds the authoritative value). Idempotent. */
export function clearReportDraft(reportId: string): void {
  const map = readAll();
  if (!(reportId in map)) return;
  delete map[reportId];
  writeAll(map);
}
