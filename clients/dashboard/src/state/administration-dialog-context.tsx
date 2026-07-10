import { createContext, useCallback, useContext, useMemo, useState, type ReactNode } from "react";

/**
 * Which view the single global Administration dialog is showing.
 * Deliberately NOT persisted to localStorage — this is ephemeral UI state
 * (unlike patient-workspace-context.tsx's tabs, which model durable work).
 */
export type AdminDialogView =
  | { kind: "closed" }
  | { kind: "hub" }
  | { kind: "section"; slug: string };

type AdministrationDialogContextValue = {
  view: AdminDialogView;
  /** Open the dialog on the 17-card hub grid. */
  openHub: () => void;
  /** Open the dialog directly on one section (also used by hub cards + deep links). */
  openSection: (slug: string) => void;
  /** From a section, return to the hub grid without closing the dialog. */
  backToHub: () => void;
  close: () => void;
};

const AdministrationDialogContext = createContext<AdministrationDialogContextValue | null>(null);

/** Mount once in AppShell (see app-shell.tsx) wrapping the Sidebar, the
 *  mobile drawer, and the routed <Outlet/> — every trigger (sidebar button,
 *  mobile drawer button, AdminDeepLinkOpener route) is a descendant. */
export function AdministrationDialogProvider({ children }: { children: ReactNode }) {
  const [view, setView] = useState<AdminDialogView>({ kind: "closed" });

  const openHub = useCallback(() => setView({ kind: "hub" }), []);
  const openSection = useCallback((slug: string) => setView({ kind: "section", slug }), []);
  const backToHub = useCallback(() => setView({ kind: "hub" }), []);
  const close = useCallback(() => setView({ kind: "closed" }), []);

  const value = useMemo<AdministrationDialogContextValue>(
    () => ({ view, openHub, openSection, backToHub, close }),
    [view, openHub, openSection, backToHub, close],
  );

  return (
    <AdministrationDialogContext.Provider value={value}>
      {children}
    </AdministrationDialogContext.Provider>
  );
}

export function useAdministrationDialog(): AdministrationDialogContextValue {
  const ctx = useContext(AdministrationDialogContext);
  if (!ctx)
    throw new Error("useAdministrationDialog must be used within AdministrationDialogProvider");
  return ctx;
}
