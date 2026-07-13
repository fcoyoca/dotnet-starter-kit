import { useEffect, useMemo, useState, type FormEvent } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { X } from "lucide-react";
import { toast } from "sonner";
import {
  createIncident,
  updateIncident,
  getPatientIncident,
  type AccidentType,
  type IncidentPatientStatus,
} from "@/api/incidents";
import {
  listCustomDiagnostics,
  useDepartmentOptions,
  useIncidentTypeOptions,
} from "@/api/administration";
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

const ACCIDENT_TYPE_OPTIONS: ComboboxOption[] = [
  { value: "Auto",        label: "Auto" },
  { value: "WorkersComp", label: "Workers Comp" },
  { value: "Slip",        label: "Slip & Fall" },
  { value: "Other",       label: "Other" },
];

const PATIENT_STATUS_OPTIONS: ComboboxOption[] = [
  { value: "Active",      label: "Active" },
  { value: "Inactive",    label: "Inactive" },
  { value: "Discharged",  label: "Discharged" },
  { value: "Transferred", label: "Transferred" },
];

const ADHERENCE_OPTIONS: ComboboxOption[] = Array.from({ length: 11 }, (_, i) => ({
  value: String(i),
  label: String(i),
}));

type Props = {
  patientId: string;
  open: boolean;
  onClose(): void;
  /** When set, dialog is in edit mode and will load the full incident. */
  incidentId?: string | null;
};

export function IncidentDialog({ patientId, open, onClose, incidentId }: Props) {
  const queryClient = useQueryClient();
  const isEdit = !!incidentId;

  // Form state
  const [dateOfLoss, setDateOfLoss] = useState("");
  const [dateOfInitialVisit, setDateOfInitialVisit] = useState("");
  const [isAccident, setIsAccident] = useState(false);
  const [accidentType, setAccidentType] = useState<AccidentType | null>(null);
  const [accidentState, setAccidentState] = useState("");
  const [incidentTypeId, setIncidentTypeId] = useState<string | null>(null);
  const [departmentId, setDepartmentId] = useState<string | null>(null);
  const [isTransfer, setIsTransfer] = useState(false);
  const [comments, setComments] = useState("");
  const [summaryOfCare, setSummaryOfCare] = useState("");
  const [adherenceToPlan, setAdherenceToPlan] = useState<string | null>(null);
  const [patientStatus, setPatientStatus] = useState<IncidentPatientStatus>("Active");
  const [isClosed, setIsClosed] = useState(false);
  const [selectedDxIds, setSelectedDxIds] = useState<string[]>([]);
  const [dxSearch, setDxSearch] = useState("");

  const departmentOptions = useDepartmentOptions();
  const incidentTypeOptions = useIncidentTypeOptions();

  // Load full detail for edit
  const detailQuery = useQuery({
    queryKey: ["incident", incidentId],
    queryFn: () => getPatientIncident(incidentId!),
    enabled: isEdit && open,
  });

  // Diagnostic search for multi-select
  const dxQuery = useQuery({
    queryKey: ["dx-search", dxSearch],
    queryFn: () => listCustomDiagnostics({ search: dxSearch, isActive: true, pageSize: 50 }),
    enabled: dxSearch.length >= 2,
  });

  const dxOptions: ComboboxOption[] = useMemo(
    () =>
      (dxQuery.data?.items ?? []).map((d) => ({
        value: d.id,
        label: `${d.code}${d.description ? " — " + d.description : ""}`,
      })),
    [dxQuery.data],
  );

  useEffect(() => {
    if (!open) {
      setDateOfLoss("");
      setDateOfInitialVisit("");
      setIsAccident(false);
      setAccidentType(null);
      setAccidentState("");
      setIncidentTypeId(null);
      setDepartmentId(null);
      setIsTransfer(false);
      setComments("");
      setSummaryOfCare("");
      setAdherenceToPlan(null);
      setPatientStatus("Active");
      setIsClosed(false);
      setSelectedDxIds([]);
      setDxSearch("");
    }
  }, [open]);

  useEffect(() => {
    const d = detailQuery.data;
    if (!d) return;
    setDateOfLoss(d.dateOfLoss.slice(0, 10));
    setDateOfInitialVisit(d.dateOfInitialVisit ? d.dateOfInitialVisit.slice(0, 10) : "");
    setIsAccident(d.isAccident);
    setAccidentType(d.accidentType ?? null);
    setAccidentState(d.accidentState ?? "");
    setIncidentTypeId(d.incidentTypeId ?? null);
    setDepartmentId(d.departmentId ?? null);
    setIsTransfer(d.isTransfer);
    setComments(d.comments ?? "");
    setSummaryOfCare(d.summaryOfCare ?? "");
    setAdherenceToPlan(d.adherenceToPlan !== null && d.adherenceToPlan !== undefined ? String(d.adherenceToPlan) : null);
    setPatientStatus(d.patientStatus);
    setIsClosed(d.isClosed);
    setSelectedDxIds(d.diagnosticIds ?? []);
  }, [detailQuery.data]);

  const createMutation = useMutation({
    mutationFn: createIncident,
    onSuccess: () => {
      toast.success("Incident created.");
      void queryClient.invalidateQueries({ queryKey: ["incidents", patientId] });
      onClose();
    },
    onError: (err) => toast.error("Failed to create incident.", { description: describe(err) }),
  });

  const updateMutation = useMutation({
    mutationFn: updateIncident,
    onSuccess: () => {
      toast.success("Incident updated.");
      void queryClient.invalidateQueries({ queryKey: ["incidents", patientId] });
      void queryClient.invalidateQueries({ queryKey: ["incident", incidentId] });
      onClose();
    },
    onError: (err) => toast.error("Failed to update incident.", { description: describe(err) }),
  });

  const isPending = createMutation.isPending || updateMutation.isPending;

  const onSubmit = (e: FormEvent<HTMLFormElement>) => {
    e.preventDefault();
    if (!dateOfLoss) return;

    if (isEdit && incidentId) {
      updateMutation.mutate({
        incidentId,
        incidentTypeId,
        departmentId,
        dateOfInitialVisit: dateOfInitialVisit || dateOfLoss,
        dateOfLoss,
        isTransfer,
        isAccident,
        accidentType: isAccident ? accidentType : null,
        accidentState: isAccident ? (accidentState.trim() || null) : null,
        comments: comments.trim() || null,
        summaryOfCare: summaryOfCare.trim() || null,
        adherenceToPlan: adherenceToPlan !== null ? Number(adherenceToPlan) : null,
        patientStatus,
        isClosed,
        diagnosticIds: selectedDxIds,
      });
    } else {
      createMutation.mutate({
        patientId,
        incidentTypeId,
        departmentId,
        dateOfInitialVisit: dateOfInitialVisit || null,
        dateOfLoss,
        isTransfer,
        isAccident,
        accidentType: isAccident ? accidentType : null,
        accidentState: isAccident ? (accidentState.trim() || null) : null,
        comments: comments.trim() || null,
        diagnosticIds: selectedDxIds,
      });
    }
  };

  const addDx = (id: string | null) => {
    if (!id || selectedDxIds.includes(id)) return;
    setSelectedDxIds((prev) => [...prev, id]);
    setDxSearch("");
  };

  const removeDx = (id: string) => {
    setSelectedDxIds((prev) => prev.filter((x) => x !== id));
  };

  // Resolve labels for the currently selected DX ids so each chip shows the code
  // (not a truncated guid). Search results only cover codes just looked up; this
  // covers edit-mode hydration and codes whose search list has since cleared.
  const dxDetailsQuery = useQuery({
    queryKey: ["custom-diagnostics", "by-ids", [...selectedDxIds].sort().join(",")],
    queryFn: () => listCustomDiagnostics({ ids: selectedDxIds, pageSize: 200 }),
    enabled: open && selectedDxIds.length > 0,
  });

  const dxLabelMap = useMemo(() => {
    const map: Record<string, string> = {};
    for (const d of dxDetailsQuery.data?.items ?? [])
      map[d.id] = `${d.code}${d.description ? " — " + d.description : ""}`;
    // Live search results override as a fast path for a just-added code whose
    // by-ids fetch hasn't landed yet.
    for (const opt of dxOptions) map[opt.value] = opt.label;
    return map;
  }, [dxDetailsQuery.data, dxOptions]);

  return (
    <Dialog open={open} onOpenChange={(o) => (!o ? onClose() : undefined)}>
      <DialogContent className="!max-w-xl">
        <form onSubmit={onSubmit}>
          <DialogHeader>
            <DialogTitle>{isEdit ? "Edit Incident" : "New Incident"}</DialogTitle>
          </DialogHeader>

          <DialogBody className="space-y-4">
            {isEdit && detailQuery.isLoading && (
              <div className="skeleton h-8 rounded" />
            )}

            <div className="grid gap-3 sm:grid-cols-2">
              <Field id="inc-dol" label="Date of Loss" required>
                <Input
                  id="inc-dol"
                  type="date"
                  value={dateOfLoss}
                  onChange={(e) => setDateOfLoss(e.target.value)}
                  required
                />
              </Field>
              <Field id="inc-doi" label="Date of Initial Visit">
                <Input
                  id="inc-doi"
                  type="date"
                  value={dateOfInitialVisit}
                  onChange={(e) => setDateOfInitialVisit(e.target.value)}
                />
              </Field>
            </div>

            <div className="grid gap-3 sm:grid-cols-2">
              <Field id="inc-type" label="Incident Type">
                <Combobox
                  id="inc-type"
                  label="Incident type"
                  value={incidentTypeId}
                  onChange={setIncidentTypeId}
                  options={incidentTypeOptions ?? []}
                  placeholder="Select…"
                />
              </Field>
              <Field id="inc-dept" label="Department">
                <Combobox
                  id="inc-dept"
                  label="Department"
                  value={departmentId}
                  onChange={setDepartmentId}
                  options={departmentOptions ?? []}
                  placeholder="Select…"
                />
              </Field>
            </div>

            <div className="flex items-center gap-4">
              <label className="flex items-center gap-2 text-[13px] cursor-pointer">
                <input
                  type="checkbox"
                  checked={isAccident}
                  onChange={(e) => setIsAccident(e.target.checked)}
                  className="rounded border-[var(--color-border)]"
                />
                <span>Accident</span>
              </label>
              <label className="flex items-center gap-2 text-[13px] cursor-pointer">
                <input
                  type="checkbox"
                  checked={isTransfer}
                  onChange={(e) => setIsTransfer(e.target.checked)}
                  className="rounded border-[var(--color-border)]"
                />
                <span>Transfer</span>
              </label>
            </div>

            {isAccident && (
              <div className="grid gap-3 sm:grid-cols-[1fr_80px]">
                <Field id="inc-at" label="Accident Type" required={isAccident}>
                  <Combobox
                    id="inc-at"
                    label="Accident type"
                    value={accidentType}
                    onChange={(v) => setAccidentType(v as AccidentType | null)}
                    options={ACCIDENT_TYPE_OPTIONS}
                    placeholder="Select…"
                    required={isAccident}
                  />
                </Field>
                <Field id="inc-as" label="State">
                  <Input
                    id="inc-as"
                    value={accidentState}
                    onChange={(e) => setAccidentState(e.target.value.slice(0, 2).toUpperCase())}
                    placeholder="CA"
                    maxLength={2}
                  />
                </Field>
              </div>
            )}

            <Field id="inc-comments" label="Comments">
              <Textarea
                id="inc-comments"
                value={comments}
                onChange={(e) => setComments(e.target.value)}
                rows={3}
                maxLength={8000}
                placeholder="Initial notes…"
              />
            </Field>


            {/* DX Codes */}
            <div className="space-y-2">
              <label className="text-[13px] font-medium">DX Codes</label>
              <Input
                type="text"
                value={dxSearch}
                onChange={(e) => setDxSearch(e.target.value)}
                placeholder="Search by code or description (min 2 chars)…"
              />
              {dxSearch.length >= 2 && dxOptions.length > 0 && (
                <ul className="max-h-40 overflow-y-auto rounded-md border border-[var(--color-border)] bg-[var(--color-card)]">
                  {dxOptions.map((opt) => (
                    <li key={opt.value}>
                      <button
                        type="button"
                        onClick={() => addDx(opt.value)}
                        disabled={selectedDxIds.includes(opt.value)}
                        className="w-full px-3 py-1.5 text-left text-[12px] hover:bg-[var(--color-accent)] disabled:opacity-40"
                      >
                        {opt.label}
                      </button>
                    </li>
                  ))}
                </ul>
              )}
              {selectedDxIds.length > 0 && (
                <div className="flex flex-wrap gap-1.5">
                  {selectedDxIds.map((id) => (
                    <span
                      key={id}
                      className="inline-flex items-center gap-1 rounded-full bg-[var(--color-accent)] px-2 py-0.5 text-[11px] font-medium"
                    >
                      {dxLabelMap[id] ?? id.slice(0, 8)}
                      <button
                        type="button"
                        aria-label="Remove"
                        onClick={() => removeDx(id)}
                        className="hover:text-[var(--color-destructive)]"
                      >
                        <X className="size-3" />
                      </button>
                    </span>
                  ))}
                </div>
              )}
            </div>

            {/* Edit-only fields */}
            {isEdit && (
              <>
                <Field id="inc-summary" label="Summary of Care">
                  <Textarea
                    id="inc-summary"
                    value={summaryOfCare}
                    onChange={(e) => setSummaryOfCare(e.target.value)}
                    rows={3}
                    maxLength={8000}
                    placeholder="Summary…"
                  />
                </Field>

                <div className="grid gap-3 sm:grid-cols-2">
                  <Field id="inc-adherence" label="Adherence to Plan">
                    <Combobox
                      id="inc-adherence"
                      label="Adherence (0–10)"
                      value={adherenceToPlan}
                      onChange={setAdherenceToPlan}
                      options={ADHERENCE_OPTIONS}
                      placeholder="Select…"
                    />
                  </Field>
                  <Field id="inc-status" label="Patient Status">
                    <Combobox
                      id="inc-status"
                      label="Status"
                      value={patientStatus}
                      onChange={(v) => setPatientStatus((v ?? "Active") as IncidentPatientStatus)}
                      options={PATIENT_STATUS_OPTIONS}
                      placeholder="Select…"
                    />
                  </Field>
                </div>

                <label className="flex items-center gap-2 text-[13px] cursor-pointer">
                  <input
                    type="checkbox"
                    checked={isClosed}
                    onChange={(e) => setIsClosed(e.target.checked)}
                    className="rounded border-[var(--color-border)]"
                  />
                  <span>Mark as Closed</span>
                </label>
              </>
            )}
          </DialogBody>

          <DialogFooter>
            <DialogClose asChild>
              <Button type="button" variant="outline" disabled={isPending}>
                Cancel
              </Button>
            </DialogClose>
            <Button type="submit" disabled={isPending}>
              {isPending ? "Saving…" : isEdit ? "Save Changes" : "Create Incident"}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}
