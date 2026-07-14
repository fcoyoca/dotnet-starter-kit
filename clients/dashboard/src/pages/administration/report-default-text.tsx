import { useMemo, useRef, useState } from "react";
import { useMutation, useQueries, useQuery, useQueryClient } from "@tanstack/react-query";
import { ChevronDown, FileSignature, Save, Search, Undo2 } from "lucide-react";
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
import { Input } from "@/components/ui/input";
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
 *
 * Layout is a master–detail rail: report types are accordion groups in a rail that
 * scrolls on its own, next to a full-height editor. Neither pane moves the page, so
 * picking a field never scrolls the editor off screen — with a dozen report types
 * the old stacked layout made you hunt up and down between the two.
 */
export function ReportDefaultTextPage() {
  const { user } = useAuth();
  const queryClient = useQueryClient();
  const canView = user?.permissions?.includes(VIEW_PERM) ?? false;
  const canUpdate = user?.permissions?.includes(UPDATE_PERM) ?? false;

  const [selectedId, setSelectedId] = useState<number | null>(null);
  const [search, setSearch] = useState("");
  // Edits are kept per field, so clicking through fields to compare them never
  // silently throws away what you typed in the one you left.
  const [drafts, setDrafts] = useState<Record<number, string>>({});
  // Every group starts collapsed, so the rail opens as a short list of report types
  // rather than every field of every type at once — you expand the one you came for.
  const [openTypes, setOpenTypes] = useState<number[]>([]);
  const editorRef = useRef<HTMLDivElement>(null);

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

  // A search matching the type name keeps that type's whole field list, so you can
  // pull up "Progress Note" as a unit rather than field by field.
  const term = search.trim().toLowerCase();
  const groups = useMemo(() => {
    if (!term) return fieldsByType;
    return fieldsByType
      .map(({ type, fields }) =>
        type.name.toLowerCase().includes(term)
          ? { type, fields }
          : { type, fields: fields.filter((f) => f.name.toLowerCase().includes(term)) },
      )
      .filter((g) => g.fields.length > 0);
  }, [fieldsByType, term]);

  const selected: ReportFieldDto | null = useMemo(() => {
    if (selectedId == null) return null;
    for (const group of fieldsByType) {
      const f = group.fields.find((x) => x.id === selectedId);
      if (f) return f;
    }
    return null;
  }, [selectedId, fieldsByType]);

  const draftOf = (f: ReportFieldDto) => drafts[f.id] ?? f.defaultText ?? "";
  const isFieldDirty = (f: ReportFieldDto) => draftOf(f) !== (f.defaultText ?? "");
  const draft = selected ? draftOf(selected) : "";
  const isDirty = selected != null && isFieldDirty(selected);

  // While searching, every surviving group is open — a hit you can't see is a hit
  // you'll think doesn't exist.
  const isOpen = (typeId: number) => term !== "" || openTypes.includes(typeId);

  const toggleType = (typeId: number) => {
    setOpenTypes((prev) =>
      prev.includes(typeId) ? prev.filter((id) => id !== typeId) : [...prev, typeId],
    );
  };

  const selectField = (f: ReportFieldDto) => {
    setSelectedId(f.id);
    // Stacked on small screens the editor sits below the rail, so bring it to the
    // user rather than making them scroll to it.
    if (typeof window !== "undefined" && window.matchMedia("(max-width: 1023px)").matches) {
      requestAnimationFrame(() => editorRef.current?.scrollIntoView({ behavior: "smooth", block: "start" }));
    }
  };

  // Drops a field's draft once it's no longer pending — saved, or thrown away.
  const clearDraft = (fieldId: number) =>
    setDrafts((prev) => {
      const next = { ...prev };
      delete next[fieldId];
      return next;
    });

  const saveMutation = useMutation({
    mutationFn: updateReportField,
    onSuccess: (_data, variables) => {
      toast.success("Default text saved.");
      clearDraft(variables.id);
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

  const onRevert = () => {
    if (!selected) return;
    clearDraft(selected.id);
  };

  const isLoading = typesQuery.isLoading || fieldQueries.some((q) => q.isLoading);
  const dirtyCount = fieldsByType.reduce(
    (n, g) => n + g.fields.filter((f) => isFieldDirty(f)).length,
    0,
  );

  if (!canView) {
    return (
      <EntityEmpty
        title="Not permitted"
        description="You don't have permission to view report templates."
      />
    );
  }

  return (
    <div className="flex flex-col gap-4">
      <div className="shrink-0">
        <EntityPageHeader
          icon={FileSignature}
          title="Report Default Text"
          description="Boilerplate a report field starts with. Applied when a report is created — changing it here won't alter reports that already exist."
        />
      </div>

      {isLoading ? (
        <EntityListLoading />
      ) : types.length === 0 ? (
        <EntityEmpty title="No report types" description="Add a report type first." />
      ) : (
        // The panes get an explicit height rather than h-full: this screen renders
        // inside the Administration dialog, whose body is `max-h-[85vh] overflow-y-auto`
        // — auto height, so a percentage height here would collapse to content and the
        // dialog itself would be what scrolls. Sized to sit inside that 85vh once the
        // dialog's padding, the back link and this page's header are taken out, so the
        // rail scrolls on its own and the editor never leaves the viewport.
        <div className="grid gap-4 lg:h-[max(320px,min(560px,calc(85vh-190px)))] lg:min-h-0 lg:grid-cols-[minmax(280px,340px)_minmax(0,1fr)] lg:grid-rows-[minmax(0,1fr)]">
          {/* ─── Rail: report types as accordion groups. Scrolls on its own so the
              editor beside it never leaves the viewport. ─── */}
          <div className="flex min-w-0 flex-col rounded-xl border border-[var(--color-border)] bg-[var(--color-card)] lg:min-h-0">
            <div className="shrink-0 border-b border-[var(--color-border)] p-3">
              <div className="relative">
                <Search
                  aria-hidden
                  className="pointer-events-none absolute left-2.5 top-1/2 size-4 -translate-y-1/2 text-[var(--color-muted-foreground)]"
                />
                <Input
                  type="search"
                  data-testid="default-text-search"
                  aria-label="Search fields"
                  placeholder="Search fields…"
                  value={search}
                  onChange={(e) => setSearch(e.target.value)}
                  className="pl-8"
                />
              </div>
              {dirtyCount > 0 && (
                <p className="mt-2 text-[11px] font-medium text-[var(--color-primary)]">
                  {dirtyCount} field{dirtyCount === 1 ? "" : "s"} with unsaved changes
                </p>
              )}
            </div>

            <div className="min-h-0 flex-1 overflow-y-auto p-2 lg:overflow-y-auto">
              {groups.length === 0 ? (
                <p className="p-3 text-[12px] text-[var(--color-muted-foreground)]">
                  No field matches “{search.trim()}”.
                </p>
              ) : (
                groups.map(({ type, fields }) => {
                  const open = isOpen(type.id);
                  const setCount = fields.filter((f) => f.defaultText).length;
                  return (
                    <div key={type.id} className="mb-1 last:mb-0">
                      <button
                        type="button"
                        data-testid="default-text-group"
                        onClick={() => toggleType(type.id)}
                        aria-expanded={open}
                        aria-controls={`fields-${type.id}`}
                        className={cn(
                          "flex w-full items-center gap-2 rounded-lg px-2 py-2 text-left",
                          "transition-colors hover:bg-[var(--color-accent)]",
                          "focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-[var(--color-ring)]",
                        )}
                      >
                        <ChevronDown
                          aria-hidden
                          className={cn(
                            "size-3.5 shrink-0 text-[var(--color-muted-foreground)]",
                            "transition-transform duration-[var(--duration-default)] ease-[var(--ease-out-cubic)]",
                            open ? "rotate-0" : "-rotate-90",
                          )}
                        />
                        <span className="min-w-0 flex-1 truncate text-[12px] font-semibold uppercase tracking-wide text-[var(--color-primary)]">
                          {type.name}
                        </span>
                        {/* How much of this type is already written, without opening it. */}
                        <span className="shrink-0 text-[11px] tabular-nums text-[var(--color-muted-foreground)]">
                          {setCount}/{fields.length}
                        </span>
                      </button>

                      {/* Open/close via the grid 0fr ↔ 1fr trick, same as the sidebar
                          accordion — animates height without measuring it. The panel
                          also flips to visibility:hidden when closed, so collapsed
                          fields leave the tab order and the accessibility tree instead
                          of merely being clipped. Transitioning `visibility` keeps them
                          on screen for the length of the close animation (CSS holds the
                          `visible` endpoint for the whole transition). */}
                      <div
                        id={`fields-${type.id}`}
                        className={cn(
                          "grid transition-[grid-template-rows] duration-[var(--duration-default)] ease-[var(--ease-out-cubic)]",
                          open ? "grid-rows-[1fr]" : "grid-rows-[0fr]",
                        )}
                      >
                        <div
                          className={cn(
                            "min-h-0 overflow-hidden",
                            "transition-[visibility] duration-[var(--duration-default)]",
                            open ? "visible" : "invisible",
                          )}
                        >
                          {fields.length === 0 ? (
                            <p className="px-2 py-1.5 pl-7 text-[12px] text-[var(--color-muted-foreground)]">
                              This report type has no fields.
                            </p>
                          ) : (
                            <ul className="pb-1">
                              {fields.map((f) => (
                                <li key={f.id}>
                                  <button
                                    type="button"
                                    data-testid="default-text-field"
                                    onClick={() => selectField(f)}
                                    aria-current={f.id === selectedId}
                                    className={cn(
                                      "flex w-full items-center gap-2 rounded-lg py-1.5 pl-7 pr-2 text-left text-[13px]",
                                      "transition-colors hover:bg-[var(--color-accent)]",
                                      "focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-[var(--color-ring)]",
                                      f.id === selectedId &&
                                        "bg-[var(--color-primary-soft)] hover:bg-[var(--color-primary-soft)]",
                                    )}
                                  >
                                    <span className="min-w-0 flex-1 truncate font-medium">
                                      {f.name}
                                    </span>
                                    {/* Unsaved beats Set — an edit in flight is the more
                                        urgent thing to tell them about. */}
                                    {isFieldDirty(f) ? (
                                      <span className="shrink-0 rounded-full bg-[var(--color-primary)] px-1.5 py-0.5 text-[10px] font-semibold uppercase tracking-wider text-[var(--color-primary-foreground)]">
                                        Unsaved
                                      </span>
                                    ) : f.defaultText ? (
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
                      </div>
                    </div>
                  );
                })
              )}
            </div>
          </div>

          {/* ─── Editor: fills the pane, so the textarea grows with the window
              instead of a fixed 14 rows the long boilerplate scrolls inside. ─── */}
          <div
            ref={editorRef}
            className="flex min-w-0 flex-col rounded-xl border border-[var(--color-border)] bg-[var(--color-card)] p-4 lg:min-h-0"
          >
            {!selected ? (
              <div className="grid flex-1 place-items-center p-8 text-center">
                <p className="text-[13px] text-[var(--color-muted-foreground)]">
                  Pick a field to edit the text a new report starts it with.
                </p>
              </div>
            ) : (
              <>
                <div className="mb-3 flex shrink-0 items-start justify-between gap-3">
                  <div className="min-w-0">
                    <h3 className="truncate text-[15px] font-semibold leading-tight">
                      {selected.name}
                    </h3>
                    <p className="text-[12px] text-[var(--color-muted-foreground)]">
                      {selected.category ?? "General"}
                    </p>
                  </div>
                  <span className="shrink-0 text-[11px] tabular-nums text-[var(--color-muted-foreground)]">
                    {draft.length.toLocaleString()} / 16,000
                  </span>
                </div>

                <Textarea
                  id="default-text"
                  data-testid="default-text-editor"
                  aria-label="Default text"
                  value={draft}
                  onChange={(e) =>
                    setDrafts((prev) => ({ ...prev, [selected.id]: e.target.value }))
                  }
                  rows={14}
                  maxLength={16000}
                  disabled={!canUpdate}
                  placeholder={canUpdate ? "Text every new report starts this field with…" : "—"}
                  className="min-h-[220px] flex-1 resize-none lg:min-h-0"
                />

                {canUpdate && (
                  <div className="mt-3 flex shrink-0 items-center justify-end gap-2">
                    {isDirty && (
                      <Button variant="ghost" onClick={onRevert} disabled={saveMutation.isPending}>
                        <Undo2 className="size-4" />
                        Discard
                      </Button>
                    )}
                    <Button onClick={onSave} disabled={!isDirty || saveMutation.isPending}>
                      <Save className="size-4" />
                      {saveMutation.isPending ? "Saving…" : "Save"}
                    </Button>
                  </div>
                )}
              </>
            )}
          </div>
        </div>
      )}
    </div>
  );
}
