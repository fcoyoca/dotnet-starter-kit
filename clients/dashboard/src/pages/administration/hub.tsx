import { useEffect, useState } from "react";
import { useNavigate, useParams } from "react-router-dom";
import { Building2 } from "lucide-react";
import { useAuth } from "@/auth/use-auth";
import { EntityPageHeader } from "@/components/list";
import { AdminSectionDialog } from "@/pages/administration/admin-section-dialog";
import { ADMIN_HUB_SECTIONS } from "@/pages/administration/section-registry";
import { cn } from "@/lib/cn";

/**
 * Pure content component: the Administration header + permission-gated
 * 17-card grid. No routing hooks — where a card click "goes" is the
 * caller's decision (the global AdministrationDialogRoot switches its
 * view; the legacy routed page below navigates). Extracted so the grid
 * renders identically inside the global dialog (Part A).
 */
export function AdministrationHubGrid({
  onSelectSection,
}: {
  onSelectSection: (slug: string) => void;
}) {
  const { user } = useAuth();
  const perms = user?.permissions ?? [];
  const visibleCards = ADMIN_HUB_SECTIONS.filter((s) => !s.perm || perms.includes(s.perm));

  return (
    <div className="space-y-4 sm:space-y-6">
      <EntityPageHeader
        icon={Building2}
        title="Administration"
        total={visibleCards.length}
        unit="section"
        description="Clinic reference data — clinics, providers, diagnostics, drugs, and more. Choose a section to manage its records."
      />

      <div className="grid grid-cols-1 gap-3 sm:grid-cols-2 lg:grid-cols-3">
        {visibleCards.map((s) => {
          const Icon = s.icon;
          return (
            <button
              key={s.slug}
              type="button"
              onClick={() => onSelectSection(s.slug)}
              className={cn(
                "flex items-center gap-3 rounded-xl border border-[var(--color-border)] bg-[var(--color-card)] p-4 text-left",
                "transition-colors hover:bg-[var(--color-accent)]",
                "focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-[var(--color-ring)]",
              )}
            >
              <span className="grid size-10 shrink-0 place-items-center rounded-lg bg-[var(--color-primary-soft)] text-[var(--color-primary)]">
                <Icon className="size-5" />
              </span>
              <span className="text-[14px] font-medium">{s.label}</span>
            </button>
          );
        })}
      </div>
    </div>
  );
}

/** Routed page — RETIRED in Task 4 (replaced by AdminDeepLinkOpener).
 *  Kept compiling through Tasks 2-3 so each task lands green independently;
 *  behavior is unchanged from before this task. */
export function AdministrationHub() {
  const { section } = useParams<{ section?: string }>();
  const navigate = useNavigate();

  const [openSlug, setOpenSlug] = useState<string | null>(section ?? null);

  // Keep the open dialog in sync with the :section route param — covers
  // direct deep links (/administration/clinics) and browser back/forward.
  useEffect(() => {
    setOpenSlug(section ?? null);
  }, [section]);

  const openSection = (slug: string) => {
    setOpenSlug(slug);
    navigate(`/administration/${slug}`, { replace: true });
  };

  const closeSection = () => {
    setOpenSlug(null);
    navigate("/administration", { replace: true });
  };

  return (
    <>
      <AdministrationHubGrid onSelectSection={openSection} />
      <AdminSectionDialog slug={openSlug} onClose={closeSection} />
    </>
  );
}
