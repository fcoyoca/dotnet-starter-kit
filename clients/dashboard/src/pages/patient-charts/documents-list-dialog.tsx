import { useMemo, useState } from "react";
import { keepPreviousData, useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { Download, Pencil, Trash2, Upload } from "lucide-react";
import { toast } from "sonner";
import { listPatientDocumentTypes } from "@/api/administration";
import {
  deletePatientDocument,
  downloadPatientDocument,
  searchPatientDocuments,
  type PatientDocument,
} from "@/api/patient-documents";
import { DOCUMENT_PERMISSIONS } from "@/lib/patient-permissions";
import { useAuth } from "@/auth/use-auth";
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
import { Combobox } from "@/components/list";
import { describe, formatDate } from "@/lib/list-helpers";
import { DocumentDialog } from "@/pages/patient-charts/document-dialog";

type Props = {
  patientId: string;
  open: boolean;
  onClose(): void;
};

/**
 * Documents dialog (BackChart `PatientDocumentIndex.razor` parity): filter by type, upload,
 * open a row for details/notes, download, delete.
 */
export function DocumentsListDialog({ patientId, open, onClose }: Props) {
  const { user } = useAuth();
  const queryClient = useQueryClient();

  const canCreate = user?.permissions?.includes(DOCUMENT_PERMISSIONS.create) ?? false;
  const canUpdate = user?.permissions?.includes(DOCUMENT_PERMISSIONS.update) ?? false;
  const canDownload = user?.permissions?.includes(DOCUMENT_PERMISSIONS.download) ?? false;
  const canDelete = user?.permissions?.includes(DOCUMENT_PERMISSIONS.delete) ?? false;

  const [typeFilter, setTypeFilter] = useState<string | null>(null);
  const [editorOpen, setEditorOpen] = useState(false);
  const [editDocument, setEditDocument] = useState<PatientDocument | null>(null);

  const documentsQuery = useQuery({
    queryKey: ["patient-documents", patientId, typeFilter],
    queryFn: () =>
      searchPatientDocuments({ patientId, documentTypeId: typeFilter, pageSize: 200 }),
    enabled: open,
    placeholderData: keepPreviousData,
  });

  const typesQuery = useQuery({
    queryKey: ["administration", "patient-document-types", "options"],
    queryFn: () => listPatientDocumentTypes({ isActive: true, pageSize: 100 }),
    staleTime: 10 * 60 * 1000,
    enabled: open,
  });

  const documents = useMemo(() => documentsQuery.data?.items ?? [], [documentsQuery.data]);

  const typeOptions = useMemo(
    () => (typesQuery.data?.items ?? []).map((t) => ({ value: t.id, label: t.name })),
    [typesQuery.data],
  );

  const typeLabel = (id: string | null | undefined): string =>
    (id && typeOptions.find((o) => o.value === id)?.label) || "—";

  const deleteMutation = useMutation({
    mutationFn: (id: string) => deletePatientDocument(id),
    onSuccess: () => {
      toast.success("Patient document deleted.");
      void queryClient.invalidateQueries({ queryKey: ["patient-documents", patientId] });
    },
    onError: (err) => toast.error("Failed to delete document.", { description: describe(err) }),
  });

  const downloadMutation = useMutation({
    mutationFn: ({ id, fileName }: { id: string; fileName: string }) =>
      downloadPatientDocument(id, fileName),
    onError: (err) => toast.error("Failed to download document.", { description: describe(err) }),
  });

  const openUpload = () => {
    setEditDocument(null);
    setEditorOpen(true);
  };

  const openDetails = (doc: PatientDocument) => {
    setEditDocument(doc);
    setEditorOpen(true);
  };

  return (
    <>
      <Dialog open={open} onOpenChange={(o) => (!o ? onClose() : undefined)}>
        <DialogContent className="!max-w-3xl">
          <DialogHeader>
            <DialogTitle>Documents</DialogTitle>
          </DialogHeader>

          <DialogBody className="space-y-4">
            <div className="flex flex-wrap items-center justify-between gap-3">
              <div className="w-56">
                <Combobox
                  id="document-type-filter"
                  label="Filter by type"
                  value={typeFilter}
                  onChange={setTypeFilter}
                  options={typeOptions}
                  placeholder="All types"
                />
              </div>
              {canCreate && (
                <Button
                  size="sm"
                  className="h-8 gap-1.5 rounded-lg px-3 text-[13px] font-semibold"
                  onClick={openUpload}
                >
                  <Upload className="size-4" />
                  Upload Document
                </Button>
              )}
            </div>

            {documentsQuery.isLoading ? (
              <div className="skeleton h-20 rounded-lg" />
            ) : documents.length === 0 ? (
              <p className="py-6 text-center text-[13px] text-[var(--color-muted-foreground)]">
                {typeFilter
                  ? "No documents match the selected type."
                  : "No documents uploaded for this patient yet."}
              </p>
            ) : (
              <ul className="divide-y divide-[var(--color-border)] rounded-lg border border-[var(--color-border)]">
                {documents.map((d) => (
                  <li key={d.id} className="flex items-center justify-between gap-3 px-3 py-2.5">
                    <div className="min-w-0">
                      <p className="truncate text-[13px] font-medium">{d.fileName}</p>
                      <p className="truncate text-[12px] text-[var(--color-muted-foreground)]">
                        {[typeLabel(d.documentTypeId), formatDate(d.uploadedAtUtc), d.notes]
                          .filter(Boolean)
                          .join(" · ")}
                      </p>
                    </div>
                    <div className="flex items-center gap-1">
                      {canDownload && (
                        <button
                          type="button"
                          title="Download document"
                          aria-label="Download document"
                          disabled={downloadMutation.isPending}
                          onClick={() => downloadMutation.mutate({ id: d.id, fileName: d.fileName })}
                          className="inline-flex size-7 items-center justify-center rounded-md border border-[var(--color-border)] transition-colors hover:bg-[var(--color-accent)] disabled:pointer-events-none disabled:opacity-40"
                        >
                          <Download className="size-4" />
                        </button>
                      )}
                      {canUpdate && (
                        <button
                          type="button"
                          title="View / edit document details"
                          aria-label="View / edit document details"
                          onClick={() => openDetails(d)}
                          className="inline-flex size-7 items-center justify-center rounded-md border border-[var(--color-border)] transition-colors hover:bg-[var(--color-accent)]"
                        >
                          <Pencil className="size-4" />
                        </button>
                      )}
                      {canDelete && (
                        <button
                          type="button"
                          title="Delete document"
                          aria-label="Delete document"
                          disabled={deleteMutation.isPending}
                          onClick={() => deleteMutation.mutate(d.id)}
                          className="inline-flex size-7 items-center justify-center rounded-md border border-[var(--color-border)] text-[var(--color-destructive)] transition-colors hover:bg-[var(--color-accent)] disabled:pointer-events-none disabled:opacity-40"
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

      <DocumentDialog
        patientId={patientId}
        open={editorOpen}
        onClose={() => {
          setEditorOpen(false);
          setEditDocument(null);
        }}
        document={editDocument}
        documentTypeOptions={typeOptions}
      />
    </>
  );
}
