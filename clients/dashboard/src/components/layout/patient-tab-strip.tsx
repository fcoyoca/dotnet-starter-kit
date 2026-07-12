import { useNavigate } from "react-router-dom";
import { X } from "lucide-react";
import { closeAndPickFallback, usePatientWorkspace } from "@/state/patient-workspace-context";
import { cn } from "@/lib/cn";

/**
 * Horizontal strip of open patient tabs — rendered inside the patient chart
 * page (under the "Patient Chart" back link), so it only appears while the
 * user is working in a chart. Renders nothing when no patient tab is open.
 */
export function PatientTabStrip() {
  const { openTabs, activePatientId, setActivePatient, closePatient } = usePatientWorkspace();
  const navigate = useNavigate();

  if (openTabs.length === 0) return null;

  const onSelect = (patientId: string) => {
    setActivePatient(patientId);
    navigate(`/patient-charts/${patientId}`);
  };

  const onClose = (patientId: string) => {
    // Compute the same fallback closePatient() is about to apply internally,
    // so the URL we navigate to always matches the resulting activePatientId
    // instead of independently guessing (previously: always "last remaining").
    const { activeId: nextActivePatientId } = closeAndPickFallback(
      openTabs,
      patientId,
      activePatientId,
      (t) => t.patientId,
    );
    const wasActive = patientId === activePatientId;
    closePatient(patientId);
    if (wasActive) {
      navigate(nextActivePatientId ? `/patient-charts/${nextActivePatientId}` : "/patient-charts");
    }
  };

  return (
    <div
      role="tablist"
      aria-label="Open patient charts"
      className={cn(
        "flex h-9 shrink-0 items-center gap-1 overflow-x-auto rounded-lg border border-[var(--color-border)]",
        "bg-[var(--color-muted)] px-2",
      )}
    >
      {openTabs.map((tab) => {
        const isActive = tab.patientId === activePatientId;
        return (
          <div
            key={tab.patientId}
            role="tab"
            aria-selected={isActive}
            tabIndex={0}
            onClick={() => onSelect(tab.patientId)}
            onKeyDown={(e) => {
              if (e.key === "Enter" || e.key === " ") {
                e.preventDefault();
                onSelect(tab.patientId);
              }
            }}
            className={cn(
              "group flex h-7 shrink-0 cursor-pointer items-center gap-1.5 rounded-md px-2.5 text-[12px] font-medium",
              "transition-colors duration-[var(--duration-fast)] ease-[var(--ease-out-cubic)]",
              "focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-[var(--color-ring)]",
              isActive
                ? "bg-[var(--color-card)] text-[var(--color-foreground)] shadow-xs"
                : "text-[var(--color-muted-foreground)] hover:bg-[var(--color-accent)] hover:text-[var(--color-foreground)]",
            )}
          >
            <span className="max-w-[140px] truncate">{tab.patientLabel}</span>
            <button
              type="button"
              aria-label={`Close ${tab.patientLabel}'s chart tab`}
              onClick={(e) => {
                e.stopPropagation();
                onClose(tab.patientId);
              }}
              className="grid size-4 shrink-0 place-items-center rounded-sm opacity-60 transition-opacity hover:bg-[var(--color-border)] hover:opacity-100"
            >
              <X className="size-3" />
            </button>
          </div>
        );
      })}
    </div>
  );
}
