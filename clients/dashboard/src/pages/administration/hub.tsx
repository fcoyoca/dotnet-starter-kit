import { useEffect, useState } from "react";
import { useNavigate, useParams } from "react-router-dom";
import { Building2 } from "lucide-react";
import { useAuth } from "@/auth/use-auth";
import { EntityPageHeader } from "@/components/list";
import { AdminSectionDialog } from "@/pages/administration/admin-section-dialog";
import { ADMIN_HUB_SECTIONS } from "@/pages/administration/section-registry";
import { cn } from "@/lib/cn";

/**
 * Administration hub — a grid of the 17 existing Administration sections.
 * Clicking a card opens that section's existing page inside a dialog
 * layered over this grid (AdminSectionDialog) instead of navigating to a
 * bare page, so a patient tab strip open above the AppShell's Outlet stays
 * visible underneath. Also backs the legacy `/administration/:section`
 * deep-link routes (routes.tsx points both `/administration` and
 * `/administration/:section` at this same component) — the optional
 * `:section` param opens the matching dialog on mount.
 */
export function AdministrationHub() {
  const { section } = useParams<{ section?: string }>();
  const navigate = useNavigate();
  const { user } = useAuth();
  const perms = user?.permissions ?? [];

  const [openSlug, setOpenSlug] = useState<string | null>(section ?? null);

  // Keep the open dialog in sync with the :section route param — covers
  // direct deep links (/administration/clinics) and browser back/forward.
  useEffect(() => {
    setOpenSlug(section ?? null);
  }, [section]);

  const visibleCards = ADMIN_HUB_SECTIONS.filter((s) => !s.perm || perms.includes(s.perm));

  const openSection = (slug: string) => {
    setOpenSlug(slug);
    navigate(`/administration/${slug}`, { replace: true });
  };

  const closeSection = () => {
    setOpenSlug(null);
    navigate("/administration", { replace: true });
  };

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
              onClick={() => openSection(s.slug)}
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

      <AdminSectionDialog slug={openSlug} onClose={closeSection} />
    </div>
  );
}
