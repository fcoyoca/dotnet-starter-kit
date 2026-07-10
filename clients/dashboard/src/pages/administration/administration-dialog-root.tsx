import { Suspense } from "react";
import { ArrowLeft } from "lucide-react";
import { Dialog, DialogContent, DialogTitle } from "@/components/ui/dialog";
import { Skeleton } from "@/components/ui/skeleton";
import { useAdministrationDialog } from "@/state/administration-dialog-context";
import {
  ADMIN_HUB_SECTIONS,
  ADMIN_SECTION_COMPONENTS,
} from "@/pages/administration/section-registry";
import { AdministrationHubGrid } from "@/pages/administration/hub";

function SectionFallback() {
  return (
    <div className="space-y-4 p-2" role="status" aria-busy="true">
      <span className="sr-only">Loading…</span>
      <Skeleton className="h-8 w-48" />
      <Skeleton className="h-40 w-full rounded-xl" />
    </div>
  );
}

/**
 * THE Administration dialog — mounted once in AppShell (Task 3), layered
 * over whatever page is behind it, which stays mounted untouched. One
 * Radix Dialog whose content switches between the hub grid and a lazy
 * section page (the same components section-registry.ts always backed),
 * so hub → section → back never re-opens the overlay. Absorbs the old
 * AdminSectionDialog's lazy-load-inside-DialogContent logic (that file is
 * deleted in Task 4 once nothing routes to the hub page).
 */
export function AdministrationDialogRoot() {
  const { view, openSection, backToHub, close } = useAdministrationDialog();

  const section =
    view.kind === "section" ? ADMIN_HUB_SECTIONS.find((s) => s.slug === view.slug) : undefined;
  const Section = view.kind === "section" ? (ADMIN_SECTION_COMPONENTS[view.slug] ?? null) : null;

  return (
    <Dialog open={view.kind !== "closed"} onOpenChange={(o) => (!o ? close() : undefined)}>
      <DialogContent className="!max-w-4xl overflow-hidden p-0">
        <DialogTitle className="sr-only">{section?.label ?? "Administration"}</DialogTitle>
        <div className="max-h-[85vh] overflow-y-auto p-6 pt-10">
          {view.kind === "hub" && <AdministrationHubGrid onSelectSection={openSection} />}
          {view.kind === "section" && (
            <div className="space-y-4">
              <button
                type="button"
                onClick={backToHub}
                className="inline-flex cursor-pointer items-center gap-1.5 text-[13px] text-[var(--color-muted-foreground)] transition-colors hover:text-[var(--color-foreground)]"
              >
                <ArrowLeft className="size-4" />
                Administration
              </button>
              {Section ? (
                <Suspense fallback={<SectionFallback />}>
                  <Section />
                </Suspense>
              ) : (
                <p className="text-[13px] text-[var(--color-muted-foreground)]">
                  Unknown administration section.
                </p>
              )}
            </div>
          )}
        </div>
      </DialogContent>
    </Dialog>
  );
}
