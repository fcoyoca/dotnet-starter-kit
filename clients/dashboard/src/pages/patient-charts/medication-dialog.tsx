import { useEffect, useState, type FormEvent } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { Info } from "lucide-react";
import { toast } from "sonner";
import {
  createMedication,
  medlinePlusUrl,
  updateMedication,
  type PatientMedication,
} from "@/api/medications";
import { listMedicationDoseUnits } from "@/api/administration";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Textarea } from "@/components/ui/textarea";
import {
  Dialog,
  DialogBody,
  DialogClose,
  DialogContent,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import { Combobox, Field, type ComboboxOption } from "@/components/list";
import { describe } from "@/lib/list-helpers";
import { DrugPicker, type DrugSelection } from "@/pages/patient-charts/drug-picker";

/** Legacy dose-period units (PatientMedicationDetail.razor PeriodNames). */
const PERIOD_OPTIONS: ComboboxOption[] = [
  { value: "h", label: "h (hour)" },
  { value: "d", label: "d (day)" },
  { value: "wk", label: "wk (week)" },
  { value: "mo", label: "mo (month)" },
];

type Props = {
  patientId: string;
  open: boolean;
  onClose(): void;
  medication?: PatientMedication | null;
  canToggleActive: boolean;
};

export function MedicationDialog({ patientId, open, onClose, medication, canToggleActive }: Props) {
  const queryClient = useQueryClient();
  const isEdit = !!medication;

  const [drug, setDrug] = useState<DrugSelection | null>(null);
  const [prescriber, setPrescriber] = useState("");
  const [startDate, setStartDate] = useState("");
  const [endDate, setEndDate] = useState("");
  const [doseValue, setDoseValue] = useState("");
  const [doseUnitId, setDoseUnitId] = useState<number | null>(null);
  const [dosePeriodValue, setDosePeriodValue] = useState("");
  const [dosePeriodUnit, setDosePeriodUnit] = useState<string | null>(null);
  const [instructions, setInstructions] = useState("");
  const [indication, setIndication] = useState("");
  const [isActive, setIsActive] = useState(true);

  useEffect(() => {
    if (!open) return;
    if (medication) {
      setDrug({
        name: medication.drugName,
        rxAui: medication.rxAui ?? null,
        rxCui: medication.rxCode ?? null,
      });
      setPrescriber(medication.prescriber ?? "");
      setStartDate(medication.startDate ? medication.startDate.slice(0, 10) : "");
      setEndDate(medication.endDate ? medication.endDate.slice(0, 10) : "");
      setDoseValue(medication.doseValue != null ? String(medication.doseValue) : "");
      setDoseUnitId(medication.doseUnitId ?? null);
      setDosePeriodValue(medication.dosePeriodValue != null ? String(medication.dosePeriodValue) : "");
      setDosePeriodUnit(medication.dosePeriodUnit ?? null);
      setInstructions(medication.instructions ?? "");
      setIndication(medication.indication ?? "");
      setIsActive(medication.isActive);
    } else {
      setDrug(null);
      setPrescriber("");
      setStartDate(new Date().toISOString().slice(0, 10));
      setEndDate("");
      setDoseValue("");
      setDoseUnitId(null);
      setDosePeriodValue("");
      setDosePeriodUnit(null);
      setInstructions("");
      setIndication("");
      setIsActive(true);
    }
  }, [open, medication]);

  const doseUnitsQuery = useQuery({
    queryKey: ["medication-dose-units", "active"],
    queryFn: () => listMedicationDoseUnits({ isActive: true }),
    enabled: open,
    staleTime: 5 * 60 * 1000,
  });

  const doseUnitOptions: ComboboxOption[] = (doseUnitsQuery.data ?? []).map((u) => ({
    value: String(u.id),
    label: u.name,
  }));

  const invalidate = () => {
    void queryClient.invalidateQueries({ queryKey: ["patient-medications", patientId] });
    void queryClient.invalidateQueries({ queryKey: ["patients", patientId] });
  };

  const createMutation = useMutation({
    mutationFn: createMedication,
    onSuccess: () => {
      toast.success("Medication added.");
      invalidate();
      onClose();
    },
    onError: (err) => toast.error("Failed to add medication.", { description: describe(err) }),
  });

  const updateMutation = useMutation({
    mutationFn: updateMedication,
    onSuccess: () => {
      toast.success("Medication updated.");
      invalidate();
      onClose();
    },
    onError: (err) => toast.error("Failed to update medication.", { description: describe(err) }),
  });

  const isPending = createMutation.isPending || updateMutation.isPending;

  const onSubmit = (e: FormEvent<HTMLFormElement>) => {
    e.preventDefault();
    if (!drug?.name || !startDate) return;
    const fields = {
      drugName: drug.name,
      rxAui: drug.rxAui,
      rxCode: drug.rxCui,
      ndc: medication?.ndc ?? null,
      prescriber: prescriber.trim() || null,
      startDate,
      endDate: endDate || null,
      doseValue: doseValue ? Number(doseValue) : null,
      doseUnitId,
      dosePeriodValue: dosePeriodValue ? Number(dosePeriodValue) : null,
      dosePeriodUnit,
      instructions: instructions.trim() || null,
      indication: indication.trim() || null,
      isActive,
    };
    if (isEdit && medication) {
      updateMutation.mutate({ ...fields, medicationId: medication.id });
    } else {
      createMutation.mutate({ ...fields, patientId });
    }
  };

  return (
    <Dialog open={open} onOpenChange={(o) => (!o ? onClose() : undefined)}>
      <DialogContent className="!max-w-lg">
        <form onSubmit={onSubmit}>
          <DialogHeader>
            <DialogTitle className="flex items-center justify-between gap-2">
              {isEdit ? "Edit Medication" : "Add Medication"}
              {medication?.rxCode && (
                <a
                  href={medlinePlusUrl(medication.rxCode)}
                  target="_blank"
                  rel="noreferrer"
                  title="Drug info (MedlinePlus)"
                  className="inline-flex items-center gap-1 text-[12px] font-medium text-[var(--color-primary)] hover:underline"
                >
                  <Info className="size-4" />
                  Info
                </a>
              )}
            </DialogTitle>
          </DialogHeader>

          <DialogBody className="space-y-4">
            <DrugPicker value={drug} onChange={setDrug} label="Medication" />

            <Field id="med-prescriber" label="Prescriber">
              <Input
                id="med-prescriber"
                type="text"
                value={prescriber}
                maxLength={256}
                onChange={(e) => setPrescriber(e.target.value)}
              />
            </Field>

            <div className="grid gap-3 sm:grid-cols-2">
              <Field id="med-start" label="Start Date">
                <Input
                  id="med-start"
                  type="date"
                  value={startDate}
                  onChange={(e) => setStartDate(e.target.value)}
                  required
                />
              </Field>
              <Field id="med-end" label="End Date">
                <Input
                  id="med-end"
                  type="date"
                  value={endDate}
                  onChange={(e) => setEndDate(e.target.value)}
                />
              </Field>
            </div>

            <div className="grid gap-3 sm:grid-cols-2">
              <Field id="med-dose-value" label="Dosage">
                <Input
                  id="med-dose-value"
                  type="number"
                  min="0"
                  step="any"
                  value={doseValue}
                  onChange={(e) => setDoseValue(e.target.value)}
                />
              </Field>
              <Field id="med-dose-unit" label="Dose Unit">
                <Combobox
                  id="med-dose-unit"
                  label="Dose Unit"
                  value={doseUnitId != null ? String(doseUnitId) : null}
                  onChange={(v) => setDoseUnitId(v ? Number(v) : null)}
                  options={doseUnitOptions}
                  placeholder="Select…"
                />
              </Field>
            </div>

            <div className="grid gap-3 sm:grid-cols-2">
              <Field id="med-period-value" label="Dosage Period">
                <Input
                  id="med-period-value"
                  type="number"
                  min="0"
                  step="any"
                  value={dosePeriodValue}
                  onChange={(e) => setDosePeriodValue(e.target.value)}
                />
              </Field>
              <Field id="med-period-unit" label="Period Unit">
                <Combobox
                  id="med-period-unit"
                  label="Period Unit"
                  value={dosePeriodUnit}
                  onChange={setDosePeriodUnit}
                  options={PERIOD_OPTIONS}
                  placeholder="Select…"
                />
              </Field>
            </div>

            <Field id="med-instructions" label="Special Instructions">
              <Textarea
                id="med-instructions"
                value={instructions}
                onChange={(e) => setInstructions(e.target.value)}
                rows={2}
                maxLength={4000}
              />
            </Field>

            <Field id="med-indication" label="Indications">
              <Textarea
                id="med-indication"
                value={indication}
                onChange={(e) => setIndication(e.target.value)}
                rows={2}
                maxLength={4000}
              />
            </Field>

            {canToggleActive && (
              <div className="flex items-center gap-4 text-[13px]">
                <label className="flex items-center gap-2 cursor-pointer">
                  <input
                    type="radio"
                    name="med-active"
                    checked={isActive}
                    onChange={() => setIsActive(true)}
                  />
                  <span>Active</span>
                </label>
                <label className="flex items-center gap-2 cursor-pointer">
                  <input
                    type="radio"
                    name="med-active"
                    checked={!isActive}
                    onChange={() => setIsActive(false)}
                  />
                  <span>Inactive</span>
                </label>
              </div>
            )}

            {isEdit && medication?.createdByName && (
              <p className="text-[12px] text-[var(--color-muted-foreground)]">
                Created: {medication.createdByName}
                {medication.updatedByName ? ` · Last modified: ${medication.updatedByName}` : ""}
              </p>
            )}
          </DialogBody>

          <DialogFooter>
            <DialogClose asChild>
              <Button type="button" variant="outline" disabled={isPending}>
                Cancel
              </Button>
            </DialogClose>
            <Button type="submit" disabled={isPending || !drug?.name || !startDate}>
              {isPending ? "Saving…" : isEdit ? "Save Changes" : "Add Medication"}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}
