import { useEffect } from "react";
import { useNavigate, useParams } from "react-router-dom";
import { Building2 } from "lucide-react";
import { useAuth } from "@/auth/use-auth";
import { EntityPageHeader } from "@/components/list";
import { useAdministrationDialog } from "@/state/administration-dialog-context";
import {
  ADMIN_HUB_SECTIONS,
  ADMIN_SECTION_COMPONENTS,
} from "@/pages/administration/section-registry";
import { cn } from "@/lib/cn";

/**
 * Pure content component: the Administration header + permission-gated
 * card grid, rendered inside the global AdministrationDialogRoot.
 * No routing hooks — a card click switches the dialog's view.
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

/**
 * Route bridge for legacy /administration[/:section] deep links: opens the
 * matching view of the global Administration dialog, then replaces the URL
 * with Overview ("/") — a cold deep link has no "previous page" to
 * preserve, so Overview is the surface the dialog layers over. An unknown
 * slug falls back to the hub. Renders nothing.
 */
export function AdminDeepLinkOpener() {
  const { section } = useParams<{ section?: string }>();
  const navigate = useNavigate();
  const { openHub, openSection } = useAdministrationDialog();

  useEffect(() => {
    if (section && ADMIN_SECTION_COMPONENTS[section]) openSection(section);
    else openHub();
    navigate("/", { replace: true });
  }, [section, openHub, openSection, navigate]);

  return null;
}
