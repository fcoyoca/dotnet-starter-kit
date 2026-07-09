import { useEffect, useState } from "react";
import { useQuery } from "@tanstack/react-query";
import { Check } from "lucide-react";
import { searchPatientMedications, type PatientMedication } from "@/api/medications";
import { Button } from "@/components/ui/button";
import {
  Dialog,
  DialogBody,
  DialogClose,
  DialogContent,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import { EntityStatusBadge } from "@/components/list";
import { formatDate } from "@/lib/list-helpers";

type Props = {
  patientId: string;
  open: boolean;
  onClose(): void;
  /** Receives the legacy-formatted medication text block for insertion into the report field. */
  onDone(text: string): void;
};

// Legacy format, binding: name line, optional prescriber/start-date line, optional
// instructions line, then a blank line between medications.
function formatMedication(m: PatientMedication): string {
  let text = `Medication Name: ${m.drugName}\n`;
  if (m.prescriber || m.startDate) {
    text += `Prescriber: ${m.prescriber ?? ""} | Start Date: ${formatDate(m.startDate)}\n`;
  }
  if (m.instructions) {
    text += `Instructions: ${m.instructions}\n`;
  }
  return text + "\n";
}

/**
 * Import Medications picker (BackChart's medication-import on report field 6): pick from
 * the patient's medication list; Done builds one legacy-format block per selected medication.
 */
export function ImportMedicationsDialog({ patientId, open, onClose, onDone }: Props) {
  const [selectedIds, setSelectedIds] = useState<string[]>([]);
  const [showInactive, setShowInactive] = useState(false);

  useEffect(() => {
    if (!open) {
      setSelectedIds([]);
      setShowInactive(false);
    }
  }, [open]);

  const medicationsQuery = useQuery({
    queryKey: ["medications", patientId, { includeInactive: showInactive, forImport: true }],
    queryFn: () =>
      searchPatientMedications({ patientId, includeInactive: showInactive, pageSize: 100 }),
    enabled: open,
  });
  const medications = medicationsQuery.data?.items ?? [];

  const toggleOne = (id: string) =>
    setSelectedIds((prev) =>
      prev.includes(id) ? prev.filter((x) => x !== id) : [...prev, id],
    );

  const onDoneClick = () => {
    const text = medications
      .filter((m) => selectedIds.includes(m.id))
      .map(formatMedication)
      .join("");
    if (text) onDone(text);
    onClose();
  };

  return (
    <Dialog
      open={open}
      onOpenChange={(o) => {
        if (!o) onClose();
      }}
    >
      <DialogContent className="!max-w-2xl">
        <DialogHeader>
          <DialogTitle>Import Medications</DialogTitle>
        </DialogHeader>

        <DialogBody className="space-y-3">
          <div className="flex flex-wrap items-center justify-between gap-3">
            <div className="flex items-center gap-3 text-[13px]">
              <button
                type="button"
                onClick={() => setSelectedIds(medications.map((m) => m.id))}
                className="font-semibold text-[var(--color-primary)] hover:underline"
              >
                All
              </button>
              <button
                type="button"
                onClick={() => setSelectedIds([])}
                className="font-semibold text-[var(--color-primary)] hover:underline"
              >
                None
              </button>
            </div>
            <label className="flex items-center gap-2 text-[13px] cursor-pointer">
              <input
                type="checkbox"
                checked={showInactive}
                onChange={(e) => setShowInactive(e.target.checked)}
                className="rounded border-[var(--color-border)]"
              />
              <span>Show Inactive</span>
            </label>
          </div>

          {medicationsQuery.isLoading ? (
            <div className="skeleton h-24 rounded-lg" />
          ) : medications.length === 0 ? (
            <p className="py-6 text-center text-[13px] text-[var(--color-muted-foreground)]">
              No medications recorded for this patient.
            </p>
          ) : (
            <div className="max-h-72 overflow-auto rounded-lg border border-[var(--color-border)]">
              <table className="w-full text-[13px]">
                <thead className="sticky top-0 bg-[var(--color-card)]">
                  <tr className="border-b border-[var(--color-border)] text-left text-[11px] uppercase tracking-wide text-[var(--color-muted-foreground)]">
                    <th className="w-10 px-3 py-2" />
                    <th className="px-3 py-2">Drug Name</th>
                    <th className="px-3 py-2">Prescriber</th>
                    <th className="px-3 py-2 w-28">Start Date</th>
                    <th className="px-3 py-2 w-24">Status</th>
                  </tr>
                </thead>
                <tbody>
                  {medications.map((m) => (
                    <tr key={m.id} className="border-b border-[var(--color-border)] last:border-b-0">
                      <td className="px-3 py-2">
                        <input
                          type="checkbox"
                          checked={selectedIds.includes(m.id)}
                          onChange={() => toggleOne(m.id)}
                          aria-label={`Select ${m.drugName}`}
                          className="rounded border-[var(--color-border)]"
                        />
                      </td>
                      <td className="px-3 py-2 font-medium">{m.drugName}</td>
                      <td className="px-3 py-2">{m.prescriber ?? "—"}</td>
                      <td className="px-3 py-2">{formatDate(m.startDate)}</td>
                      <td className="px-3 py-2">
                        <EntityStatusBadge tone={m.isActive ? "success" : "default"}>
                          {m.isActive ? "Active" : "Inactive"}
                        </EntityStatusBadge>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
        </DialogBody>

        <DialogFooter>
          <Button type="button" onClick={onDoneClick} disabled={medicationsQuery.isLoading}>
            <Check className="size-4" />
            Done{selectedIds.length > 0 ? ` (${selectedIds.length})` : ""}
          </Button>
          <DialogClose asChild>
            <Button type="button" variant="outline">
              Close
            </Button>
          </DialogClose>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
