import { Suspense } from "react";
import { Dialog, DialogContent, DialogTitle } from "@/components/ui/dialog";
import { Skeleton } from "@/components/ui/skeleton";
import {
  ADMIN_HUB_SECTIONS,
  ADMIN_SECTION_COMPONENTS,
} from "@/pages/administration/section-registry";

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
 * Generic dialog shell — lazy-loads and renders an EXISTING Administration
 * page component (ClinicsPage, DepartmentsPage, etc.) inside a DialogContent
 * instead of routing to a bare page. Every admin page is already a
 * self-contained list+CRUD unit on shared EntityListCard/Dialog primitives,
 * so this is a thin wrapper, not a rewrite.
 */
export function AdminSectionDialog({
  slug,
  onClose,
}: {
  /** Section slug (e.g. "clinics"), or null when nothing should be open. */
  slug: string | null;
  onClose: () => void;
}) {
  const section = slug ? ADMIN_HUB_SECTIONS.find((s) => s.slug === slug) : undefined;
  const Section = slug ? ADMIN_SECTION_COMPONENTS[slug] : null;

  return (
    <Dialog open={slug != null} onOpenChange={(o) => (!o ? onClose() : undefined)}>
      <DialogContent className="!max-w-4xl overflow-hidden p-0">
        <DialogTitle className="sr-only">{section?.label ?? "Administration"}</DialogTitle>
        <div className="max-h-[85vh] overflow-y-auto p-6 pt-10">
          {Section && (
            <Suspense fallback={<SectionFallback />}>
              <Section />
            </Suspense>
          )}
        </div>
      </DialogContent>
    </Dialog>
  );
}
