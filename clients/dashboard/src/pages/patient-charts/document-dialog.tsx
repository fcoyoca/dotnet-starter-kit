import { useEffect, useRef, useState, type FormEvent } from "react";
import { useMutation, useQueryClient } from "@tanstack/react-query";
import { Download } from "lucide-react";
import { toast } from "sonner";
import {
  ALLOWED_DOCUMENT_EXTENSIONS,
  MAX_DOCUMENT_SIZE_BYTES,
  downloadPatientDocument,
  fileToBase64,
  updatePatientDocument,
  uploadPatientDocument,
  type PatientDocument,
} from "@/api/patient-documents";
import { DOCUMENT_PERMISSIONS } from "@/lib/patient-permissions";
import { useAuth } from "@/auth/use-auth";
import { Button } from "@/components/ui/button";
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
import { describe, formatDate } from "@/lib/list-helpers";

type Props = {
  patientId: string;
  open: boolean;
  onClose(): void;
  /** null → upload mode; set → view/edit an existing document's type + notes. */
  document?: PatientDocument | null;
  documentTypeOptions: { value: string; label: string }[];
};

function hasAllowedExtension(name: string): boolean {
  const dot = name.lastIndexOf(".");
  if (dot < 0) return false;
  const ext = name.slice(dot).toLowerCase();
  return (ALLOWED_DOCUMENT_EXTENSIONS as readonly string[]).includes(ext);
}

/**
 * Upload/detail dialog (BackChart `PatientDocumentDetail.razor` parity): upload mode picks one or
 * more files sharing a type + notes (one document row per file, like legacy multi-upload); edit
 * mode shows the immutable file with editable type/notes and a Download button.
 */
export function DocumentDialog({ patientId, open, onClose, document, documentTypeOptions }: Props) {
  const { user } = useAuth();
  const queryClient = useQueryClient();
  const isEdit = !!document;

  const canDownload = user?.permissions?.includes(DOCUMENT_PERMISSIONS.download) ?? false;

  const [documentTypeId, setDocumentTypeId] = useState<string | null>(null);
  const [notes, setNotes] = useState("");
  const [files, setFiles] = useState<File[]>([]);
  const fileInputRef = useRef<HTMLInputElement>(null);

  useEffect(() => {
    if (!open) return;
    setDocumentTypeId(document?.documentTypeId ?? null);
    setNotes(document?.notes ?? "");
    setFiles([]);
    if (fileInputRef.current) fileInputRef.current.value = "";
  }, [open, document]);

  const invalidate = () => {
    void queryClient.invalidateQueries({ queryKey: ["patient-documents", patientId] });
  };

  const uploadMutation = useMutation({
    mutationFn: async ({ picked, typeId, sharedNotes }: { picked: File[]; typeId: string | null; sharedNotes: string | null }) => {
      // One document per file, sequential (legacy multi-upload created one row per file).
      for (const file of picked) {
        const contentBase64 = await fileToBase64(file);
        await uploadPatientDocument({
          patientId,
          documentTypeId: typeId,
          fileName: file.name,
          contentBase64,
          contentType: file.type || null,
          notes: sharedNotes,
        });
      }
      return picked.length;
    },
    onSuccess: (count) => {
      toast.success(count === 1 ? "Document uploaded." : `${count} documents uploaded.`);
      invalidate();
      onClose();
    },
    onError: (err) => toast.error("Failed to upload document.", { description: describe(err) }),
  });

  const updateMutation = useMutation({
    mutationFn: updatePatientDocument,
    onSuccess: () => {
      toast.success("Document saved.");
      invalidate();
      onClose();
    },
    onError: (err) => toast.error("Failed to save document.", { description: describe(err) }),
  });

  const downloadMutation = useMutation({
    mutationFn: ({ id, fileName }: { id: string; fileName: string }) =>
      downloadPatientDocument(id, fileName),
    onError: (err) => toast.error("Failed to download document.", { description: describe(err) }),
  });

  const isPending = uploadMutation.isPending || updateMutation.isPending;

  const onPickFiles = (list: FileList | null) => {
    const picked = Array.from(list ?? []);
    const rejected = picked.filter(
      (f) => !hasAllowedExtension(f.name) || f.size > MAX_DOCUMENT_SIZE_BYTES,
    );
    if (rejected.length > 0) {
      toast.warning(
        `Skipped ${rejected.map((f) => f.name).join(", ")} — allowed types: ${ALLOWED_DOCUMENT_EXTENSIONS.join(", ")}, max 10 MB.`,
      );
    }
    setFiles(picked.filter((f) => hasAllowedExtension(f.name) && f.size <= MAX_DOCUMENT_SIZE_BYTES));
  };

  const onSubmit = (e: FormEvent<HTMLFormElement>) => {
    e.preventDefault();
    if (isEdit && document) {
      updateMutation.mutate({
        documentId: document.id,
        documentTypeId,
        notes: notes.trim() || null,
      });
    } else {
      if (files.length === 0) {
        toast.warning("No file selected.");
        return;
      }
      uploadMutation.mutate({ picked: files, typeId: documentTypeId, sharedNotes: notes.trim() || null });
    }
  };

  return (
    <Dialog open={open} onOpenChange={(o) => (!o ? onClose() : undefined)}>
      <DialogContent className="!max-w-lg">
        <form onSubmit={onSubmit}>
          <DialogHeader>
            <DialogTitle>{isEdit ? "Document Details" : "Upload Document"}</DialogTitle>
          </DialogHeader>

          <DialogBody className="space-y-4">
            <div className="w-full">
              <Combobox
                id="document-type"
                label="Type"
                value={documentTypeId}
                onChange={setDocumentTypeId}
                options={documentTypeOptions}
                placeholder="Select type"
              />
            </div>

            {isEdit && document ? (
              <div className="flex items-center justify-between gap-3 rounded-lg border border-[var(--color-border)] px-3 py-2.5">
                <div className="min-w-0">
                  <p className="truncate text-[13px] font-medium">{document.fileName}</p>
                  <p className="text-[12px] text-[var(--color-muted-foreground)]">
                    {(document.fileSizeBytes / 1024).toFixed(0)} KB
                  </p>
                </div>
                {canDownload && (
                  <Button
                    type="button"
                    size="sm"
                    variant="outline"
                    className="h-8 gap-1.5 rounded-lg px-3 text-[13px] font-semibold"
                    disabled={downloadMutation.isPending}
                    onClick={() =>
                      downloadMutation.mutate({ id: document.id, fileName: document.fileName })
                    }
                  >
                    <Download className="size-4" />
                    {downloadMutation.isPending ? "Downloading…" : "Download"}
                  </Button>
                )}
              </div>
            ) : (
              <Field id="document-files" label="File(s)">
                <input
                  id="document-files"
                  ref={fileInputRef}
                  type="file"
                  multiple
                  accept={ALLOWED_DOCUMENT_EXTENSIONS.join(",")}
                  onChange={(e) => onPickFiles(e.target.files)}
                  className="block w-full text-[13px] file:mr-3 file:rounded-md file:border file:border-[var(--color-border)] file:bg-transparent file:px-3 file:py-1.5 file:text-[13px] file:font-semibold"
                />
                {files.length > 0 && (
                  <p className="mt-1 text-[12px] text-[var(--color-muted-foreground)]">
                    {files.length} file(s) selected
                  </p>
                )}
              </Field>
            )}

            <Field id="document-notes" label="Notes">
              <Textarea
                id="document-notes"
                value={notes}
                onChange={(e) => setNotes(e.target.value)}
                rows={4}
                maxLength={8000}
              />
            </Field>

            {isEdit && document && (
              <div className="space-y-0.5 text-[12px] text-[var(--color-muted-foreground)]">
                <p>Uploaded Date: {formatDate(document.uploadedAtUtc)}</p>
                <p>Uploaded By: {document.uploadedByName || "—"}</p>
              </div>
            )}
          </DialogBody>

          <DialogFooter>
            <DialogClose asChild>
              <Button type="button" variant="outline" disabled={isPending}>
                Cancel
              </Button>
            </DialogClose>
            <Button type="submit" disabled={isPending || (!isEdit && files.length === 0)}>
              {isPending ? "Saving…" : isEdit ? "Save" : "Upload"}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}
