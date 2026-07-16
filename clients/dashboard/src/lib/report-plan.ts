import { useEffect, useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { toast } from "sonner";
import { getReport, updateReport } from "@/api/reports";
import { listReportFields } from "@/api/administration";
import { describe } from "@/lib/list-helpers";

// The report's Plan field, matched by name (legacy ldfID 24 "Plan"). Clinic fields are
// per-report-type copies, so there's no stable id to key on — the name is the join key.
const PLAN_FIELDS = new Set(["plan", "plan comments", "treatment plan"]);
export const fieldIsPlan = (name: string) => PLAN_FIELDS.has(name.trim().toLowerCase());

/**
 * Editable Plan field bound to a report, with BackChart's `HasPlanDisable()` condition:
 * the plan is editable only while the report is unsigned. Saving rewrites the FULL report
 * (all other fields + vitals preserved) with just the Plan field replaced, so a plan edit
 * never drops the rest of the report.
 *
 * Shares the ["report", id] / ["report-fields", typeId] query keys with the chart, so the
 * report is usually served from cache rather than refetched.
 */
export function useReportPlanField(reportId: string | null | undefined, enabled = true) {
  const qc = useQueryClient();
  const on = enabled && !!reportId;

  const reportQuery = useQuery({
    queryKey: ["report", reportId],
    queryFn: () => getReport(reportId!),
    enabled: on,
  });
  const report = reportQuery.data;

  const fieldsQuery = useQuery({
    queryKey: ["report-fields", report?.reportTypeId],
    queryFn: () => listReportFields(report!.reportTypeId),
    enabled: on && report != null,
  });
  const planFieldId = fieldsQuery.data?.find((f) => fieldIsPlan(f.name))?.id ?? null;
  const hasPlanField = planFieldId != null;
  const isSigned = report?.isSigned ?? false;

  const [text, setTextState] = useState("");
  const [dirty, setDirty] = useState(false);
  const [hydratedFor, setHydratedFor] = useState<string | null>(null);

  useEffect(() => {
    if (!report || planFieldId == null) return;
    if (hydratedFor === report.id) return;
    setTextState(report.fieldValues.find((f) => f.reportFieldId === planFieldId)?.text ?? "");
    setDirty(false);
    setHydratedFor(report.id);
  }, [report, planFieldId, hydratedFor]);

  const saveMutation = useMutation({
    mutationFn: async () => {
      if (!report || planFieldId == null) return;
      const others = report.fieldValues.filter((f) => f.reportFieldId !== planFieldId);
      await updateReport({
        reportId: report.id,
        reportDate: report.reportDate,
        providerId: report.providerId ?? null,
        clinicId: report.clinicId ?? null,
        isNoShow: report.isNoShow,
        vitals: report.vitals,
        fieldValues: [...others, { reportFieldId: planFieldId, text }],
      });
    },
    onSuccess: () => {
      toast.success("Plan saved.");
      setDirty(false);
      void qc.invalidateQueries({ queryKey: ["report", reportId] });
      void qc.invalidateQueries({ queryKey: ["reports"] });
    },
    onError: (err) => toast.error("Failed to save plan.", { description: describe(err) }),
  });

  return {
    text,
    setText: (t: string) => {
      setTextState(t);
      setDirty(true);
    },
    /** Append macro/boilerplate text on its own line (mirrors the SuperBill macro flow). */
    appendText: (t: string) => {
      setTextState((prev) => (prev && !prev.endsWith("\n") ? `${prev}\n${t}` : `${prev}${t}`));
      setDirty(true);
    },
    dirty,
    isSigned,
    hasPlanField,
    /** Editable only when the report has a Plan field and isn't signed (BackChart parity). */
    canEdit: hasPlanField && !isSigned,
    save: () => saveMutation.mutate(),
    isSaving: saveMutation.isPending,
    isLoading: reportQuery.isLoading || (report != null && fieldsQuery.isLoading),
  };
}
