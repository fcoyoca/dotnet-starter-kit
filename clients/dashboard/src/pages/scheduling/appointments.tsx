import { CalendarClock } from "lucide-react";
import { EntityPageHeader } from "@/components/list";

export function AppointmentsPage() {
  return (
    <div className="space-y-4">
      <EntityPageHeader
        icon={CalendarClock}
        title="Appointments"
        description="Day-view scheduler across providers, in each clinic's local time."
      />
      <div className="rounded-lg border border-[var(--color-border)] p-8 text-center text-[13px] text-[var(--color-muted-foreground)]">
        Calendar loading…
      </div>
    </div>
  );
}
