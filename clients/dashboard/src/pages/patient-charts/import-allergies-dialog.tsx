import { useEffect, useState } from "react";
import { useQuery } from "@tanstack/react-query";
import { Check } from "lucide-react";
import { searchPatientAllergies } from "@/api/allergies";
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
  /** Receives the legacy-formatted allergy text block for insertion into the report field. */
  onDone(text: string): void;
};

/**
 * Import Allergies picker (BackChart's allergy-import on report field 5): pick from the
 * patient's allergy list; Done builds one legacy-format line per selected allergy.
 */
export function ImportAllergiesDialog({ patientId, open, onClose, onDone }: Props) {
  const [selectedIds, setSelectedIds] = useState<string[]>([]);
  const [showInactive, setShowInactive] = useState(false);

  useEffect(() => {
    if (!open) {
      setSelectedIds([]);
      setShowInactive(false);
    }
  }, [open]);

  const allergiesQuery = useQuery({
    queryKey: ["allergies", patientId, { includeInactive: showInactive, forImport: true }],
    queryFn: () =>
      searchPatientAllergies({ patientId, includeInactive: showInactive, pageSize: 100 }),
    enabled: open,
  });
  const allergies = allergiesQuery.data?.items ?? [];

  const toggleOne = (id: string) =>
    setSelectedIds((prev) =>
      prev.includes(id) ? prev.filter((x) => x !== id) : [...prev, id],
    );

  const onDoneClick = () => {
    // Legacy format, binding: one line per selected allergy, each ending with a newline.
    const text = allergies
      .filter((a) => selectedIds.includes(a.id))
      .map((a) => `Allergen: ${a.drugName} | Reaction: ${a.reaction ?? ""}\n`)
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
          <DialogTitle>Import Allergies</DialogTitle>
        </DialogHeader>

        <DialogBody className="space-y-3">
          <div className="flex flex-wrap items-center justify-between gap-3">
            <div className="flex items-center gap-3 text-[13px]">
              <button
                type="button"
                onClick={() => setSelectedIds(allergies.map((a) => a.id))}
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

          {allergiesQuery.isLoading ? (
            <div className="skeleton h-24 rounded-lg" />
          ) : allergies.length === 0 ? (
            <p className="py-6 text-center text-[13px] text-[var(--color-muted-foreground)]">
              No allergies recorded for this patient.
            </p>
          ) : (
            <div className="max-h-72 overflow-auto rounded-lg border border-[var(--color-border)]">
              <table className="w-full text-[13px]">
                <thead className="sticky top-0 bg-[var(--color-card)]">
                  <tr className="border-b border-[var(--color-border)] text-left text-[11px] uppercase tracking-wide text-[var(--color-muted-foreground)]">
                    <th className="w-10 px-3 py-2" />
                    <th className="px-3 py-2">Drug Name</th>
                    <th className="px-3 py-2 w-28">Date Noted</th>
                    <th className="px-3 py-2">Reaction</th>
                    <th className="px-3 py-2 w-24">Status</th>
                  </tr>
                </thead>
                <tbody>
                  {allergies.map((a) => (
                    <tr key={a.id} className="border-b border-[var(--color-border)] last:border-b-0">
                      <td className="px-3 py-2">
                        <input
                          type="checkbox"
                          checked={selectedIds.includes(a.id)}
                          onChange={() => toggleOne(a.id)}
                          aria-label={`Select ${a.drugName}`}
                          className="rounded border-[var(--color-border)]"
                        />
                      </td>
                      <td className="px-3 py-2 font-medium">{a.drugName}</td>
                      <td className="px-3 py-2">{formatDate(a.dateNoted)}</td>
                      <td className="px-3 py-2">{a.reaction ?? "—"}</td>
                      <td className="px-3 py-2">
                        <EntityStatusBadge tone={a.isActive ? "success" : "default"}>
                          {a.isActive ? "Active" : "Inactive"}
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
          <Button type="button" onClick={onDoneClick} disabled={allergiesQuery.isLoading}>
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
