import { useMemo, useState } from "react";
import { useMutation, useQueries, useQuery, useQueryClient } from "@tanstack/react-query";
import { FileSignature, Save } from "lucide-react";
import { toast } from "sonner";
import {
  listReportFields,
  listReportTypes,
  updateReportField,
  type ReportFieldDto,
  type ReportTypeDto,
} from "@/api/administration";
import { useAuth } from "@/auth/use-auth";
import { Button } from "@/components/ui/button";
import { Textarea } from "@/components/ui/textarea";
import { EntityEmpty, EntityListLoading, EntityPageHeader } from "@/components/list";
import { describe } from "@/lib/list-helpers";
import { cn } from "@/lib/cn";

const VIEW_PERM = "Permissions.Administration.ReportTemplates.View";
const UPDATE_PERM = "Permissions.Administration.ReportTemplates.Update";

/**
 * Report Default Text — the boilerplate each report field is pre-filled with when
 * a report is created (legacy BackChart's screen of the same name). The text is
 * stamped onto the report at creation and belongs to it from then on, so editing
 * a default here never rewrites reports that already exist.
 */
export function ReportDefaultTextPage() {
  const { user } = useAuth();
  const queryClient = useQueryClient();
  const canView = user?.permissions?.includes(VIEW_PERM) ?? false;
  const canUpdate = user?.permissions?.includes(UPDATE_PERM) ?? false;

  const [selectedId, setSelectedId] = useState<number | null>(null);
  const [draft, setDraft] = useState("");

  const typesQuery = useQuery({
    queryKey: ["report-types", "default-text"],
    queryFn: () => listReportTypes(true),
    enabled: canView,
  });
  const types: ReportTypeDto[] = useMemo(() => typesQuery.data ?? [], [typesQuery.data]);

  // Inactive fields included: a field that's hidden today still carries its
  // boilerplate for when it's switched back on.
  const fieldQueries = useQueries({
    queries: types.map((t) => ({
      queryKey: ["report-fields", t.id, "default-text"],
      queryFn: () => listReportFields(t.id, true),
      enabled: canView,
    })),
  });

  const fieldsByType = useMemo(
    () => types.map((t, i) => ({ type: t, fields: fieldQueries[i]?.data ?? [] })),
    // fieldQueries is a fresh array each render; its data is what matters.
    // eslint-disable-next-line react-hooks/exhaustive-deps
    [types, fieldQueries.map((q) => q.data)],
  );

  const selected: ReportFieldDto | null = useMemo(() => {
    if (selectedId == null) return null;
    for (const group of fieldsByType) {
      const f = group.fields.find((x) => x.id === selectedId);
      if (f) return f;
    }
    return null;
  }, [selectedId, fieldsByType]);

  const selectField = (f: ReportFieldDto) => {
    setSelectedId(f.id);
    setDraft(f.defaultText ?? "");
  };

  const saveMutation = useMutation({
    mutationFn: updateReportField,
    onSuccess: () => {
      toast.success("Default text saved.");
      void queryClient.invalidateQueries({ queryKey: ["report-fields"] });
    },
    onError: (err) => toast.error("Failed to save default text.", { description: describe(err) }),
  });

  const onSave = () => {
    if (!selected) return;
    // The whole field round-trips on PUT, so the rest of it is passed back
    // unchanged — this screen only owns the default text.
    saveMutation.mutate({
      id: selected.id,
      name: selected.name,
      category: selected.category ?? null,
      displayOrder: selected.displayOrder,
      isActive: selected.isActive,
      defaultText: draft.trim() === "" ? null : draft,
    });
  };

  const isDirty = selected != null && draft !== (selected.defaultText ?? "");
  const isLoading = typesQuery.isLoading || fieldQueries.some((q) => q.isLoading);

  if (!canView) {
    return (
      <EntityEmpty
        title="Not permitted"
        description="You don't have permission to view report templates."
      />
    );
  }

  return (
    <div className="space-y-4 sm:space-y-6">
      <EntityPageHeader
        icon={FileSignature}
        title="Report Default Text"
        description="Boilerplate a report field starts with. Applied when a report is created — changing it here won't alter reports that already exist."
      />

      {isLoading ? (
        <EntityListLoading />
      ) : types.length === 0 ? (
        <EntityEmpty title="No report types" description="Add a report type first." />
      ) : (
        <div className="grid gap-4 lg:grid-cols-2">
          {/* Report types → their fields */}
          <div className="space-y-4">
            {fieldsByType.map(({ type, fields }) => (
              <div
                key={type.id}
                className="rounded-xl border border-[var(--color-border)] bg-[var(--color-card)] p-4"
              >
                <h3 className="mb-2 text-[12px] font-semibold uppercase tracking-wide text-[var(--color-primary)]">
                  {type.name}
                </h3>
                {fields.length === 0 ? (
                  <p className="text-[12px] text-[var(--color-muted-foreground)]">
                    This report type has no fields.
                  </p>
                ) : (
                  <ul className="divide-y divide-[var(--color-border)] rounded-lg border border-[var(--color-border)]">
                    {fields.map((f) => (
                      <li key={f.id}>
                        <button
                          type="button"
                          data-testid="default-text-field"
                          onClick={() => selectField(f)}
                          className={cn(
                            "flex w-full items-center justify-between gap-2 px-3 py-2 text-left text-[13px]",
                            "transition-colors hover:bg-[var(--color-accent)]",
                            f.id === selectedId && "bg-[var(--color-primary-soft)]",
                          )}
                        >
                          <span className="min-w-0 flex-1 truncate font-medium">{f.name}</span>
                          {/* Which fields already carry boilerplate, at a glance. */}
                          {f.defaultText ? (
                            <span className="shrink-0 rounded-full bg-[var(--color-muted)] px-1.5 py-0.5 text-[10px] font-semibold uppercase tracking-wider text-[var(--color-muted-foreground)]">
                              Set
                            </span>
                          ) : null}
                        </button>
                      </li>
                    ))}
                  </ul>
                )}
              </div>
            ))}
          </div>

          {/* The selected field's default text */}
          <div className="rounded-xl border border-[var(--color-border)] bg-[var(--color-card)] p-4">
            {!selected ? (
              <p className="text-[13px] text-[var(--color-muted-foreground)]">
                Pick a field to edit the text a new report starts it with.
              </p>
            ) : (
              <div className="space-y-3">
                <div>
                  <h3 className="text-[15px] font-semibold leading-tight">{selected.name}</h3>
                  <p className="text-[12px] text-[var(--color-muted-foreground)]">
                    {selected.category ?? "General"}
                  </p>
                </div>

                <Textarea
                  id="default-text"
                  data-testid="default-text-editor"
                  aria-label="Default text"
                  value={draft}
                  onChange={(e) => setDraft(e.target.value)}
                  rows={14}
                  maxLength={16000}
                  disabled={!canUpdate}
                  placeholder={
                    canUpdate ? "Text every new report starts this field with…" : "—"
                  }
                />

                {canUpdate && (
                  <div className="flex justify-end">
                    <Button onClick={onSave} disabled={!isDirty || saveMutation.isPending}>
                      <Save className="size-4" />
                      {saveMutation.isPending ? "Saving…" : "Save"}
                    </Button>
                  </div>
                )}
              </div>
            )}
          </div>
        </div>
      )}
    </div>
  );
}
