import { useEffect, useMemo, useState, type FormEvent } from "react";
import { useMutation, useQueryClient } from "@tanstack/react-query";
import { toast } from "sonner";
import {
  createInsurancePolicy,
  updateInsurancePolicy,
  INSURANCE_PRIORITY_OPTIONS,
  SUBSCRIBER_RELATIONSHIP_OPTIONS,
  type InsurancePriority,
  type PatientInsurancePolicy,
  type SubscriberRelationship,
} from "@/api/patient-insurance";
import type { PatientDetailDto } from "@/api/patients";
import {
  GENDER_OPTIONS,
  useInsuranceCompanyOptions,
  useInsuranceTypeOptions,
} from "@/lib/patient-lookups";
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
import { Combobox, Field } from "@/components/list";
import { describe } from "@/lib/list-helpers";

type Props = {
  patient: PatientDetailDto;
  open: boolean;
  onClose(): void;
  /** Edit mode when set. */
  policy?: PatientInsurancePolicy | null;
  /** Whether the current user may flip Active/Inactive (InsurancePolicies.Delete). */
  canToggleActive: boolean;
};

/** The subscriber block as it reads when the patient is the policy holder. */
type Subscriber = {
  firstName: string;
  lastName: string;
  dateOfBirth: string;
  gender: string;
  employerName: string;
  address1: string;
  address2: string;
  city: string;
  state: string;
  zipCode: string;
};

const EMPTY_SUBSCRIBER: Subscriber = {
  firstName: "",
  lastName: "",
  dateOfBirth: "",
  gender: "",
  employerName: "",
  address1: "",
  address2: "",
  city: "",
  state: "",
  zipCode: "",
};

/**
 * Legacy behaviour: when the subscriber is the patient, the insured block is filled from the
 * patient's own demographics and locked. The values are still persisted onto the policy, so the row
 * stays a self-contained snapshot of what was submitted to the payer.
 *
 * The SSN is deliberately not copied — the API only ever returns it masked, so there is no plaintext
 * to copy. Leaving it blank tells the server to keep whatever is already stored.
 */
function subscriberFromPatient(patient: PatientDetailDto): Subscriber {
  return {
    firstName: patient.demographics.firstName ?? "",
    lastName: patient.demographics.lastName ?? "",
    dateOfBirth: patient.demographics.dateOfBirth?.slice(0, 10) ?? "",
    gender: patient.demographics.gender ?? "",
    employerName: patient.employment?.employerName ?? "",
    address1: patient.contact?.address1 ?? "",
    address2: patient.contact?.address2 ?? "",
    city: patient.contact?.city ?? "",
    state: patient.contact?.state ?? "",
    zipCode: patient.contact?.zipCode ?? "",
  };
}

function subscriberFromPolicy(policy: PatientInsurancePolicy): Subscriber {
  return {
    firstName: policy.subscriberFirstName ?? "",
    lastName: policy.subscriberLastName ?? "",
    dateOfBirth: policy.subscriberDateOfBirth?.slice(0, 10) ?? "",
    gender: policy.subscriberGender ?? "",
    employerName: policy.subscriberEmployerName ?? "",
    address1: policy.subscriberAddress1 ?? "",
    address2: policy.subscriberAddress2 ?? "",
    city: policy.subscriberCity ?? "",
    state: policy.subscriberState ?? "",
    zipCode: policy.subscriberZipCode ?? "",
  };
}

const numberOrNull = (value: string): number | null => {
  const trimmed = value.trim();
  if (!trimmed) return null;
  const parsed = Number(trimmed);
  return Number.isFinite(parsed) ? parsed : null;
};

export function InsurancePolicyDialog({ patient, open, onClose, policy, canToggleActive }: Props) {
  const queryClient = useQueryClient();
  const isEdit = !!policy;

  const companyOptions = useInsuranceCompanyOptions() ?? [];
  const typeOptions = useInsuranceTypeOptions() ?? [];

  const [insuranceCompanyId, setInsuranceCompanyId] = useState<string | null>(null);
  const [insuranceTypeId, setInsuranceTypeId] = useState<string | null>(null);
  const [priority, setPriority] = useState<InsurancePriority>("Primary");
  const [policyNumber, setPolicyNumber] = useState("");
  const [groupNumber, setGroupNumber] = useState("");
  const [memberId, setMemberId] = useState("");
  const [coPay, setCoPay] = useState("");
  const [deductible, setDeductible] = useState("");
  const [effectiveDate, setEffectiveDate] = useState("");
  const [expirationDate, setExpirationDate] = useState("");
  const [relationship, setRelationship] = useState<SubscriberRelationship>("Self");
  const [subscriber, setSubscriber] = useState<Subscriber>(EMPTY_SUBSCRIBER);
  const [subscriberSsn, setSubscriberSsn] = useState("");
  const [notes, setNotes] = useState("");
  const [isActive, setIsActive] = useState(true);

  useEffect(() => {
    if (!open) return;
    if (policy) {
      setInsuranceCompanyId(policy.insuranceCompanyId);
      setInsuranceTypeId(policy.insuranceTypeId ?? null);
      setPriority(policy.priority);
      setPolicyNumber(policy.policyNumber ?? "");
      setGroupNumber(policy.groupNumber ?? "");
      setMemberId(policy.memberId ?? "");
      setCoPay(policy.coPay != null ? String(policy.coPay) : "");
      setDeductible(policy.deductible != null ? String(policy.deductible) : "");
      setEffectiveDate(policy.effectiveDate?.slice(0, 10) ?? "");
      setExpirationDate(policy.expirationDate?.slice(0, 10) ?? "");
      setRelationship(policy.subscriberRelationship);
      // `subscriber` holds the *non-Self* subscriber only. A Self policy's stored block is just a
      // copy of the patient, so seeding from it would let the patient's identity leak into the form
      // if the user later switched the relationship to Spouse/Child/Other.
      setSubscriber(
        policy.subscriberRelationship === "Self" ? EMPTY_SUBSCRIBER : subscriberFromPolicy(policy),
      );
      setNotes(policy.notes ?? "");
      setIsActive(policy.isActive);
    } else {
      setInsuranceCompanyId(null);
      setInsuranceTypeId(null);
      setPriority("Primary");
      setPolicyNumber("");
      setGroupNumber("");
      setMemberId("");
      setCoPay("");
      setDeductible("");
      setEffectiveDate("");
      setExpirationDate("");
      setRelationship("Self");
      setSubscriber(EMPTY_SUBSCRIBER);
      setNotes("");
      setIsActive(true);
    }
    // The SSN field always starts blank: the API never hands back plaintext to prefill it with.
    setSubscriberSsn("");
  }, [open, policy, patient]);

  const isSelf = relationship === "Self";

  // When the patient is the subscriber the block mirrors their demographics live; otherwise it is
  // whatever the user typed. Keeping the two apart means switching the relationship back and forth
  // never carries one identity into the other — see the seeding note above.
  const effectiveSubscriber = useMemo(
    () => (isSelf ? subscriberFromPatient(patient) : subscriber),
    [isSelf, patient, subscriber],
  );

  const setField = (key: keyof Subscriber) => (value: string) =>
    setSubscriber((prev) => ({ ...prev, [key]: value }));

  const invalidate = () => {
    void queryClient.invalidateQueries({ queryKey: ["patient-insurance-policies", patient.id] });
  };

  const createMutation = useMutation({
    mutationFn: createInsurancePolicy,
    onSuccess: () => {
      toast.success("Insurance policy added.");
      invalidate();
      onClose();
    },
    onError: (err) => toast.error("Failed to add insurance policy.", { description: describe(err) }),
  });

  const updateMutation = useMutation({
    mutationFn: updateInsurancePolicy,
    onSuccess: () => {
      toast.success("Insurance policy updated.");
      invalidate();
      onClose();
    },
    onError: (err) =>
      toast.error("Failed to update insurance policy.", { description: describe(err) }),
  });

  const isPending = createMutation.isPending || updateMutation.isPending;

  // Mirrors the server rules: an insurer is always required, and a subscriber who is not the patient
  // must be identified by name and date of birth.
  const subscriberIdentified =
    isSelf ||
    (!!effectiveSubscriber.firstName.trim() &&
      !!effectiveSubscriber.lastName.trim() &&
      !!effectiveSubscriber.dateOfBirth);
  const datesOrdered =
    !effectiveDate || !expirationDate || expirationDate >= effectiveDate;
  const canSubmit = !!insuranceCompanyId && subscriberIdentified && datesOrdered;

  const onSubmit = (e: FormEvent<HTMLFormElement>) => {
    e.preventDefault();
    if (!insuranceCompanyId || !canSubmit) return;

    const fields = {
      insuranceCompanyId,
      insuranceTypeId,
      priority,
      policyNumber: policyNumber.trim() || null,
      groupNumber: groupNumber.trim() || null,
      memberId: memberId.trim() || null,
      coPay: numberOrNull(coPay),
      deductible: numberOrNull(deductible),
      effectiveDate: effectiveDate || null,
      expirationDate: expirationDate || null,
      subscriberRelationship: relationship,
      subscriberFirstName: effectiveSubscriber.firstName.trim() || null,
      subscriberLastName: effectiveSubscriber.lastName.trim() || null,
      subscriberDateOfBirth: effectiveSubscriber.dateOfBirth || null,
      subscriberGender: effectiveSubscriber.gender || null,
      // Blank means "leave the stored SSN unchanged" — never send the masked value back.
      subscriberSsn: subscriberSsn.trim() || null,
      subscriberEmployerName: effectiveSubscriber.employerName.trim() || null,
      subscriberAddress1: effectiveSubscriber.address1.trim() || null,
      subscriberAddress2: effectiveSubscriber.address2.trim() || null,
      subscriberCity: effectiveSubscriber.city.trim() || null,
      subscriberState: effectiveSubscriber.state.trim() || null,
      subscriberZipCode: effectiveSubscriber.zipCode.trim() || null,
      notes: notes.trim() || null,
      isActive,
    };

    if (isEdit && policy) {
      updateMutation.mutate({ ...fields, policyId: policy.id });
    } else {
      createMutation.mutate({ ...fields, patientId: patient.id });
    }
  };

  return (
    <Dialog open={open} onOpenChange={(o) => (!o ? onClose() : undefined)}>
      <DialogContent className="!max-w-3xl">
        <form onSubmit={onSubmit}>
          <DialogHeader>
            <DialogTitle>{isEdit ? "Edit Insurance Policy" : "Add Insurance Policy"}</DialogTitle>
          </DialogHeader>

          <DialogBody className="space-y-6">
            <section className="grid grid-cols-1 gap-4 sm:grid-cols-2">
              <Field id="policy-company" label="Insurer">
                <Combobox
                  id="policy-company"
                  label="Insurer"
                  value={insuranceCompanyId}
                  onChange={setInsuranceCompanyId}
                  options={companyOptions}
                  placeholder="Select an insurer…"
                />
              </Field>

              <Field id="policy-type" label="Insurance Type">
                <Combobox
                  id="policy-type"
                  label="Insurance Type"
                  value={insuranceTypeId}
                  onChange={setInsuranceTypeId}
                  options={typeOptions}
                  placeholder="Select a type…"
                  clearable
                />
              </Field>

              <Field id="policy-priority" label="Priority">
                <Combobox
                  id="policy-priority"
                  label="Priority"
                  value={priority}
                  onChange={(v) => setPriority((v as InsurancePriority) ?? "Primary")}
                  options={INSURANCE_PRIORITY_OPTIONS}
                />
              </Field>

              <Field id="policy-member-id" label="Insured's I.D.">
                <Input
                  id="policy-member-id"
                  value={memberId}
                  maxLength={50}
                  onChange={(e) => setMemberId(e.target.value)}
                />
              </Field>

              <Field id="policy-number" label="Policy Number">
                <Input
                  id="policy-number"
                  value={policyNumber}
                  maxLength={50}
                  onChange={(e) => setPolicyNumber(e.target.value)}
                />
              </Field>

              <Field id="policy-group" label="Group Number">
                <Input
                  id="policy-group"
                  value={groupNumber}
                  maxLength={50}
                  onChange={(e) => setGroupNumber(e.target.value)}
                />
              </Field>

              <Field id="policy-copay" label="Copay">
                <Input
                  id="policy-copay"
                  type="number"
                  min="0"
                  step="0.01"
                  value={coPay}
                  onChange={(e) => setCoPay(e.target.value)}
                />
              </Field>

              <Field id="policy-deductible" label="Deductible">
                <Input
                  id="policy-deductible"
                  type="number"
                  min="0"
                  step="0.01"
                  value={deductible}
                  onChange={(e) => setDeductible(e.target.value)}
                />
              </Field>

              <Field id="policy-effective" label="Plan Effective Date">
                <Input
                  id="policy-effective"
                  type="date"
                  value={effectiveDate}
                  onChange={(e) => setEffectiveDate(e.target.value)}
                />
              </Field>

              <Field id="policy-expiration" label="Plan Expiration Date">
                <Input
                  id="policy-expiration"
                  type="date"
                  value={expirationDate}
                  onChange={(e) => setExpirationDate(e.target.value)}
                  aria-invalid={!datesOrdered}
                />
              </Field>
            </section>

            {!datesOrdered && (
              <p role="alert" className="text-[12px] text-[var(--color-destructive)]">
                The plan expiration date cannot precede the plan effective date.
              </p>
            )}

            <section className="space-y-4 border-t border-[var(--color-border)] pt-5">
              <div className="flex flex-wrap items-end justify-between gap-3">
                <h3 className="text-[13px] font-semibold">Insured / Subscriber</h3>
                <div className="w-full sm:w-56">
                  <Field id="policy-relationship" label="Relationship to Insured">
                    <Combobox
                      id="policy-relationship"
                      label="Relationship to Insured"
                      value={relationship}
                      onChange={(v) => setRelationship((v as SubscriberRelationship) ?? "Self")}
                      options={SUBSCRIBER_RELATIONSHIP_OPTIONS}
                    />
                  </Field>
                </div>
              </div>

              {isSelf && (
                <p className="text-[12px] text-[var(--color-muted-foreground)]">
                  The patient is the policy holder — these fields are filled from their demographics.
                </p>
              )}

              <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
                <Field id="policy-sub-first" label="Insured's First Name">
                  <Input
                    id="policy-sub-first"
                    value={effectiveSubscriber.firstName}
                    maxLength={100}
                    disabled={isSelf}
                    required={!isSelf}
                    onChange={(e) => setField("firstName")(e.target.value)}
                  />
                </Field>

                <Field id="policy-sub-last" label="Insured's Last Name">
                  <Input
                    id="policy-sub-last"
                    value={effectiveSubscriber.lastName}
                    maxLength={100}
                    disabled={isSelf}
                    required={!isSelf}
                    onChange={(e) => setField("lastName")(e.target.value)}
                  />
                </Field>

                <Field id="policy-sub-dob" label="Insured's Date of Birth">
                  <Input
                    id="policy-sub-dob"
                    type="date"
                    value={effectiveSubscriber.dateOfBirth}
                    disabled={isSelf}
                    required={!isSelf}
                    onChange={(e) => setField("dateOfBirth")(e.target.value)}
                  />
                </Field>

                <Field id="policy-sub-gender" label="Insured's Gender">
                  <Combobox
                    id="policy-sub-gender"
                    label="Insured's Gender"
                    value={effectiveSubscriber.gender || null}
                    onChange={(v) => setField("gender")(v ?? "")}
                    options={GENDER_OPTIONS}
                    disabled={isSelf}
                    clearable
                  />
                </Field>

                <Field
                  id="policy-sub-ssn"
                  label={
                    policy?.subscriberSsnMasked
                      ? `Insured's SSN (on file: ${policy.subscriberSsnMasked})`
                      : "Insured's SSN"
                  }
                >
                  <Input
                    id="policy-sub-ssn"
                    value={subscriberSsn}
                    maxLength={15}
                    autoComplete="off"
                    placeholder={policy?.subscriberSsnMasked ? "Leave blank to keep" : ""}
                    onChange={(e) => setSubscriberSsn(e.target.value)}
                  />
                </Field>

                <Field id="policy-sub-employer" label="Insured's Employer">
                  <Input
                    id="policy-sub-employer"
                    value={effectiveSubscriber.employerName}
                    maxLength={200}
                    disabled={isSelf}
                    onChange={(e) => setField("employerName")(e.target.value)}
                  />
                </Field>

                <Field id="policy-sub-address1" label="Address 1">
                  <Input
                    id="policy-sub-address1"
                    value={effectiveSubscriber.address1}
                    maxLength={200}
                    disabled={isSelf}
                    onChange={(e) => setField("address1")(e.target.value)}
                  />
                </Field>

                <Field id="policy-sub-address2" label="Address 2">
                  <Input
                    id="policy-sub-address2"
                    value={effectiveSubscriber.address2}
                    maxLength={200}
                    disabled={isSelf}
                    onChange={(e) => setField("address2")(e.target.value)}
                  />
                </Field>

                <Field id="policy-sub-city" label="City">
                  <Input
                    id="policy-sub-city"
                    value={effectiveSubscriber.city}
                    maxLength={100}
                    disabled={isSelf}
                    onChange={(e) => setField("city")(e.target.value)}
                  />
                </Field>

                <Field id="policy-sub-state" label="State">
                  <Input
                    id="policy-sub-state"
                    value={effectiveSubscriber.state}
                    maxLength={2}
                    disabled={isSelf}
                    onChange={(e) => setField("state")(e.target.value.toUpperCase())}
                  />
                </Field>

                <Field id="policy-sub-zip" label="Zip">
                  <Input
                    id="policy-sub-zip"
                    value={effectiveSubscriber.zipCode}
                    maxLength={10}
                    disabled={isSelf}
                    onChange={(e) => setField("zipCode")(e.target.value)}
                  />
                </Field>
              </div>
            </section>

            <Field id="policy-notes" label="Notes">
              <Textarea
                id="policy-notes"
                value={notes}
                rows={3}
                maxLength={1000}
                onChange={(e) => setNotes(e.target.value)}
              />
            </Field>

            {canToggleActive && (
              <div className="flex items-center gap-4 text-[13px]">
                <label className="flex items-center gap-2 cursor-pointer">
                  <input
                    type="radio"
                    name="policy-active"
                    checked={isActive}
                    onChange={() => setIsActive(true)}
                  />
                  <span>Active</span>
                </label>
                <label className="flex items-center gap-2 cursor-pointer">
                  <input
                    type="radio"
                    name="policy-active"
                    checked={!isActive}
                    onChange={() => setIsActive(false)}
                  />
                  <span>Inactive</span>
                </label>
              </div>
            )}

            {isEdit && policy?.createdByName && (
              <p className="text-[12px] text-[var(--color-muted-foreground)]">
                Created: {policy.createdByName}
                {policy.updatedByName ? ` · Last modified: ${policy.updatedByName}` : ""}
              </p>
            )}
          </DialogBody>

          <DialogFooter>
            <DialogClose asChild>
              <Button type="button" variant="outline" disabled={isPending}>
                Cancel
              </Button>
            </DialogClose>
            <Button type="submit" disabled={isPending || !canSubmit}>
              {isPending ? "Saving…" : isEdit ? "Save Changes" : "Add Policy"}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}
