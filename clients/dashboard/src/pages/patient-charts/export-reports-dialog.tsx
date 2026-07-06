import { useMemo, useState } from "react";
import { keepPreviousData, useMutation, useQuery } from "@tanstack/react-query";
import { Copy, FileDown } from "lucide-react";
import { toast } from "sonner";
import { listReportTypes } from "@/api/administration";
import {
  exportReportsPdf,
  searchPatientReports,
  type ExportedReportPdf,
} from "@/api/reports";
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
import { EntityStatusBadge } from "@/components/list";
import { describe, formatDate } from "@/lib/list-helpers";

type Props = {
  incidentId: string;
  open: boolean;
  onClose(): void;
};

/**
 * Export Reports dialog (BackChart `ExportReport.razor` + `SecureFileDownloadDialog` parity):
 * pick reports of the active incident, optionally merge them into one PDF, download, and
 * show each file's SHA-256 so the download can be integrity-checked.
 */
export function ExportReportsDialog({ incidentId, open, onClose }: Props) {
  const [selectedIds, setSelectedIds] = useState<string[]>([]);
  const [mergePdf, setMergePdf] = useState(false);
  const [exportedFiles, setExportedFiles] = useState<ExportedReportPdf[]>([]);

  // Same keys as the chart page so the cached lists are shared.
  const reportsQuery = useQuery({
    queryKey: ["reports", incidentId],
    queryFn: () => searchPatientReports({ incidentId, pageSize: 100 }),
    enabled: open,
    placeholderData: keepPreviousData,
  });

  const reportTypesQuery = useQuery({
    queryKey: ["report-types"],
    queryFn: () => listReportTypes(true),
    staleTime: 10 * 60 * 1000,
    enabled: open,
  });

  // Legacy grid sorted by report date ascending.
  const reports = useMemo(
    () =>
      [...(reportsQuery.data?.items ?? [])].sort(
        (a, b) => new Date(a.reportDate).getTime() - new Date(b.reportDate).getTime(),
      ),
    [reportsQuery.data],
  );

  const reportTypeLabel = (id: number): string =>
    reportTypesQuery.data?.find((t) => t.id === id)?.name ?? "Report";

  const allSelected = reports.length > 0 && selectedIds.length === reports.length;

  const toggleAll = () =>
    setSelectedIds(allSelected ? [] : reports.map((r) => r.id));

  const toggleOne = (id: string) =>
    setSelectedIds((prev) =>
      prev.includes(id) ? prev.filter((x) => x !== id) : [...prev, id],
    );

  const exportMutation = useMutation({
    mutationFn: async ({ ids, merge }: { ids: string[]; merge: boolean }) => {
      if (merge) {
        return [await exportReportsPdf(ids)];
      }
      // Separate files: one call per report, sequential so downloads fire reliably.
      const files: ExportedReportPdf[] = [];
      for (const id of ids) {
        files.push(await exportReportsPdf([id]));
      }
      return files;
    },
    onSuccess: (files) => {
      setExportedFiles(files);
      toast.success(files.length === 1 ? "Report exported." : `${files.length} reports exported.`);
    },
    onError: (err) => toast.error("Failed to export reports.", { description: describe(err) }),
  });

  const copySha = async (sha: string) => {
    try {
      await navigator.clipboard.writeText(sha);
      toast.info("SHA256SUM copied to clipboard.");
    } catch {
      toast.warning("Could not copy to clipboard.");
    }
  };

  const reset = () => {
    setSelectedIds([]);
    setMergePdf(false);
    setExportedFiles([]);
  };

  return (
    <Dialog
      open={open}
      onOpenChange={(o) => {
        if (!o) {
          reset();
          onClose();
        }
      }}
    >
      <DialogContent className="!max-w-3xl">
        <DialogHeader>
          <DialogTitle>Export Reports</DialogTitle>
        </DialogHeader>

        <DialogBody className="space-y-4">
          <p className="rounded-lg border border-[var(--color-border)] bg-[var(--color-accent)] px-3 py-2 text-[12px] text-[var(--color-muted-foreground)]">
            Select the reports you would like to export, then click Export PDF. Each file's
            SHA-256 checksum is shown after the download so you can verify its integrity.
          </p>

          <div className="flex flex-wrap items-center justify-between gap-3">
            <label className="flex items-center gap-2 text-[13px] cursor-pointer">
              <input
                type="checkbox"
                checked={allSelected}
                disabled={reports.length === 0}
                onChange={toggleAll}
                className="rounded border-[var(--color-border)]"
              />
              <span>Select all</span>
            </label>
            <div className="flex items-center gap-3">
              <label className="flex items-center gap-2 text-[13px] cursor-pointer">
                <input
                  type="checkbox"
                  checked={mergePdf}
                  onChange={(e) => setMergePdf(e.target.checked)}
                  className="rounded border-[var(--color-border)]"
                />
                <span>Merge into one PDF</span>
              </label>
              <Button
                size="sm"
                className="h-8 gap-1.5 rounded-lg px-3 text-[13px] font-semibold"
                disabled={selectedIds.length === 0 || exportMutation.isPending}
                onClick={() => exportMutation.mutate({ ids: selectedIds, merge: mergePdf })}
              >
                <FileDown className="size-4" />
                {exportMutation.isPending
                  ? "Exporting…"
                  : `Export PDF${selectedIds.length > 0 ? ` (${selectedIds.length})` : ""}`}
              </Button>
            </div>
          </div>

          {reportsQuery.isLoading ? (
            <div className="skeleton h-20 rounded-lg" />
          ) : reports.length === 0 ? (
            <p className="py-6 text-center text-[13px] text-[var(--color-muted-foreground)]">
              No reports to export for this incident.
            </p>
          ) : (
            <ul className="divide-y divide-[var(--color-border)] rounded-lg border border-[var(--color-border)]">
              {reports.map((r) => (
                <li key={r.id}>
                  <label className="flex cursor-pointer items-center justify-between gap-3 px-3 py-2.5">
                    <div className="flex min-w-0 items-center gap-2.5">
                      <input
                        type="checkbox"
                        checked={selectedIds.includes(r.id)}
                        onChange={() => toggleOne(r.id)}
                        className="rounded border-[var(--color-border)]"
                      />
                      <div className="min-w-0">
                        <p className="truncate text-[13px] font-medium">
                          {reportTypeLabel(r.reportTypeId)}
                        </p>
                        <p className="text-[12px] text-[var(--color-muted-foreground)]">
                          {formatDate(r.reportDate)}
                          {r.signedByName ? ` · Signed by ${r.signedByName}` : ""}
                        </p>
                      </div>
                    </div>
                    <EntityStatusBadge tone={r.isSigned ? "info" : "default"}>
                      {r.workflowStatus}
                    </EntityStatusBadge>
                  </label>
                </li>
              ))}
            </ul>
          )}

          {exportedFiles.length > 0 && (
            <div className="space-y-2 rounded-lg border border-[var(--color-border)] p-3">
              <p className="text-[12px] font-semibold uppercase tracking-wide text-[var(--color-primary)]">
                Downloaded Files
              </p>
              <p className="text-[12px] text-[var(--color-muted-foreground)]">
                The SHA256SUM verifies the file you downloaded is exactly the file generated by the
                server and was not altered in transit.
              </p>
              <ul className="space-y-2">
                {exportedFiles.map((f) => (
                  <li key={f.fileName} className="text-[12px]">
                    <p className="font-medium">{f.fileName}</p>
                    <div className="flex items-center gap-1.5">
                      <code className="min-w-0 break-all text-[11px] text-[var(--color-muted-foreground)]">
                        {f.sha256}
                      </code>
                      <button
                        type="button"
                        title="Copy SHA256SUM to clipboard"
                        aria-label="Copy SHA256SUM to clipboard"
                        onClick={() => void copySha(f.sha256)}
                        className="inline-flex size-6 shrink-0 items-center justify-center rounded-md border border-[var(--color-border)] transition-colors hover:bg-[var(--color-accent)]"
                      >
                        <Copy className="size-3.5" />
                      </button>
                    </div>
                  </li>
                ))}
              </ul>
            </div>
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
  );
}
