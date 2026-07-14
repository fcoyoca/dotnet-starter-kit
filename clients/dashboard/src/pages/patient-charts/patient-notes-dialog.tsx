import { useMemo, useState } from "react";
import { keepPreviousData, useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { AlertTriangle, Pencil, Plus, Trash2 } from "lucide-react";
import { toast } from "sonner";
import { deleteNote, searchPatientNotes, type PatientNote } from "@/api/patient-notes";
import { NOTE_PERMISSIONS } from "@/lib/patient-permissions";
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
import { describe, formatDate } from "@/lib/list-helpers";
import { PatientNoteDialog } from "@/pages/patient-charts/patient-note-dialog";

type Props = {
  patientId: string;
  open: boolean;
  onClose(): void;
};

export function PatientNotesDialog({ patientId, open, onClose }: Props) {
  const { user } = useAuth();
  const queryClient = useQueryClient();

  const canCreate = user?.permissions?.includes(NOTE_PERMISSIONS.create) ?? false;
  const canUpdate = user?.permissions?.includes(NOTE_PERMISSIONS.update) ?? false;
  const canDelete = user?.permissions?.includes(NOTE_PERMISSIONS.delete) ?? false;

  const [editorOpen, setEditorOpen] = useState(false);
  const [editNote, setEditNote] = useState<PatientNote | null>(null);
  const [pendingDelete, setPendingDelete] = useState<PatientNote | null>(null);

  const notesQuery = useQuery({
    queryKey: ["patient-notes", patientId],
    queryFn: () => searchPatientNotes({ patientId, pageSize: 200 }),
    enabled: open,
    placeholderData: keepPreviousData,
  });

  const notes = useMemo(() => notesQuery.data?.items ?? [], [notesQuery.data]);

  const deleteMutation = useMutation({
    mutationFn: (id: string) => deleteNote(id),
    onSuccess: () => {
      toast.success("Note deleted.");
      void queryClient.invalidateQueries({ queryKey: ["patient-notes", patientId] });
      setPendingDelete(null);
    },
    onError: (err) => toast.error("Failed to delete note.", { description: describe(err) }),
  });

  return (
    <>
      <Dialog open={open} onOpenChange={(o) => (!o ? onClose() : undefined)}>
        <DialogContent className="!max-w-3xl">
          <DialogHeader>
            <DialogTitle>Patient Notes</DialogTitle>
          </DialogHeader>

          <DialogBody className="space-y-4">
            <div className="flex items-center justify-end">
              {canCreate && (
                <Button
                  size="sm"
                  className="h-8 gap-1.5 rounded-lg px-3 text-[13px] font-semibold"
                  onClick={() => {
                    setEditNote(null);
                    setEditorOpen(true);
                  }}
                >
                  <Plus className="size-4" />
                  Add Note
                </Button>
              )}
            </div>

            {notesQuery.isLoading ? (
              <div className="skeleton h-20 rounded-lg" />
            ) : notes.length === 0 ? (
              <p className="py-6 text-center text-[13px] text-[var(--color-muted-foreground)]">
                No notes recorded for this patient.
              </p>
            ) : (
              <ul className="divide-y divide-[var(--color-border)] rounded-lg border border-[var(--color-border)]">
                {notes.map((n) => (
                  <li key={n.id} className="flex items-center justify-between gap-3 px-3 py-2.5">
                    <div className="min-w-0">
                      <p className="flex items-center gap-1.5 truncate text-[13px] font-medium">
                        {n.name}
                        {n.isMedicalAlert && (
                          <AlertTriangle className="size-3.5 text-[var(--color-destructive)]" />
                        )}
                      </p>
                      {n.description && (
                        <p className="truncate text-[12px] text-[var(--color-muted-foreground)]">
                          {n.description}
                        </p>
                      )}
                    </div>
                    <div className="flex items-center gap-2">
                      <span className="hidden text-[12px] text-[var(--color-muted-foreground)] sm:inline">
                        {formatDate(n.createdAtUtc)}
                      </span>
                      <div className="flex items-center gap-1">
                        {canUpdate && (
                          <button
                            type="button"
                            title="Edit note"
                            aria-label="Edit note"
                            onClick={() => {
                              setEditNote(n);
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
                            title="Delete note"
                            aria-label="Delete note"
                            disabled={deleteMutation.isPending}
                            onClick={() => setPendingDelete(n)}
                            className="inline-flex size-7 items-center justify-center rounded-md border border-[var(--color-border)] text-[var(--color-destructive)] transition-colors hover:bg-[var(--color-accent)] disabled:pointer-events-none disabled:opacity-40"
                          >
                            <Trash2 className="size-4" />
                          </button>
                        )}
                      </div>
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

      <PatientNoteDialog
        patientId={patientId}
        open={editorOpen}
        onClose={() => {
          setEditorOpen(false);
          setEditNote(null);
        }}
        note={editNote}
      />

      <ConfirmDialog
        open={pendingDelete !== null}
        eyebrow="Delete note"
        title="Delete this note?"
        description={
          <>
            <span className="font-medium text-[var(--color-foreground)]">{pendingDelete?.name}</span>{" "}
            will be removed from this patient&apos;s chart. This can&apos;t be undone.
          </>
        }
        confirmLabel="Delete note"
        pending={deleteMutation.isPending}
        onCancel={() => setPendingDelete(null)}
        onConfirm={() => pendingDelete && deleteMutation.mutate(pendingDelete.id)}
      />
    </>
  );
}
