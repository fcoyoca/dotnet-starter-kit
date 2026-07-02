import { useEffect, useState, type FormEvent } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { UserPlus } from "lucide-react";
import { toast } from "sonner";
import {
  createPatient,
  getNextPatientCodePreview,
  type CreatePatientInput,
} from "@/api/patients";
import { emptyPatientFields } from "@/pages/patients/patient-mappers";
import { GENDER_OPTIONS, MARITAL_STATUS_OPTIONS } from "@/lib/patient-lookups";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
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
import { Combobox, Field } from "@/components/list";
import { describe } from "@/lib/list-helpers";

export function CreatePatientDialog({
  open,
  onClose,
  onCreated,
}: {
  open: boolean;
  onClose: () => void;
  onCreated?: (patientId: string) => void;
}) {
  const queryClient = useQueryClient();
  const [firstName, setFirstName] = useState("");
  const [lastName, setLastName] = useState("");
  const [middleInitial, setMiddleInitial] = useState("");
  const [dateOfBirth, setDateOfBirth] = useState("");
  const [gender, setGender] = useState<string | null>(null);
  const [maritalStatus, setMaritalStatus] = useState<string | null>(null);

  useEffect(() => {
    if (!open) {
      setFirstName("");
      setLastName("");
      setMiddleInitial("");
      setDateOfBirth("");
      setGender(null);
      setMaritalStatus(null);
    }
  }, [open]);

  const previewQuery = useQuery({
    queryKey: ["patients", "next-code-preview"],
    queryFn: getNextPatientCodePreview,
    enabled: open,
    staleTime: 0,
  });

  const mutation = useMutation({
    mutationFn: (input: CreatePatientInput) => createPatient(input),
    onSuccess: (patientId) => {
      toast.success("Patient registered");
      void queryClient.invalidateQueries({ queryKey: ["patients", "list"] });
      onClose();
      onCreated?.(patientId);
    },
    onError: (err) =>
      toast.error("Registration failed", { description: describe(err) }),
  });

  const onSubmit = (e: FormEvent<HTMLFormElement>) => {
    e.preventDefault();
    if (!gender) return;
    mutation.mutate({
      ...emptyPatientFields(),
      patientCode: undefined,
      firstName: firstName.trim(),
      lastName: lastName.trim(),
      middleInitial: middleInitial.trim() || null,
      dateOfBirth,
      gender,
      maritalStatus,
    });
  };

  return (
    <Dialog open={open} onOpenChange={(o) => (!o ? onClose() : undefined)}>
      <DialogContent className="!max-w-lg">
        <form onSubmit={onSubmit}>
          <DialogHeader>
            <DialogTitle>Register a patient</DialogTitle>
            <DialogDescription>
              Capture the essentials now — every other section can be filled in from the
              patient's detail page.
            </DialogDescription>
          </DialogHeader>

          <DialogBody className="space-y-4">
            <Field id="pat-code" label="Patient code">
              <Input
                id="pat-code"
                value={previewQuery.data ?? (previewQuery.isLoading ? "Generating…" : "")}
                disabled
                readOnly
              />
            </Field>

            <div className="grid gap-3 sm:grid-cols-[1fr_1fr_72px]">
              <Field id="pat-first" label="First name" required>
                <Input
                  id="pat-first"
                  value={firstName}
                  onChange={(e) => setFirstName(e.target.value)}
                  placeholder="Ada"
                  autoFocus
                  required
                />
              </Field>
              <Field id="pat-last" label="Last name" required>
                <Input
                  id="pat-last"
                  value={lastName}
                  onChange={(e) => setLastName(e.target.value)}
                  placeholder="Lovelace"
                  required
                />
              </Field>
              <Field id="pat-mi" label="M.I.">
                <Input
                  id="pat-mi"
                  value={middleInitial}
                  onChange={(e) => setMiddleInitial(e.target.value)}
                  maxLength={5}
                />
              </Field>
            </div>

            <div className="grid gap-3 sm:grid-cols-2">
              <Field id="pat-dob" label="Date of birth" required>
                <Input
                  id="pat-dob"
                  type="date"
                  value={dateOfBirth}
                  onChange={(e) => setDateOfBirth(e.target.value)}
                  max={new Date().toISOString().slice(0, 10)}
                  required
                />
              </Field>
              <Field id="pat-gender" label="Gender" required>
                <Combobox
                  id="pat-gender"
                  label="Gender"
                  value={gender}
                  onChange={setGender}
                  options={GENDER_OPTIONS}
                  placeholder="Select…"
                  required
                />
              </Field>
            </div>

            <Field id="pat-marital" label="Marital status">
              <Combobox
                id="pat-marital"
                label="Marital status"
                value={maritalStatus}
                onChange={setMaritalStatus}
                options={MARITAL_STATUS_OPTIONS}
                placeholder="Select…"
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
            <Button
              type="submit"
              disabled={mutation.isPending || !gender}
              className="gap-1.5"
            >
              <UserPlus className="h-4 w-4" />
              {mutation.isPending ? "Registering…" : "Register patient"}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}
