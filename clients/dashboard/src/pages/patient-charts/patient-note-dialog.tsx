import { useEffect, useState, type FormEvent } from "react";
import { useMutation, useQueryClient } from "@tanstack/react-query";
import { toast } from "sonner";
import { createNote, updateNote, type PatientNote } from "@/api/patient-notes";
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
import { Field } from "@/components/list";
import { describe } from "@/lib/list-helpers";

type Props = {
  patientId: string;
  open: boolean;
  onClose(): void;
  note?: PatientNote | null;
};

export function PatientNoteDialog({ patientId, open, onClose, note }: Props) {
  const queryClient = useQueryClient();
  const isEdit = !!note;

  const [name, setName] = useState("");
  const [description, setDescription] = useState("");
  const [isMedicalAlert, setIsMedicalAlert] = useState(false);

  useEffect(() => {
    if (!open) return;
    setName(note?.name ?? "");
    setDescription(note?.description ?? "");
    setIsMedicalAlert(note?.isMedicalAlert ?? false);
  }, [open, note]);

  const invalidate = () => {
    void queryClient.invalidateQueries({ queryKey: ["patient-notes", patientId] });
  };

  const createMutation = useMutation({
    mutationFn: createNote,
    onSuccess: () => {
      toast.success("Patient note saved.");
      invalidate();
      onClose();
    },
    onError: (err) => toast.error("Failed to save note.", { description: describe(err) }),
  });

  const updateMutation = useMutation({
    mutationFn: updateNote,
    onSuccess: () => {
      toast.success("Patient note saved.");
      invalidate();
      onClose();
    },
    onError: (err) => toast.error("Failed to save note.", { description: describe(err) }),
  });

  const isPending = createMutation.isPending || updateMutation.isPending;

  const onSubmit = (e: FormEvent<HTMLFormElement>) => {
    e.preventDefault();
    if (!name.trim()) return;
    if (isEdit && note) {
      updateMutation.mutate({
        noteId: note.id,
        name: name.trim(),
        description: description.trim() || null,
        isMedicalAlert,
      });
    } else {
      createMutation.mutate({
        patientId,
        name: name.trim(),
        description: description.trim() || null,
        isMedicalAlert,
      });
    }
  };

  return (
    <Dialog open={open} onOpenChange={(o) => (!o ? onClose() : undefined)}>
      <DialogContent className="!max-w-lg">
        <form onSubmit={onSubmit}>
          <DialogHeader>
            <DialogTitle>{isEdit ? "Edit Patient Note" : "Add Patient Note"}</DialogTitle>
          </DialogHeader>

          <DialogBody className="space-y-4">
            <Field id="note-name" label="Name">
              <Input
                id="note-name"
                type="text"
                value={name}
                maxLength={256}
                onChange={(e) => setName(e.target.value)}
                required
                autoFocus
              />
            </Field>

            <Field id="note-description" label="Description">
              <Textarea
                id="note-description"
                value={description}
                onChange={(e) => setDescription(e.target.value)}
                rows={4}
                maxLength={8000}
              />
            </Field>

            <label className="flex w-fit items-center gap-2 text-[13px] cursor-pointer">
              <input
                type="checkbox"
                checked={isMedicalAlert}
                onChange={(e) => setIsMedicalAlert(e.target.checked)}
                className="rounded border-[var(--color-border)]"
              />
              <span>Flag as medical alert</span>
            </label>
          </DialogBody>

          <DialogFooter>
            <DialogClose asChild>
              <Button type="button" variant="outline" disabled={isPending}>
                Cancel
              </Button>
            </DialogClose>
            <Button type="submit" disabled={isPending || !name.trim()}>
              {isPending ? "Saving…" : "Save Note"}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}
