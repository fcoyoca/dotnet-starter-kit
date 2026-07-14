import { useMemo, useState } from "react";
import { keepPreviousData, useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { Pencil, Plus, Trash2 } from "lucide-react";
import { toast } from "sonner";
import {
  deleteInsurancePolicy,
  searchPatientInsurancePolicies,
  type PatientInsurancePolicy,
} from "@/api/patient-insurance";
import type { PatientDetailDto } from "@/api/patients";
import { INSURANCE_PERMISSIONS } from "@/lib/patient-permissions";
import { useAuth } from "@/auth/use-auth";
import { Button } from "@/components/ui/button";
import { ConfirmDialog } from "@/components/ui/confirm-dialog";
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
import { describe, formatDate } from "@/lib/list-helpers";
import { InsurancePolicyDialog } from "@/pages/patients/insurance-policy-dialog";

type Props = {
  patient: PatientDetailDto;
  open: boolean;
  onClose(): void;
};

const currency = new Intl.NumberFormat(undefined, { style: "currency", currency: "USD" });

/** The one-line summary under each policy: what a biller needs to see without opening it. */
function policySummary(policy: PatientInsurancePolicy): string {
  const parts = [
    policy.policyNumber ? `Policy ${policy.policyNumber}` : null,
    policy.groupNumber ? `Group ${policy.groupNumber}` : null,
    policy.coPay != null ? `Copay ${currency.format(policy.coPay)}` : null,
    policy.deductible != null ? `Deductible ${currency.format(policy.deductible)}` : null,
  ].filter(Boolean);
  return parts.join(" · ");
}

function subscriberSummary(policy: PatientInsurancePolicy): string {
  if (policy.subscriberRelationship === "Self") return "Subscriber: Self";
  const name = [policy.subscriberFirstName, policy.subscriberLastName].filter(Boolean).join(" ");
  return `Subscriber: ${name || "—"} (${policy.subscriberRelationship})`;
}

export function InsurancePolicyListDialog({ patient, open, onClose }: Props) {
  const { user } = useAuth();
  const queryClient = useQueryClient();

  const canCreate = user?.permissions?.includes(INSURANCE_PERMISSIONS.create) ?? false;
  const canUpdate = user?.permissions?.includes(INSURANCE_PERMISSIONS.update) ?? false;
  const canDelete = user?.permissions?.includes(INSURANCE_PERMISSIONS.delete) ?? false;

  const [showInactive, setShowInactive] = useState(false);
  const [editorOpen, setEditorOpen] = useState(false);
  const [editPolicy, setEditPolicy] = useState<PatientInsurancePolicy | null>(null);
  const [pendingDelete, setPendingDelete] = useState<PatientInsurancePolicy | null>(null);

  const policiesQuery = useQuery({
    queryKey: ["patient-insurance-policies", patient.id, showInactive],
    queryFn: () =>
      searchPatientInsurancePolicies({
        patientId: patient.id,
        includeInactive: showInactive,
        pageSize: 200,
      }),
    enabled: open,
    placeholderData: keepPreviousData,
  });

  const policies = useMemo(() => policiesQuery.data?.items ?? [], [policiesQuery.data]);

  const deleteMutation = useMutation({
    mutationFn: (policyId: string) => deleteInsurancePolicy(policyId),
    onSuccess: () => {
      toast.success("Insurance policy removed.");
      void queryClient.invalidateQueries({
        queryKey: ["patient-insurance-policies", patient.id],
      });
      setPendingDelete(null);
    },
    onError: (err) =>
      toast.error("Failed to remove insurance policy.", { description: describe(err) }),
  });

  return (
    <>
      <Dialog open={open} onOpenChange={(o) => (!o ? onClose() : undefined)}>
        <DialogContent className="!max-w-3xl">
          <DialogHeader>
            <DialogTitle>Insurance</DialogTitle>
          </DialogHeader>

          <DialogBody className="space-y-4">
            <div className="flex flex-wrap items-center justify-between gap-3">
              <label className="flex cursor-pointer items-center gap-2 text-[13px]">
                <input
                  type="checkbox"
                  checked={showInactive}
                  onChange={(e) => setShowInactive(e.target.checked)}
                  className="rounded border-[var(--color-border)]"
                />
                <span>Show inactive</span>
              </label>
              {canCreate && (
                <Button
                  size="sm"
                  className="h-8 gap-1.5 rounded-lg px-3 text-[13px] font-semibold"
                  onClick={() => {
                    setEditPolicy(null);
                    setEditorOpen(true);
                  }}
                >
                  <Plus className="size-4" />
                  Add Insurance
                </Button>
              )}
            </div>

            {policiesQuery.isLoading ? (
              <div className="skeleton h-20 rounded-lg" />
            ) : policies.length === 0 ? (
              <p className="py-6 text-center text-[13px] text-[var(--color-muted-foreground)]">
                No insurance policies on file for this patient.
              </p>
            ) : (
              <ul className="divide-y divide-[var(--color-border)] rounded-lg border border-[var(--color-border)]">
                {policies.map((p) => (
                  <li key={p.id} className="flex items-center justify-between gap-3 px-3 py-2.5">
                    <div className="min-w-0">
                      <p className="truncate text-[13px] font-medium">
                        {p.insuranceCompanyName ?? "Unknown insurer"}
                        {p.insuranceTypeName ? (
                          <span className="font-normal text-[var(--color-muted-foreground)]">
                            {" "}
                            · {p.insuranceTypeName}
                          </span>
                        ) : null}
                      </p>
                      {policySummary(p) && (
                        <p className="truncate text-[12px] text-[var(--color-muted-foreground)]">
                          {policySummary(p)}
                        </p>
                      )}
                      <p className="truncate text-[12px] text-[var(--color-muted-foreground)]">
                        {subscriberSummary(p)}
                        {p.effectiveDate ? ` · Effective ${formatDate(p.effectiveDate)}` : ""}
                        {p.expirationDate ? ` – ${formatDate(p.expirationDate)}` : ""}
                      </p>
                    </div>
                    <div className="flex shrink-0 items-center gap-2">
                      <EntityStatusBadge tone="info">{p.priority}</EntityStatusBadge>
                      <EntityStatusBadge tone={p.isActive ? "success" : "warning"}>
                        {p.isActive ? "Active" : "Inactive"}
                      </EntityStatusBadge>
                      {canUpdate && (
                        <button
                          type="button"
                          title="Edit insurance policy"
                          aria-label="Edit insurance policy"
                          onClick={() => {
                            setEditPolicy(p);
                            setEditorOpen(true);
                          }}
                          className="inline-flex size-7 items-center justify-center rounded-md border border-[var(--color-border)] transition-colors hover:bg-[var(--color-accent)]"
                        >
                          <Pencil className="size-4" />
                        </button>
                      )}
                      {canDelete && (
                        <button
                          type="button"
                          title="Remove insurance policy"
                          aria-label="Remove insurance policy"
                          disabled={deleteMutation.isPending}
                          onClick={() => setPendingDelete(p)}
                          className="inline-flex size-7 items-center justify-center rounded-md border border-[var(--color-border)] transition-colors hover:bg-[var(--color-accent)]"
                        >
                          <Trash2 className="size-4" />
                        </button>
                      )}
                    </div>
                  </li>
                ))}
              </ul>
            )}
          </DialogBody>

          <DialogFooter>
            <DialogClose asChild>
              <Button type="button" variant="outline">
                Close
              </Button>
            </DialogClose>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      <InsurancePolicyDialog
        patient={patient}
        open={editorOpen}
        onClose={() => {
          setEditorOpen(false);
          setEditPolicy(null);
        }}
        policy={editPolicy}
        canToggleActive={canDelete}
      />

      <ConfirmDialog
        open={pendingDelete !== null}
        eyebrow="Remove policy"
        title="Remove this insurance policy?"
        description={
          <>
            The{" "}
            <span className="font-medium text-[var(--color-foreground)]">
              {pendingDelete?.priority}
            </span>{" "}
            policy with{" "}
            <span className="font-medium text-[var(--color-foreground)]">
              {pendingDelete?.insuranceCompanyName ?? "Unknown insurer"}
            </span>{" "}
            will be removed from this patient. Billing that references it may need to be reassigned.
          </>
        }
        confirmLabel="Remove policy"
        pendingLabel="Removing…"
        pending={deleteMutation.isPending}
        onCancel={() => setPendingDelete(null)}
        onConfirm={() => pendingDelete && deleteMutation.mutate(pendingDelete.id)}
      />
    </>
  );
}
