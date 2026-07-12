import { useEffect, useState, type FormEvent } from "react";
import { useMutation, useQueryClient } from "@tanstack/react-query";
import {
  Banknote,
  Briefcase,
  ClipboardList,
  IdCard,
  Info,
  Lock,
  MapPin,
  Pencil,
  Power,
  PowerOff,
  Trash2,
  Users,
  UserSquare2,
} from "lucide-react";
import { toast } from "sonner";
import {
  deletePatient,
  updatePatient,
  type PatientDetailDto,
  type UpdatePatientInput,
} from "@/api/patients";
import { mergePatientUpdate } from "@/pages/patients/patient-mappers";
import {
  GENDER_OPTIONS,
  MARITAL_STATUS_OPTIONS,
  RELATION_OPTIONS,
  findRelationOption,
  useRaceOptions,
  useEthnicityOptions,
  useLanguageOptions,
  useSmokingStatusOptions,
  useContactMethodOptions,
  useReferralTypeOptions,
} from "@/lib/patient-lookups";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Switch } from "@/components/ui/switch";
import {
  Dialog,
  DialogBody,
  DialogClose,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import {
  Combobox,
  EntityDetailSection,
  EntityStatusBadge,
  Field,
} from "@/components/list";
import { cn } from "@/lib/cn";
import { describe, formatDate, formatDateTimeMono, formatRelative } from "@/lib/list-helpers";

type DialogState =
  | { mode: "closed" }
  | { mode: "edit-demographics" }
  | { mode: "edit-contact" }
  | { mode: "edit-phi" }
  | { mode: "edit-employment" }
  | { mode: "edit-guardian" }
  | { mode: "edit-next-of-kin" }
  | { mode: "edit-insurance" }
  | { mode: "edit-flags" }
  | { mode: "toggle-status" }
  | { mode: "delete" };

export function PatientInfoEditor({
  patient,
  onDeleted,
}: {
  patient: PatientDetailDto;
  onDeleted: () => void;
}) {
  const [dialog, setDialog] = useState<DialogState>({ mode: "closed" });

  return (
    <div className="space-y-5">
      {/* Action bar — status + lifecycle actions (previously the page hero's actions) */}
      <div className="flex flex-wrap items-center justify-between gap-3">
        <div className="flex items-center gap-2">
          {patient.isActive ? (
            <EntityStatusBadge tone="success">Active</EntityStatusBadge>
          ) : (
            <EntityStatusBadge tone="default">Inactive</EntityStatusBadge>
          )}
          {patient.demographics.isMinor && <EntityStatusBadge tone="info">Minor</EntityStatusBadge>}
        </div>
        <div className="flex items-center gap-2">
          <Button variant="outline" size="sm" onClick={() => setDialog({ mode: "toggle-status" })} className="gap-1.5">
            {patient.isActive ? <PowerOff className="h-3.5 w-3.5" /> : <Power className="h-3.5 w-3.5" />}
            <span className="hidden sm:inline">{patient.isActive ? "Deactivate" : "Reactivate"}</span>
          </Button>
          <Button
            variant="outline"
            size="sm"
            onClick={() => setDialog({ mode: "delete" })}
            className="gap-1.5 hover:!border-[var(--color-destructive)] hover:!text-[var(--color-destructive)]"
          >
            <Trash2 className="h-3.5 w-3.5" />
            <span className="hidden sm:inline">Delete</span>
          </Button>
        </div>
      </div>

      <div className="grid grid-cols-1 gap-5 lg:grid-cols-[300px_1fr]">
        {/* Left: demographics + phi + flags + audit */}
        <aside className="space-y-5">
          <EntityDetailSection
            title="Demographics"
            icon={IdCard}
            action={<EditButton onClick={() => setDialog({ mode: "edit-demographics" })} />}
          >
            <DemographicsPanel patient={patient} />
          </EntityDetailSection>
          <EntityDetailSection
            title="Protected health info"
            icon={Lock}
            description="SSN is encrypted at rest and only ever shown masked."
            action={<EditButton onClick={() => setDialog({ mode: "edit-phi" })} />}
          >
            <PhiPanel patient={patient} />
          </EntityDetailSection>
          <EntityDetailSection
            title="Flags &amp; visits"
            icon={ClipboardList}
            action={<EditButton onClick={() => setDialog({ mode: "edit-flags" })} />}
          >
            <FlagsPanel patient={patient} />
          </EntityDetailSection>
          <EntityDetailSection title="Audit" icon={Info}>
            <AuditPanel patient={patient} />
          </EntityDetailSection>
        </aside>

        {/* Right: contact + kin + employment + guardian + insurance */}
        <div className="space-y-5">
          <EntityDetailSection
            title="Contact"
            icon={MapPin}
            action={<EditButton onClick={() => setDialog({ mode: "edit-contact" })} />}
          >
            <ContactPanel patient={patient} />
          </EntityDetailSection>
          <EntityDetailSection
            title="Next of kin"
            icon={Users}
            action={<EditButton onClick={() => setDialog({ mode: "edit-next-of-kin" })} />}
          >
            <NextOfKinPanel patient={patient} />
          </EntityDetailSection>
          <EntityDetailSection
            title="Employment"
            icon={Briefcase}
            action={<EditButton onClick={() => setDialog({ mode: "edit-employment" })} />}
          >
            <EmploymentPanel patient={patient} />
          </EntityDetailSection>
          {patient.demographics.isMinor && (
            <EntityDetailSection
              title="Guardian"
              icon={UserSquare2}
              description="Required while this patient is recorded as a minor."
              action={<EditButton onClick={() => setDialog({ mode: "edit-guardian" })} />}
            >
              <GuardianPanel patient={patient} />
            </EntityDetailSection>
          )}
          <EntityDetailSection
            title="Insurance"
            icon={Banknote}
            action={<EditButton onClick={() => setDialog({ mode: "edit-insurance" })} />}
          >
            <InsurancePanel patient={patient} />
          </EntityDetailSection>
        </div>
      </div>

      {/* Nested edit dialogs (stack on top of the container dialog) */}
      <DemographicsDialog open={dialog.mode === "edit-demographics"} patient={patient} onClose={() => setDialog({ mode: "closed" })} />
      <ContactDialog open={dialog.mode === "edit-contact"} patient={patient} onClose={() => setDialog({ mode: "closed" })} />
      <PhiDialog open={dialog.mode === "edit-phi"} patient={patient} onClose={() => setDialog({ mode: "closed" })} />
      <EmploymentDialog open={dialog.mode === "edit-employment"} patient={patient} onClose={() => setDialog({ mode: "closed" })} />
      <GuardianDialog open={dialog.mode === "edit-guardian"} patient={patient} onClose={() => setDialog({ mode: "closed" })} />
      <NextOfKinDialog open={dialog.mode === "edit-next-of-kin"} patient={patient} onClose={() => setDialog({ mode: "closed" })} />
      <InsuranceDialog open={dialog.mode === "edit-insurance"} patient={patient} onClose={() => setDialog({ mode: "closed" })} />
      <FlagsDialog open={dialog.mode === "edit-flags"} patient={patient} onClose={() => setDialog({ mode: "closed" })} />
      <ToggleStatusDialog open={dialog.mode === "toggle-status"} patient={patient} onClose={() => setDialog({ mode: "closed" })} />
      <DeleteDialog
        open={dialog.mode === "delete"}
        patient={patient}
        onClose={() => setDialog({ mode: "closed" })}
        onDeleted={onDeleted}
      />
    </div>
  );
}

function EditButton({ onClick }: { onClick: () => void }) {
  return (
    <Button variant="outline" size="sm" onClick={onClick} className="gap-1.5">
      <Pencil className="h-3.5 w-3.5" />
      Edit
    </Button>
  );
}

// ───────────────────────────────────────────────────────────────────────
//  Date / name helpers
// ───────────────────────────────────────────────────────────────────────

function fullName(detail: PatientDetailDto): string {
  const { firstName, middleInitial, lastName } = detail.demographics;
  return [firstName, middleInitial, lastName].filter(Boolean).join(" ");
}

/** `<input type="date">` needs `yyyy-MM-dd` — trims any time component the API returns. */
function toDateInputValue(value: string | null | undefined): string {
  return value ? value.slice(0, 10) : "";
}

function fromDateInputValue(value: string): string | null {
  return value || null;
}

// ───────────────────────────────────────────────────────────────────────
//  Read-only panels
// ───────────────────────────────────────────────────────────────────────

function DemographicsPanel({ patient }: { patient: PatientDetailDto }) {
  const { demographics } = patient;
  const raceOptions = useRaceOptions() ?? [];
  const ethnicityOptions = useEthnicityOptions() ?? [];
  const languageOptions = useLanguageOptions() ?? [];
  const smokingStatusOptions = useSmokingStatusOptions() ?? [];
  return (
    <dl className="space-y-3 text-[13px]">
      <MetaRow label="Gender" value={findOptionLabel(GENDER_OPTIONS, demographics.gender)} />
      <MetaRow
        label="Marital status"
        value={findOptionLabel(MARITAL_STATUS_OPTIONS, demographics.maritalStatus) ?? "—"}
      />
      <MetaRow label="Race" value={findOptionLabel(raceOptions, demographics.raceId != null ? String(demographics.raceId) : null) ?? "—"} />
      <MetaRow label="Ethnicity" value={findOptionLabel(ethnicityOptions, demographics.ethnicityId != null ? String(demographics.ethnicityId) : null) ?? "—"} />
      <MetaRow label="Language" value={findOptionLabel(languageOptions, demographics.languageId != null ? String(demographics.languageId) : null) ?? "—"} />
      <MetaRow
        label="Smoking"
        value={findOptionLabel(smokingStatusOptions, demographics.smokingStatusId != null ? String(demographics.smokingStatusId) : null) ?? "—"}
      />
      {demographics.medicalAlertNotes && (
        <MetaRow label="Alerts" value={demographics.medicalAlertNotes} tone="warning" />
      )}
    </dl>
  );
}

function PhiPanel({ patient }: { patient: PatientDetailDto }) {
  return (
    <dl className="space-y-3 text-[13px]">
      <MetaRow
        label="SSN"
        value={
          patient.phi.ssnMasked ? (
            <code className="font-mono text-[12.5px]">{patient.phi.ssnMasked}</code>
          ) : (
            "Not on file"
          )
        }
      />
    </dl>
  );
}

function FlagsPanel({ patient }: { patient: PatientDetailDto }) {
  return (
    <dl className="space-y-3 text-[13px]">
      <MetaRow
        label="No known problems"
        value={patient.hasNoKnownProblems ? "Confirmed" : "Not confirmed"}
        tone={patient.hasNoKnownProblems ? "success" : "muted"}
      />
      <MetaRow
        label="No known meds"
        value={patient.hasNoKnownMedications ? "Confirmed" : "Not confirmed"}
        tone={patient.hasNoKnownMedications ? "success" : "muted"}
      />
      <MetaRow
        label="No known allergies"
        value={patient.hasNoKnownAllergies ? "Confirmed" : "Not confirmed"}
        tone={patient.hasNoKnownAllergies ? "success" : "muted"}
      />
      <MetaRow
        label="Email reminders"
        value={patient.receivesEmailReminders ? "Enabled" : "Disabled"}
        tone={patient.receivesEmailReminders ? "success" : "muted"}
      />
    </dl>
  );
}

function AuditPanel({ patient }: { patient: PatientDetailDto }) {
  return (
    <dl className="space-y-3 text-[13px]">
      <MetaRow
        label="Created"
        value={formatDateTimeMono(patient.createdAtUtc)}
        hint={formatRelative(patient.createdAtUtc)}
      />
      {patient.updatedAtUtc ? (
        <MetaRow
          label="Revised"
          value={formatDateTimeMono(patient.updatedAtUtc)}
          hint={formatRelative(patient.updatedAtUtc)}
        />
      ) : (
        <MetaRow label="Revised" value="Never" hint="no edits since creation" />
      )}
      <MetaRow
        label="Status"
        value={patient.isActive ? "Active" : "Inactive"}
        tone={patient.isActive ? "success" : "muted"}
      />
    </dl>
  );
}

function ContactPanel({ patient }: { patient: PatientDetailDto }) {
  const { contact } = patient;
  const contactMethodOptions = useContactMethodOptions() ?? [];
  const addressParts = [contact.address1, contact.address2, contact.city, contact.state, contact.zipCode].filter(
    Boolean,
  );
  return (
    <dl className="grid grid-cols-1 gap-x-6 gap-y-3 text-[13px] sm:grid-cols-2">
      <MetaRow label="Address" value={addressParts.length > 0 ? addressParts.join(", ") : "—"} />
      <MetaRow label="Phone" value={contact.phone ?? "—"} />
      <MetaRow label="Cell phone" value={contact.cellPhone ?? "—"} />
      <MetaRow label="Email" value={contact.email ?? "—"} />
      <MetaRow
        label="Preferred contact"
        value={
          findOptionLabel(
            contactMethodOptions,
            contact.preferredContactMethodId != null ? String(contact.preferredContactMethodId) : null,
          ) ?? "—"
        }
      />
    </dl>
  );
}

function NextOfKinPanel({ patient }: { patient: PatientDetailDto }) {
  const kin = patient.nextOfKin;
  if (!kin || (!kin.firstName && !kin.lastName)) {
    return <EmptySection label="No next of kin on file." />;
  }
  return (
    <dl className="grid grid-cols-1 gap-x-6 gap-y-3 text-[13px] sm:grid-cols-2">
      <MetaRow label="Name" value={[kin.firstName, kin.lastName].filter(Boolean).join(" ") || "—"} />
      <MetaRow label="Phone" value={kin.phone ?? "—"} />
      <MetaRow label="Relation" value={kin.relation ?? "—"} />
      <MetaRow label="Role code" value={kin.relationRoleCode ? <IdCode value={kin.relationRoleCode} /> : "—"} />
    </dl>
  );
}

function EmploymentPanel({ patient }: { patient: PatientDetailDto }) {
  const employment = patient.employment;
  if (!employment || (!employment.occupation && !employment.employerName)) {
    return <EmptySection label="No employment info on file." />;
  }
  const addressParts = [
    employment.employerAddress1,
    employment.employerAddress2,
    employment.employerCity,
    employment.employerState,
    employment.employerZipCode,
  ].filter(Boolean);
  return (
    <dl className="grid grid-cols-1 gap-x-6 gap-y-3 text-[13px] sm:grid-cols-2">
      <MetaRow label="Occupation" value={employment.occupation ?? "—"} />
      <MetaRow label="Employer" value={employment.employerName ?? "—"} />
      <MetaRow label="Employer address" value={addressParts.length > 0 ? addressParts.join(", ") : "—"} />
      <MetaRow label="Employer phone" value={employment.employerPhone ?? "—"} />
    </dl>
  );
}

function GuardianPanel({ patient }: { patient: PatientDetailDto }) {
  const guardian = patient.guardian;
  if (!guardian) {
    return <EmptySection label="No guardian on file yet." />;
  }
  return (
    <dl className="grid grid-cols-1 gap-x-6 gap-y-3 text-[13px] sm:grid-cols-2">
      <MetaRow
        label="Name"
        value={[guardian.firstName, guardian.middleInitial, guardian.lastName].filter(Boolean).join(" ") || "—"}
      />
      <MetaRow label="Date of birth" value={guardian.dateOfBirth ? formatDate(guardian.dateOfBirth) : "—"} />
      <MetaRow label="Phone" value={guardian.phone ?? "—"} />
      <MetaRow label="Cell phone" value={guardian.cellPhone ?? "—"} />
      <MetaRow label="Employer" value={guardian.employerName ?? "—"} />
    </dl>
  );
}

function InsurancePanel({ patient }: { patient: PatientDetailDto }) {
  const insurance = patient.insurance;
  const referralTypeOptions = useReferralTypeOptions() ?? [];
  if (!insurance || !insurance.insuredFullName) {
    return <EmptySection label="No insurance info on file." />;
  }
  return (
    <dl className="grid grid-cols-1 gap-x-6 gap-y-3 text-[13px] sm:grid-cols-2">
      <MetaRow label="Insured name" value={insurance.insuredFullName ?? "—"} />
      <MetaRow
        label="Insured DOB"
        value={insurance.insuredDateOfBirth ? formatDate(insurance.insuredDateOfBirth) : "—"}
      />
      <MetaRow label="Insured employer" value={insurance.insuredEmployerName ?? "—"} />
      <MetaRow
        label="Referral type"
        value={
          findOptionLabel(referralTypeOptions, insurance.referralTypeId != null ? String(insurance.referralTypeId) : null) ??
          "—"
        }
      />
    </dl>
  );
}

// ───────────────────────────────────────────────────────────────────────
//  Small display helpers
// ───────────────────────────────────────────────────────────────────────

function findOptionLabel(options: { value: string; label: string }[], value: string | null | undefined): string | null {
  if (!value) return null;
  return options.find((o) => o.value === value)?.label ?? value;
}

function MetaRow({
  label,
  value,
  hint,
  tone = "default",
}: {
  label: string;
  value: React.ReactNode;
  hint?: string;
  tone?: "default" | "success" | "muted" | "warning";
}) {
  return (
    <div className="grid grid-cols-[120px_1fr] items-baseline gap-3">
      <dt className="text-[11px] font-medium uppercase tracking-wider text-[var(--color-muted-foreground)]">
        {label}
      </dt>
      <dd
        className={cn(
          "min-w-0 text-[13px]",
          tone === "success" && "text-[var(--color-success)]",
          tone === "muted" && "text-[var(--color-muted-foreground)]",
          tone === "warning" && "text-[var(--color-warning)]",
        )}
      >
        <div className="truncate">{value}</div>
        {hint && (
          <div className="mt-0.5 text-[11px] text-[var(--color-muted-foreground)]/70">{hint}</div>
        )}
      </dd>
    </div>
  );
}

function IdCode({ value }: { value: string }) {
  return (
    <code
      title={value}
      className="block max-w-full truncate rounded bg-[var(--color-muted)] px-1.5 py-0.5 font-mono text-[10.5px] tracking-tight text-[var(--color-muted-foreground)]"
    >
      {value}
    </code>
  );
}

function EmptySection({ label }: { label: string }) {
  return (
    <p className="text-[13px] italic leading-relaxed text-[var(--color-muted-foreground)]">{label}</p>
  );
}

// ───────────────────────────────────────────────────────────────────────
//  Edit dialogs — each merges its own fields on top of the patient's full
//  current state via mergePatientUpdate, since UpdatePatientCommand is a
//  single full-replace PUT (see patient-mappers.ts).
// ───────────────────────────────────────────────────────────────────────

function useUpdateMutation(_patient: PatientDetailDto, onClose: () => void, successMessage: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (input: UpdatePatientInput) => updatePatient(input),
    onSuccess: () => {
      toast.success(successMessage);
      // Prefix invalidation refreshes the chart's Patient Info card
      // (["patients", patientId]), the dialog's own query (same key), and
      // the list (["patients","list"]) in one shot.
      void queryClient.invalidateQueries({ queryKey: ["patients"] });
      onClose();
    },
    onError: (err: unknown) => toast.error("Update failed", { description: describe(err) }),
  });
}

function DemographicsDialog({
  open,
  patient,
  onClose,
}: {
  open: boolean;
  patient: PatientDetailDto;
  onClose: () => void;
}) {
  const { demographics } = patient;
  const raceOptions = useRaceOptions() ?? [];
  const ethnicityOptions = useEthnicityOptions() ?? [];
  const languageOptions = useLanguageOptions() ?? [];
  const smokingStatusOptions = useSmokingStatusOptions() ?? [];
  const [firstName, setFirstName] = useState(demographics.firstName);
  const [lastName, setLastName] = useState(demographics.lastName);
  const [middleInitial, setMiddleInitial] = useState(demographics.middleInitial ?? "");
  const [dateOfBirth, setDateOfBirth] = useState(toDateInputValue(demographics.dateOfBirth));
  const [gender, setGender] = useState<string | null>(demographics.gender);
  const [maritalStatus, setMaritalStatus] = useState<string | null>(demographics.maritalStatus ?? null);
  const [raceId, setRaceId] = useState<string | null>(demographics.raceId != null ? String(demographics.raceId) : null);
  const [ethnicityId, setEthnicityId] = useState<string | null>(
    demographics.ethnicityId != null ? String(demographics.ethnicityId) : null,
  );
  const [languageId, setLanguageId] = useState<string | null>(
    demographics.languageId != null ? String(demographics.languageId) : null,
  );
  const [smokingStatusId, setSmokingStatusId] = useState<string | null>(
    demographics.smokingStatusId != null ? String(demographics.smokingStatusId) : null,
  );
  const [smokingStartDate, setSmokingStartDate] = useState(toDateInputValue(demographics.smokingStartDate));
  const [smokingEndDate, setSmokingEndDate] = useState(toDateInputValue(demographics.smokingEndDate));
  const [medicalAlertNotes, setMedicalAlertNotes] = useState(demographics.medicalAlertNotes ?? "");

  useEffect(() => {
    if (open) {
      setFirstName(demographics.firstName);
      setLastName(demographics.lastName);
      setMiddleInitial(demographics.middleInitial ?? "");
      setDateOfBirth(toDateInputValue(demographics.dateOfBirth));
      setGender(demographics.gender);
      setMaritalStatus(demographics.maritalStatus ?? null);
      setRaceId(demographics.raceId != null ? String(demographics.raceId) : null);
      setEthnicityId(demographics.ethnicityId != null ? String(demographics.ethnicityId) : null);
      setLanguageId(demographics.languageId != null ? String(demographics.languageId) : null);
      setSmokingStatusId(demographics.smokingStatusId != null ? String(demographics.smokingStatusId) : null);
      setSmokingStartDate(toDateInputValue(demographics.smokingStartDate));
      setSmokingEndDate(toDateInputValue(demographics.smokingEndDate));
      setMedicalAlertNotes(demographics.medicalAlertNotes ?? "");
    }
  }, [open, demographics]);

  const mutation = useUpdateMutation(patient, onClose, "Demographics updated");
  const trimmedFirst = firstName.trim();
  const trimmedLast = lastName.trim();
  const valid = trimmedFirst.length > 0 && trimmedLast.length > 0 && !!dateOfBirth && !!gender;

  const onSubmit = (e: FormEvent<HTMLFormElement>) => {
    e.preventDefault();
    if (!valid || !gender) return;
    mutation.mutate(
      mergePatientUpdate(patient, {
        firstName: trimmedFirst,
        lastName: trimmedLast,
        middleInitial: middleInitial.trim() || null,
        dateOfBirth,
        gender,
        maritalStatus,
        raceId: raceId != null ? Number(raceId) : null,
        ethnicityId: ethnicityId != null ? Number(ethnicityId) : null,
        languageId: languageId != null ? Number(languageId) : null,
        smokingStatusId: smokingStatusId != null ? Number(smokingStatusId) : null,
        smokingStartDate: fromDateInputValue(smokingStartDate),
        smokingEndDate: fromDateInputValue(smokingEndDate),
        medicalAlertNotes: medicalAlertNotes.trim() || null,
      }),
    );
  };

  return (
    <Dialog open={open} onOpenChange={(o) => (!o ? onClose() : undefined)}>
      <DialogContent className="!max-w-2xl">
        <form onSubmit={onSubmit}>
          <DialogHeader>
            <DialogTitle>Edit demographics</DialogTitle>
            <DialogDescription>
              Core identity details for {fullName(patient)}. Status (active/inactive) is changed
              from the action bar above instead.
            </DialogDescription>
          </DialogHeader>

          <DialogBody className="space-y-4">
            <div className="grid gap-3 sm:grid-cols-[1fr_1fr_72px]">
              <Field id="demo-first" label="First name" required>
                <Input id="demo-first" value={firstName} onChange={(e) => setFirstName(e.target.value)} required autoFocus />
              </Field>
              <Field id="demo-last" label="Last name" required>
                <Input id="demo-last" value={lastName} onChange={(e) => setLastName(e.target.value)} required />
              </Field>
              <Field id="demo-mi" label="M.I.">
                <Input id="demo-mi" value={middleInitial} onChange={(e) => setMiddleInitial(e.target.value)} maxLength={5} />
              </Field>
            </div>

            <div className="grid gap-3 sm:grid-cols-2">
              <Field id="demo-dob" label="Date of birth" required>
                <Input
                  id="demo-dob"
                  type="date"
                  value={dateOfBirth}
                  onChange={(e) => setDateOfBirth(e.target.value)}
                  max={new Date().toISOString().slice(0, 10)}
                  required
                />
              </Field>
              <Field id="demo-gender" label="Gender" required>
                <Combobox
                  id="demo-gender"
                  label="Gender"
                  value={gender}
                  onChange={setGender}
                  options={GENDER_OPTIONS}
                  required
                />
              </Field>
            </div>

            <div className="grid gap-3 sm:grid-cols-2">
              <Field id="demo-marital" label="Marital status">
                <Combobox
                  id="demo-marital"
                  label="Marital status"
                  value={maritalStatus}
                  onChange={setMaritalStatus}
                  options={MARITAL_STATUS_OPTIONS}
                  clearable
                />
              </Field>
              <Field id="demo-race" label="Race">
                <Combobox id="demo-race" label="Race" value={raceId} onChange={setRaceId} options={raceOptions} clearable />
              </Field>
            </div>

            <div className="grid gap-3 sm:grid-cols-2">
              <Field id="demo-ethnicity" label="Ethnicity">
                <Combobox
                  id="demo-ethnicity"
                  label="Ethnicity"
                  value={ethnicityId}
                  onChange={setEthnicityId}
                  options={ethnicityOptions}
                  clearable
                />
              </Field>
              <Field id="demo-language" label="Language">
                <Combobox
                  id="demo-language"
                  label="Language"
                  value={languageId}
                  onChange={setLanguageId}
                  options={languageOptions}
                  clearable
                />
              </Field>
            </div>

            <div className="grid gap-3 sm:grid-cols-3">
              <Field id="demo-smoking" label="Smoking status">
                <Combobox
                  id="demo-smoking"
                  label="Smoking status"
                  value={smokingStatusId}
                  onChange={setSmokingStatusId}
                  options={smokingStatusOptions}
                  clearable
                />
              </Field>
              <Field id="demo-smoke-start" label="Smoking start">
                <Input id="demo-smoke-start" type="date" value={smokingStartDate} onChange={(e) => setSmokingStartDate(e.target.value)} />
              </Field>
              <Field id="demo-smoke-end" label="Smoking end">
                <Input id="demo-smoke-end" type="date" value={smokingEndDate} onChange={(e) => setSmokingEndDate(e.target.value)} />
              </Field>
            </div>

            <Field id="demo-alerts" label="Medical alert notes">
              <textarea
                id="demo-alerts"
                value={medicalAlertNotes}
                onChange={(e) => setMedicalAlertNotes(e.target.value)}
                rows={3}
                maxLength={2000}
                className="flex w-full rounded-md border border-[var(--color-input)] bg-transparent px-3 py-2 text-sm shadow-sm placeholder:text-[var(--color-muted-foreground)] focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-[var(--color-ring)] focus-visible:ring-offset-2"
              />
            </Field>
          </DialogBody>

          <DialogFooter>
            <DialogClose asChild>
              <Button type="button" variant="outline" disabled={mutation.isPending}>
                Cancel
              </Button>
            </DialogClose>
            <Button type="submit" disabled={mutation.isPending || !valid}>
              {mutation.isPending ? "Saving…" : "Save changes"}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}

function ContactDialog({
  open,
  patient,
  onClose,
}: {
  open: boolean;
  patient: PatientDetailDto;
  onClose: () => void;
}) {
  const { contact } = patient;
  const contactMethodOptions = useContactMethodOptions() ?? [];
  const [address1, setAddress1] = useState(contact.address1 ?? "");
  const [address2, setAddress2] = useState(contact.address2 ?? "");
  const [city, setCity] = useState(contact.city ?? "");
  const [state, setState] = useState(contact.state ?? "");
  const [zipCode, setZipCode] = useState(contact.zipCode ?? "");
  const [phone, setPhone] = useState(contact.phone ?? "");
  const [phoneExtension, setPhoneExtension] = useState(contact.phoneExtension ?? "");
  const [cellPhone, setCellPhone] = useState(contact.cellPhone ?? "");
  const [email, setEmail] = useState(contact.email ?? "");
  const [preferredContactMethodId, setPreferredContactMethodId] = useState<string | null>(
    contact.preferredContactMethodId != null ? String(contact.preferredContactMethodId) : null,
  );

  useEffect(() => {
    if (open) {
      setAddress1(contact.address1 ?? "");
      setAddress2(contact.address2 ?? "");
      setCity(contact.city ?? "");
      setState(contact.state ?? "");
      setZipCode(contact.zipCode ?? "");
      setPhone(contact.phone ?? "");
      setPhoneExtension(contact.phoneExtension ?? "");
      setCellPhone(contact.cellPhone ?? "");
      setEmail(contact.email ?? "");
      setPreferredContactMethodId(contact.preferredContactMethodId != null ? String(contact.preferredContactMethodId) : null);
    }
  }, [open, contact]);

  const mutation = useUpdateMutation(patient, onClose, "Contact info updated");

  const onSubmit = (e: FormEvent<HTMLFormElement>) => {
    e.preventDefault();
    mutation.mutate(
      mergePatientUpdate(patient, {
        address1: address1.trim() || null,
        address2: address2.trim() || null,
        city: city.trim() || null,
        state: state.trim() || null,
        zipCode: zipCode.trim() || null,
        phone: phone.trim() || null,
        phoneExtension: phoneExtension.trim() || null,
        cellPhone: cellPhone.trim() || null,
        email: email.trim() || null,
        preferredContactMethodId: preferredContactMethodId != null ? Number(preferredContactMethodId) : null,
      }),
    );
  };

  return (
    <Dialog open={open} onOpenChange={(o) => (!o ? onClose() : undefined)}>
      <DialogContent className="!max-w-2xl">
        <form onSubmit={onSubmit}>
          <DialogHeader>
            <DialogTitle>Edit contact info</DialogTitle>
            <DialogDescription>Address and reachability for {fullName(patient)}.</DialogDescription>
          </DialogHeader>

          <DialogBody className="space-y-4">
            <Field id="contact-addr1" label="Address line 1">
              <Input id="contact-addr1" value={address1} onChange={(e) => setAddress1(e.target.value)} autoFocus />
            </Field>
            <Field id="contact-addr2" label="Address line 2">
              <Input id="contact-addr2" value={address2} onChange={(e) => setAddress2(e.target.value)} />
            </Field>
            <div className="grid gap-3 sm:grid-cols-[1fr_100px_120px]">
              <Field id="contact-city" label="City">
                <Input id="contact-city" value={city} onChange={(e) => setCity(e.target.value)} />
              </Field>
              <Field id="contact-state" label="State">
                <Input id="contact-state" value={state} onChange={(e) => setState(e.target.value)} maxLength={2} />
              </Field>
              <Field id="contact-zip" label="Zip code">
                <Input id="contact-zip" value={zipCode} onChange={(e) => setZipCode(e.target.value)} />
              </Field>
            </div>
            <div className="grid gap-3 sm:grid-cols-[1fr_100px]">
              <Field id="contact-phone" label="Phone">
                <Input id="contact-phone" value={phone} onChange={(e) => setPhone(e.target.value)} />
              </Field>
              <Field id="contact-ext" label="Ext.">
                <Input id="contact-ext" value={phoneExtension} onChange={(e) => setPhoneExtension(e.target.value)} />
              </Field>
            </div>
            <div className="grid gap-3 sm:grid-cols-2">
              <Field id="contact-cell" label="Cell phone">
                <Input id="contact-cell" value={cellPhone} onChange={(e) => setCellPhone(e.target.value)} />
              </Field>
              <Field id="contact-email" label="Email">
                <Input id="contact-email" type="email" value={email} onChange={(e) => setEmail(e.target.value)} />
              </Field>
            </div>
            <Field id="contact-preferred" label="Preferred contact method">
              <Combobox
                id="contact-preferred"
                label="Preferred contact method"
                value={preferredContactMethodId}
                onChange={setPreferredContactMethodId}
                options={contactMethodOptions}
                clearable
              />
            </Field>
          </DialogBody>

          <DialogFooter>
            <DialogClose asChild>
              <Button type="button" variant="outline" disabled={mutation.isPending}>
                Cancel
              </Button>
            </DialogClose>
            <Button type="submit" disabled={mutation.isPending}>
              {mutation.isPending ? "Saving…" : "Save changes"}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}

function PhiDialog({
  open,
  patient,
  onClose,
}: {
  open: boolean;
  patient: PatientDetailDto;
  onClose: () => void;
}) {
  const [ssn, setSsn] = useState("");
  const [guardianSsn, setGuardianSsn] = useState("");

  useEffect(() => {
    if (open) {
      setSsn("");
      setGuardianSsn("");
    }
  }, [open]);

  const mutation = useUpdateMutation(patient, onClose, "Protected health info updated");

  const onSubmit = (e: FormEvent<HTMLFormElement>) => {
    e.preventDefault();
    mutation.mutate(
      mergePatientUpdate(patient, {
        ssn: ssn.trim() || null,
        guardianSsn: guardianSsn.trim() || null,
      }),
    );
  };

  return (
    <Dialog open={open} onOpenChange={(o) => (!o ? onClose() : undefined)}>
      <DialogContent>
        <form onSubmit={onSubmit}>
          <DialogHeader>
            <DialogTitle>Edit protected health info</DialogTitle>
            <DialogDescription>
              Current SSN on file: {patient.phi.ssnMasked ?? "not on file"}. Leave a field blank to
              keep its current encrypted value unchanged — these are never re-displayed once saved.
            </DialogDescription>
          </DialogHeader>

          <DialogBody className="space-y-4">
            <Field id="phi-ssn" label="SSN" hint="Leave blank to keep the current value.">
              <Input
                id="phi-ssn"
                type="password"
                autoComplete="off"
                value={ssn}
                onChange={(e) => setSsn(e.target.value)}
                placeholder="•••-••-••••"
                autoFocus
              />
            </Field>
            <Field id="phi-guardian-ssn" label="Guardian SSN" hint="Leave blank to keep the current value.">
              <Input
                id="phi-guardian-ssn"
                type="password"
                autoComplete="off"
                value={guardianSsn}
                onChange={(e) => setGuardianSsn(e.target.value)}
                placeholder="•••-••-••••"
              />
            </Field>
          </DialogBody>

          <DialogFooter>
            <DialogClose asChild>
              <Button type="button" variant="outline" disabled={mutation.isPending}>
                Cancel
              </Button>
            </DialogClose>
            <Button type="submit" disabled={mutation.isPending}>
              {mutation.isPending ? "Saving…" : "Save changes"}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}

function EmploymentDialog({
  open,
  patient,
  onClose,
}: {
  open: boolean;
  patient: PatientDetailDto;
  onClose: () => void;
}) {
  const employment = patient.employment;
  const [occupation, setOccupation] = useState(employment?.occupation ?? "");
  const [employerName, setEmployerName] = useState(employment?.employerName ?? "");
  const [employerAddress1, setEmployerAddress1] = useState(employment?.employerAddress1 ?? "");
  const [employerAddress2, setEmployerAddress2] = useState(employment?.employerAddress2 ?? "");
  const [employerCity, setEmployerCity] = useState(employment?.employerCity ?? "");
  const [employerState, setEmployerState] = useState(employment?.employerState ?? "");
  const [employerZipCode, setEmployerZipCode] = useState(employment?.employerZipCode ?? "");
  const [employerPhone, setEmployerPhone] = useState(employment?.employerPhone ?? "");
  const [employerPhoneExtension, setEmployerPhoneExtension] = useState(employment?.employerPhoneExtension ?? "");

  useEffect(() => {
    if (open) {
      setOccupation(employment?.occupation ?? "");
      setEmployerName(employment?.employerName ?? "");
      setEmployerAddress1(employment?.employerAddress1 ?? "");
      setEmployerAddress2(employment?.employerAddress2 ?? "");
      setEmployerCity(employment?.employerCity ?? "");
      setEmployerState(employment?.employerState ?? "");
      setEmployerZipCode(employment?.employerZipCode ?? "");
      setEmployerPhone(employment?.employerPhone ?? "");
      setEmployerPhoneExtension(employment?.employerPhoneExtension ?? "");
    }
  }, [open, employment]);

  const mutation = useUpdateMutation(patient, onClose, "Employment info updated");

  const onSubmit = (e: FormEvent<HTMLFormElement>) => {
    e.preventDefault();
    mutation.mutate(
      mergePatientUpdate(patient, {
        occupation: occupation.trim() || null,
        employerName: employerName.trim() || null,
        employerAddress1: employerAddress1.trim() || null,
        employerAddress2: employerAddress2.trim() || null,
        employerCity: employerCity.trim() || null,
        employerState: employerState.trim() || null,
        employerZipCode: employerZipCode.trim() || null,
        employerPhone: employerPhone.trim() || null,
        employerPhoneExtension: employerPhoneExtension.trim() || null,
      }),
    );
  };

  return (
    <Dialog open={open} onOpenChange={(o) => (!o ? onClose() : undefined)}>
      <DialogContent className="!max-w-2xl">
        <form onSubmit={onSubmit}>
          <DialogHeader>
            <DialogTitle>Edit employment info</DialogTitle>
            <DialogDescription>Occupation and employer for {fullName(patient)}.</DialogDescription>
          </DialogHeader>

          <DialogBody className="space-y-4">
            <div className="grid gap-3 sm:grid-cols-2">
              <Field id="emp-occupation" label="Occupation">
                <Input id="emp-occupation" value={occupation} onChange={(e) => setOccupation(e.target.value)} autoFocus />
              </Field>
              <Field id="emp-name" label="Employer name">
                <Input id="emp-name" value={employerName} onChange={(e) => setEmployerName(e.target.value)} />
              </Field>
            </div>
            <Field id="emp-addr1" label="Employer address line 1">
              <Input id="emp-addr1" value={employerAddress1} onChange={(e) => setEmployerAddress1(e.target.value)} />
            </Field>
            <Field id="emp-addr2" label="Employer address line 2">
              <Input id="emp-addr2" value={employerAddress2} onChange={(e) => setEmployerAddress2(e.target.value)} />
            </Field>
            <div className="grid gap-3 sm:grid-cols-[1fr_100px_120px]">
              <Field id="emp-city" label="City">
                <Input id="emp-city" value={employerCity} onChange={(e) => setEmployerCity(e.target.value)} />
              </Field>
              <Field id="emp-state" label="State">
                <Input id="emp-state" value={employerState} onChange={(e) => setEmployerState(e.target.value)} maxLength={2} />
              </Field>
              <Field id="emp-zip" label="Zip code">
                <Input id="emp-zip" value={employerZipCode} onChange={(e) => setEmployerZipCode(e.target.value)} />
              </Field>
            </div>
            <div className="grid gap-3 sm:grid-cols-[1fr_100px]">
              <Field id="emp-phone" label="Employer phone">
                <Input id="emp-phone" value={employerPhone} onChange={(e) => setEmployerPhone(e.target.value)} />
              </Field>
              <Field id="emp-ext" label="Ext.">
                <Input id="emp-ext" value={employerPhoneExtension} onChange={(e) => setEmployerPhoneExtension(e.target.value)} />
              </Field>
            </div>
          </DialogBody>

          <DialogFooter>
            <DialogClose asChild>
              <Button type="button" variant="outline" disabled={mutation.isPending}>
                Cancel
              </Button>
            </DialogClose>
            <Button type="submit" disabled={mutation.isPending}>
              {mutation.isPending ? "Saving…" : "Save changes"}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}

function GuardianDialog({
  open,
  patient,
  onClose,
}: {
  open: boolean;
  patient: PatientDetailDto;
  onClose: () => void;
}) {
  const guardian = patient.guardian;
  const [firstName, setFirstName] = useState(guardian?.firstName ?? "");
  const [lastName, setLastName] = useState(guardian?.lastName ?? "");
  const [middleInitial, setMiddleInitial] = useState(guardian?.middleInitial ?? "");
  const [dateOfBirth, setDateOfBirth] = useState(toDateInputValue(guardian?.dateOfBirth));
  const [gender, setGender] = useState<string | null>(guardian?.gender ?? null);
  const [maritalStatus, setMaritalStatus] = useState<string | null>(guardian?.maritalStatus ?? null);
  const [address1, setAddress1] = useState(guardian?.address1 ?? "");
  const [address2, setAddress2] = useState(guardian?.address2 ?? "");
  const [city, setCity] = useState(guardian?.city ?? "");
  const [state, setState] = useState(guardian?.state ?? "");
  const [zipCode, setZipCode] = useState(guardian?.zipCode ?? "");
  const [phone, setPhone] = useState(guardian?.phone ?? "");
  const [cellPhone, setCellPhone] = useState(guardian?.cellPhone ?? "");
  const [employerName, setEmployerName] = useState(guardian?.employerName ?? "");
  const [employerAddress1, setEmployerAddress1] = useState(guardian?.employerAddress1 ?? "");
  const [employerAddress2, setEmployerAddress2] = useState(guardian?.employerAddress2 ?? "");
  const [employerCity, setEmployerCity] = useState(guardian?.employerCity ?? "");
  const [employerState, setEmployerState] = useState(guardian?.employerState ?? "");
  const [employerZipCode, setEmployerZipCode] = useState(guardian?.employerZipCode ?? "");

  useEffect(() => {
    if (open) {
      setFirstName(guardian?.firstName ?? "");
      setLastName(guardian?.lastName ?? "");
      setMiddleInitial(guardian?.middleInitial ?? "");
      setDateOfBirth(toDateInputValue(guardian?.dateOfBirth));
      setGender(guardian?.gender ?? null);
      setMaritalStatus(guardian?.maritalStatus ?? null);
      setAddress1(guardian?.address1 ?? "");
      setAddress2(guardian?.address2 ?? "");
      setCity(guardian?.city ?? "");
      setState(guardian?.state ?? "");
      setZipCode(guardian?.zipCode ?? "");
      setPhone(guardian?.phone ?? "");
      setCellPhone(guardian?.cellPhone ?? "");
      setEmployerName(guardian?.employerName ?? "");
      setEmployerAddress1(guardian?.employerAddress1 ?? "");
      setEmployerAddress2(guardian?.employerAddress2 ?? "");
      setEmployerCity(guardian?.employerCity ?? "");
      setEmployerState(guardian?.employerState ?? "");
      setEmployerZipCode(guardian?.employerZipCode ?? "");
    }
  }, [open, guardian]);

  const mutation = useUpdateMutation(patient, onClose, "Guardian info updated");
  const trimmedFirst = firstName.trim();
  const trimmedLast = lastName.trim();
  const valid = trimmedFirst.length > 0 && trimmedLast.length > 0;

  const onSubmit = (e: FormEvent<HTMLFormElement>) => {
    e.preventDefault();
    if (!valid) return;
    mutation.mutate(
      mergePatientUpdate(patient, {
        guardianFirstName: trimmedFirst,
        guardianLastName: trimmedLast,
        guardianMiddleInitial: middleInitial.trim() || null,
        guardianDateOfBirth: fromDateInputValue(dateOfBirth),
        guardianGender: gender,
        guardianMaritalStatus: maritalStatus,
        guardianAddress1: address1.trim() || null,
        guardianAddress2: address2.trim() || null,
        guardianCity: city.trim() || null,
        guardianState: state.trim() || null,
        guardianZipCode: zipCode.trim() || null,
        guardianPhone: phone.trim() || null,
        guardianCellPhone: cellPhone.trim() || null,
        guardianEmployerName: employerName.trim() || null,
        guardianEmployerAddress1: employerAddress1.trim() || null,
        guardianEmployerAddress2: employerAddress2.trim() || null,
        guardianEmployerCity: employerCity.trim() || null,
        guardianEmployerState: employerState.trim() || null,
        guardianEmployerZipCode: employerZipCode.trim() || null,
      }),
    );
  };

  return (
    <Dialog open={open} onOpenChange={(o) => (!o ? onClose() : undefined)}>
      <DialogContent className="!max-w-2xl">
        <form onSubmit={onSubmit}>
          <DialogHeader>
            <DialogTitle>Edit guardian info</DialogTitle>
            <DialogDescription>Required while {fullName(patient)} is recorded as a minor.</DialogDescription>
          </DialogHeader>

          <DialogBody className="space-y-4">
            <div className="grid gap-3 sm:grid-cols-[1fr_1fr_72px]">
              <Field id="grd-first" label="First name" required>
                <Input id="grd-first" value={firstName} onChange={(e) => setFirstName(e.target.value)} required autoFocus />
              </Field>
              <Field id="grd-last" label="Last name" required>
                <Input id="grd-last" value={lastName} onChange={(e) => setLastName(e.target.value)} required />
              </Field>
              <Field id="grd-mi" label="M.I.">
                <Input id="grd-mi" value={middleInitial} onChange={(e) => setMiddleInitial(e.target.value)} maxLength={5} />
              </Field>
            </div>
            <div className="grid gap-3 sm:grid-cols-3">
              <Field id="grd-dob" label="Date of birth">
                <Input id="grd-dob" type="date" value={dateOfBirth} onChange={(e) => setDateOfBirth(e.target.value)} />
              </Field>
              <Field id="grd-gender" label="Gender">
                <Combobox id="grd-gender" label="Gender" value={gender} onChange={setGender} options={GENDER_OPTIONS} clearable />
              </Field>
              <Field id="grd-marital" label="Marital status">
                <Combobox
                  id="grd-marital"
                  label="Marital status"
                  value={maritalStatus}
                  onChange={setMaritalStatus}
                  options={MARITAL_STATUS_OPTIONS}
                  clearable
                />
              </Field>
            </div>
            <Field id="grd-addr1" label="Address line 1">
              <Input id="grd-addr1" value={address1} onChange={(e) => setAddress1(e.target.value)} />
            </Field>
            <Field id="grd-addr2" label="Address line 2">
              <Input id="grd-addr2" value={address2} onChange={(e) => setAddress2(e.target.value)} />
            </Field>
            <div className="grid gap-3 sm:grid-cols-[1fr_100px_120px]">
              <Field id="grd-city" label="City">
                <Input id="grd-city" value={city} onChange={(e) => setCity(e.target.value)} />
              </Field>
              <Field id="grd-state" label="State">
                <Input id="grd-state" value={state} onChange={(e) => setState(e.target.value)} maxLength={2} />
              </Field>
              <Field id="grd-zip" label="Zip code">
                <Input id="grd-zip" value={zipCode} onChange={(e) => setZipCode(e.target.value)} />
              </Field>
            </div>
            <div className="grid gap-3 sm:grid-cols-2">
              <Field id="grd-phone" label="Phone">
                <Input id="grd-phone" value={phone} onChange={(e) => setPhone(e.target.value)} />
              </Field>
              <Field id="grd-cell" label="Cell phone">
                <Input id="grd-cell" value={cellPhone} onChange={(e) => setCellPhone(e.target.value)} />
              </Field>
            </div>
            <Field id="grd-employer" label="Employer name">
              <Input id="grd-employer" value={employerName} onChange={(e) => setEmployerName(e.target.value)} />
            </Field>
            <Field id="grd-employer-addr1" label="Employer address line 1">
              <Input id="grd-employer-addr1" value={employerAddress1} onChange={(e) => setEmployerAddress1(e.target.value)} />
            </Field>
            <Field id="grd-employer-addr2" label="Employer address line 2">
              <Input id="grd-employer-addr2" value={employerAddress2} onChange={(e) => setEmployerAddress2(e.target.value)} />
            </Field>
            <div className="grid gap-3 sm:grid-cols-[1fr_100px_120px]">
              <Field id="grd-employer-city" label="Employer city">
                <Input id="grd-employer-city" value={employerCity} onChange={(e) => setEmployerCity(e.target.value)} />
              </Field>
              <Field id="grd-employer-state" label="Employer state">
                <Input id="grd-employer-state" value={employerState} onChange={(e) => setEmployerState(e.target.value)} maxLength={2} />
              </Field>
              <Field id="grd-employer-zip" label="Employer zip">
                <Input id="grd-employer-zip" value={employerZipCode} onChange={(e) => setEmployerZipCode(e.target.value)} />
              </Field>
            </div>
          </DialogBody>

          <DialogFooter>
            <DialogClose asChild>
              <Button type="button" variant="outline" disabled={mutation.isPending}>
                Cancel
              </Button>
            </DialogClose>
            <Button type="submit" disabled={mutation.isPending || !valid}>
              {mutation.isPending ? "Saving…" : "Save changes"}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}

function NextOfKinDialog({
  open,
  patient,
  onClose,
}: {
  open: boolean;
  patient: PatientDetailDto;
  onClose: () => void;
}) {
  const kin = patient.nextOfKin;
  const [firstName, setFirstName] = useState(kin?.firstName ?? "");
  const [lastName, setLastName] = useState(kin?.lastName ?? "");
  const [phone, setPhone] = useState(kin?.phone ?? "");
  const [relationRoleCode, setRelationRoleCode] = useState<string | null>(kin?.relationRoleCode ?? null);

  useEffect(() => {
    if (open) {
      setFirstName(kin?.firstName ?? "");
      setLastName(kin?.lastName ?? "");
      setPhone(kin?.phone ?? "");
      setRelationRoleCode(kin?.relationRoleCode ?? null);
    }
  }, [open, kin]);

  const mutation = useUpdateMutation(patient, onClose, "Next of kin updated");
  const selectedRelation = findRelationOption(relationRoleCode);

  const onSubmit = (e: FormEvent<HTMLFormElement>) => {
    e.preventDefault();
    mutation.mutate(
      mergePatientUpdate(patient, {
        nextOfKinFirstName: firstName.trim() || null,
        nextOfKinLastName: lastName.trim() || null,
        nextOfKinPhone: phone.trim() || null,
        nextOfKinRelation: selectedRelation?.label ?? null,
        nextOfKinRelationRoleCode: relationRoleCode,
      }),
    );
  };

  return (
    <Dialog open={open} onOpenChange={(o) => (!o ? onClose() : undefined)}>
      <DialogContent className="!max-w-xl">
        <form onSubmit={onSubmit}>
          <DialogHeader>
            <DialogTitle>Edit next of kin</DialogTitle>
            <DialogDescription>
              Selecting a relation sets the role code automatically — it's the unique key for the
              relation and can't be typed independently.
            </DialogDescription>
          </DialogHeader>

          <DialogBody className="space-y-4">
            <div className="grid gap-3 sm:grid-cols-2">
              <Field id="kin-first" label="First name">
                <Input id="kin-first" value={firstName} onChange={(e) => setFirstName(e.target.value)} autoFocus />
              </Field>
              <Field id="kin-last" label="Last name">
                <Input id="kin-last" value={lastName} onChange={(e) => setLastName(e.target.value)} />
              </Field>
            </div>
            <Field id="kin-phone" label="Phone">
              <Input id="kin-phone" value={phone} onChange={(e) => setPhone(e.target.value)} />
            </Field>
            <div className="grid gap-3 sm:grid-cols-2">
              <Field id="kin-relation" label="Relation">
                <Combobox
                  id="kin-relation"
                  label="Relation"
                  value={relationRoleCode}
                  onChange={setRelationRoleCode}
                  options={RELATION_OPTIONS}
                  searchable
                  clearable
                />
              </Field>
              <Field id="kin-role-code" label="Relation role code" hint="Derived from the relation — read only.">
                <Input id="kin-role-code" value={relationRoleCode ?? ""} disabled className="font-mono text-[13px] tracking-tight" />
              </Field>
            </div>
          </DialogBody>

          <DialogFooter>
            <DialogClose asChild>
              <Button type="button" variant="outline" disabled={mutation.isPending}>
                Cancel
              </Button>
            </DialogClose>
            <Button type="submit" disabled={mutation.isPending}>
              {mutation.isPending ? "Saving…" : "Save changes"}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}

function InsuranceDialog({
  open,
  patient,
  onClose,
}: {
  open: boolean;
  patient: PatientDetailDto;
  onClose: () => void;
}) {
  const insurance = patient.insurance;
  const referralTypeOptions = useReferralTypeOptions() ?? [];
  const [insuredFullName, setInsuredFullName] = useState(insurance?.insuredFullName ?? "");
  const [insuredDateOfBirth, setInsuredDateOfBirth] = useState(toDateInputValue(insurance?.insuredDateOfBirth));
  const [insuredEmployerName, setInsuredEmployerName] = useState(insurance?.insuredEmployerName ?? "");
  const [referralTypeId, setReferralTypeId] = useState<string | null>(
    insurance?.referralTypeId != null ? String(insurance.referralTypeId) : null,
  );

  useEffect(() => {
    if (open) {
      setInsuredFullName(insurance?.insuredFullName ?? "");
      setInsuredDateOfBirth(toDateInputValue(insurance?.insuredDateOfBirth));
      setInsuredEmployerName(insurance?.insuredEmployerName ?? "");
      setReferralTypeId(insurance?.referralTypeId != null ? String(insurance.referralTypeId) : null);
    }
  }, [open, insurance]);

  const mutation = useUpdateMutation(patient, onClose, "Insurance info updated");

  const onSubmit = (e: FormEvent<HTMLFormElement>) => {
    e.preventDefault();
    mutation.mutate(
      mergePatientUpdate(patient, {
        insuredFullName: insuredFullName.trim() || null,
        insuredDateOfBirth: fromDateInputValue(insuredDateOfBirth),
        insuredEmployerName: insuredEmployerName.trim() || null,
        referralTypeId: referralTypeId != null ? Number(referralTypeId) : null,
      }),
    );
  };

  return (
    <Dialog open={open} onOpenChange={(o) => (!o ? onClose() : undefined)}>
      <DialogContent>
        <form onSubmit={onSubmit}>
          <DialogHeader>
            <DialogTitle>Edit insurance info</DialogTitle>
            <DialogDescription>Insured party and referral source for {fullName(patient)}.</DialogDescription>
          </DialogHeader>

          <DialogBody className="space-y-4">
            <Field id="ins-name" label="Insured full name">
              <Input id="ins-name" value={insuredFullName} onChange={(e) => setInsuredFullName(e.target.value)} autoFocus />
            </Field>
            <Field id="ins-dob" label="Insured date of birth">
              <Input id="ins-dob" type="date" value={insuredDateOfBirth} onChange={(e) => setInsuredDateOfBirth(e.target.value)} />
            </Field>
            <Field id="ins-employer" label="Insured employer">
              <Input id="ins-employer" value={insuredEmployerName} onChange={(e) => setInsuredEmployerName(e.target.value)} />
            </Field>
            <Field id="ins-referral" label="Referral type">
              <Combobox
                id="ins-referral"
                label="Referral type"
                value={referralTypeId}
                onChange={setReferralTypeId}
                options={referralTypeOptions}
                clearable
              />
            </Field>
          </DialogBody>

          <DialogFooter>
            <DialogClose asChild>
              <Button type="button" variant="outline" disabled={mutation.isPending}>
                Cancel
              </Button>
            </DialogClose>
            <Button type="submit" disabled={mutation.isPending}>
              {mutation.isPending ? "Saving…" : "Save changes"}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}

function FlagsDialog({
  open,
  patient,
  onClose,
}: {
  open: boolean;
  patient: PatientDetailDto;
  onClose: () => void;
}) {
  const [hasNoKnownProblems, setHasNoKnownProblems] = useState(patient.hasNoKnownProblems);
  const [hasNoKnownMedications, setHasNoKnownMedications] = useState(patient.hasNoKnownMedications);
  const [hasNoKnownAllergies, setHasNoKnownAllergies] = useState(patient.hasNoKnownAllergies);
  const [receivesEmailReminders, setReceivesEmailReminders] = useState(patient.receivesEmailReminders);
  const [lastVisitDate, setLastVisitDate] = useState(toDateInputValue(patient.lastVisitDate));
  const [nextVisitDate, setNextVisitDate] = useState(toDateInputValue(patient.nextVisitDate));

  useEffect(() => {
    if (open) {
      setHasNoKnownProblems(patient.hasNoKnownProblems);
      setHasNoKnownMedications(patient.hasNoKnownMedications);
      setHasNoKnownAllergies(patient.hasNoKnownAllergies);
      setReceivesEmailReminders(patient.receivesEmailReminders);
      setLastVisitDate(toDateInputValue(patient.lastVisitDate));
      setNextVisitDate(toDateInputValue(patient.nextVisitDate));
    }
  }, [open, patient]);

  const mutation = useUpdateMutation(patient, onClose, "Flags &amp; visits updated");

  const onSubmit = (e: FormEvent<HTMLFormElement>) => {
    e.preventDefault();
    mutation.mutate(
      mergePatientUpdate(patient, {
        hasNoKnownProblems,
        hasNoKnownMedications,
        hasNoKnownAllergies,
        receivesEmailReminders,
        lastVisitDate: fromDateInputValue(lastVisitDate),
        nextVisitDate: fromDateInputValue(nextVisitDate),
      }),
    );
  };

  return (
    <Dialog open={open} onOpenChange={(o) => (!o ? onClose() : undefined)}>
      <DialogContent>
        <form onSubmit={onSubmit}>
          <DialogHeader>
            <DialogTitle>Edit flags &amp; visits</DialogTitle>
            <DialogDescription>Quick-reference flags and scheduling dates for {fullName(patient)}.</DialogDescription>
          </DialogHeader>

          <DialogBody className="space-y-3">
            <FlagRow
              label="No known problems"
              checked={hasNoKnownProblems}
              onCheckedChange={setHasNoKnownProblems}
            />
            <FlagRow
              label="No known medications"
              checked={hasNoKnownMedications}
              onCheckedChange={setHasNoKnownMedications}
            />
            <FlagRow
              label="No known allergies"
              checked={hasNoKnownAllergies}
              onCheckedChange={setHasNoKnownAllergies}
            />
            <FlagRow
              label="Receives email reminders"
              checked={receivesEmailReminders}
              onCheckedChange={setReceivesEmailReminders}
            />
            <div className="grid gap-3 pt-2 sm:grid-cols-2">
              <Field id="flags-last-visit" label="Last visit date">
                <Input id="flags-last-visit" type="date" value={lastVisitDate} onChange={(e) => setLastVisitDate(e.target.value)} />
              </Field>
              <Field id="flags-next-visit" label="Next visit date">
                <Input id="flags-next-visit" type="date" value={nextVisitDate} onChange={(e) => setNextVisitDate(e.target.value)} />
              </Field>
            </div>
          </DialogBody>

          <DialogFooter>
            <DialogClose asChild>
              <Button type="button" variant="outline" disabled={mutation.isPending}>
                Cancel
              </Button>
            </DialogClose>
            <Button type="submit" disabled={mutation.isPending}>
              {mutation.isPending ? "Saving…" : "Save changes"}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}

function FlagRow({
  label,
  checked,
  onCheckedChange,
}: {
  label: string;
  checked: boolean;
  onCheckedChange: (value: boolean) => void;
}) {
  return (
    <div className="flex items-center justify-between rounded-lg border border-[var(--color-border)] bg-[var(--color-muted)] px-4 py-3">
      <span className="text-[12px] font-medium text-[var(--color-foreground)]">{label}</span>
      <Switch checked={checked} onCheckedChange={onCheckedChange} aria-label={label} />
    </div>
  );
}

function ToggleStatusDialog({
  open,
  patient,
  onClose,
}: {
  open: boolean;
  patient: PatientDetailDto;
  onClose: () => void;
}) {
  const mutation = useUpdateMutation(
    patient,
    onClose,
    patient.isActive ? "Patient deactivated" : "Patient reactivated",
  );
  const display = fullName(patient);

  return (
    <Dialog open={open} onOpenChange={(o) => (!o ? onClose() : undefined)}>
      <DialogContent>
        <DialogHeader>
          <DialogTitle>{patient.isActive ? "Deactivate patient?" : "Reactivate patient?"}</DialogTitle>
          <DialogDescription>
            {patient.isActive
              ? `${display} will be marked inactive. Their record is kept, not deleted.`
              : `${display} will be marked active again.`}
          </DialogDescription>
        </DialogHeader>
        <DialogFooter>
          <DialogClose asChild>
            <Button type="button" variant="outline" disabled={mutation.isPending}>
              Cancel
            </Button>
          </DialogClose>
          <Button
            onClick={() => mutation.mutate(mergePatientUpdate(patient, { isActive: !patient.isActive }))}
            disabled={mutation.isPending}
          >
            {mutation.isPending ? "Working…" : patient.isActive ? "Deactivate" : "Reactivate"}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}

function DeleteDialog({
  open,
  patient,
  onClose,
  onDeleted,
}: {
  open: boolean;
  patient: PatientDetailDto;
  onClose: () => void;
  onDeleted: () => void;
}) {
  const queryClient = useQueryClient();
  const display = fullName(patient);
  const mutation = useMutation({
    mutationFn: () => deletePatient(patient.id),
    onSuccess: () => {
      toast.success("Patient deleted");
      void queryClient.invalidateQueries({ queryKey: ["patients"] });
      onClose();
      onDeleted();
    },
    onError: (err: unknown) => toast.error("Delete failed", { description: describe(err) }),
  });

  return (
    <Dialog open={open} onOpenChange={(o) => (!o ? onClose() : undefined)}>
      <DialogContent>
        <DialogHeader>
          <DialogTitle>Delete this patient</DialogTitle>
          <DialogDescription>
            This removes <span className="font-medium text-[var(--color-foreground)]">{display}</span>'s
            record from active lists. This cannot be undone from here.
          </DialogDescription>
        </DialogHeader>
        <DialogFooter>
          <DialogClose asChild>
            <Button type="button" variant="outline" disabled={mutation.isPending}>
              Cancel
            </Button>
          </DialogClose>
          <Button variant="destructive" onClick={() => mutation.mutate()} disabled={mutation.isPending}>
            {mutation.isPending ? "Deleting…" : "Delete patient"}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
