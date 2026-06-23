import { useEffect, useMemo, useState, type FormEvent } from "react";
import {
  keepPreviousData,
  useMutation,
  useQuery,
  useQueryClient,
} from "@tanstack/react-query";
import { ChevronRight, Layers, Pencil, Plus, ScrollText, Search, Trash2 } from "lucide-react";
import { toast } from "sonner";
import {
  createMacro,
  deleteMacro,
  listMacros,
  listReportFields,
  listReportTypes,
  updateMacro,
  type MacroDto,
  type CreateMacroInput,
  type UpdateMacroInput,
} from "@/api/administration";
import { Button } from "@/components/ui/button";
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
import { Input } from "@/components/ui/input";
import { Switch } from "@/components/ui/switch";
import {
  EntityEmpty,
  EntityInitialsAvatar,
  EntityListCard,
  EntityListHeader,
  EntityListLoading,
  EntityListRow,
  EntityMobileCard,
  EntityPageHeader,
  EntityPager,
  EntitySearch,
  EntityStatusBadge,
  Field,
} from "@/components/list";
import { describe, formatDate } from "@/lib/list-helpers";
import { cn } from "@/lib/cn";

const PAGE_SIZE = 20;

const textareaClass = cn(
  "flex w-full rounded-lg border border-[var(--color-input)] bg-transparent px-3 py-2 text-sm shadow-xs",
  "placeholder:text-[oklch(from_var(--color-muted-foreground)_l_c_h_/_0.6)]",
  "focus-visible:border-[var(--color-ring)] focus-visible:outline-none focus-visible:ring-[3px] focus-visible:ring-[oklch(from_var(--color-ring)_l_c_h_/_0.5)]",
);

/** The "All (General)" pseudo-field: macros not tied to a specific report field. */
const GENERAL = { id: null as number | null, name: "All (General)" };

type SelectedField = { id: number | null; name: string };

type EditorState =
  | { mode: "closed" }
  | { mode: "create" }
  | { mode: "edit"; macro: MacroDto }
  | { mode: "delete"; macro: MacroDto };

export function MacrosPage() {
  const [reportTypeId, setReportTypeId] = useState(1);
  const [field, setField] = useState<SelectedField>(GENERAL);

  const [search, setSearch] = useState("");
  const [debouncedSearch, setDebouncedSearch] = useState("");
  const [showInactive, setShowInactive] = useState(false);
  const [pageNumber, setPageNumber] = useState(1);
  const [editor, setEditor] = useState<EditorState>({ mode: "closed" });

  useEffect(() => {
    const t = setTimeout(() => {
      setDebouncedSearch(search.trim());
      setPageNumber(1);
    }, 250);
    return () => clearTimeout(t);
  }, [search]);

  const typesQuery = useQuery({
    queryKey: ["administration", "report-types"],
    queryFn: listReportTypes,
    staleTime: 5 * 60 * 1000,
  });

  const fieldsQuery = useQuery({
    queryKey: ["administration", "report-fields", reportTypeId],
    queryFn: () => listReportFields(reportTypeId),
    staleTime: 5 * 60 * 1000,
  });

  const macrosQuery = useQuery({
    queryKey: [
      "administration",
      "macros",
      { fieldId: field.id, search: debouncedSearch, showInactive, pageNumber },
    ],
    queryFn: () =>
      listMacros({
        search: debouncedSearch || undefined,
        reportFieldId: field.id ?? undefined,
        general: field.id === null,
        isActive: showInactive ? undefined : true,
        pageNumber,
        pageSize: PAGE_SIZE,
      }),
    placeholderData: keepPreviousData,
  });

  const onSelectType = (id: number) => {
    setReportTypeId(id);
    setField(GENERAL);
    setPageNumber(1);
  };

  const onSelectField = (f: SelectedField) => {
    setField(f);
    setPageNumber(1);
  };

  const data = macrosQuery.data;
  const items = data?.items ?? [];
  const searchActive = debouncedSearch.length > 0;
  const fields = fieldsQuery.data ?? [];

  return (
    <div className="space-y-4 sm:space-y-6">
      <EntityPageHeader
        icon={ScrollText}
        title="Macros"
        description="Reusable text snippets, organized by report type and field. Fields shared across report types share their macros."
      />

      {/* Report type → field master selectors */}
      <div className="grid gap-4 lg:grid-cols-2">
        <SelectorCard title="Report type" hint="Choose a report type.">
          {typesQuery.isLoading ? (
            <SelectorSkeleton />
          ) : (
            (typesQuery.data ?? []).map((t) => (
              <SelectorRow
                key={t.id}
                label={t.name}
                selected={t.id === reportTypeId}
                onClick={() => onSelectType(t.id)}
              />
            ))
          )}
        </SelectorCard>

        <SelectorCard
          title="Fields"
          hint="Click a field to view its macros, or use “All (General)”."
        >
          <SelectorRow
            label={GENERAL.name}
            icon={Layers}
            selected={field.id === null}
            onClick={() => onSelectField(GENERAL)}
          />
          {fieldsQuery.isLoading ? (
            <SelectorSkeleton />
          ) : (
            fields.map((f) => (
              <SelectorRow
                key={f.id}
                label={f.name}
                sub={f.category && f.category !== f.name ? f.category : undefined}
                selected={field.id === f.id}
                onClick={() => onSelectField({ id: f.id, name: f.name })}
              />
            ))
          )}
        </SelectorCard>
      </div>

      {/* Macros for the selected field */}
      <div className="rounded-xl border border-[var(--color-border)] p-4 sm:p-5">
        <div className="mb-4 flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
          <div>
            <h2 className="text-[14px] font-semibold text-[var(--color-foreground)]">
              Macros for {field.name}
            </h2>
            <p className="text-[12px] text-[var(--color-muted-foreground)]">
              {data?.totalCount ?? 0} macro{(data?.totalCount ?? 0) !== 1 ? "s" : ""}
            </p>
          </div>
          <div className="flex items-center gap-3">
            <label
              htmlFor="show-inactive"
              className="flex cursor-pointer items-center gap-2 text-[12px] text-[var(--color-muted-foreground)]"
            >
              <Switch
                id="show-inactive"
                checked={showInactive}
                onCheckedChange={(v) => {
                  setShowInactive(v);
                  setPageNumber(1);
                }}
                aria-label="Show inactive macros"
              />
              Show inactive
            </label>
            <Button
              onClick={() => setEditor({ mode: "create" })}
              className="h-9 gap-1.5 rounded-lg px-4 text-[13px] font-semibold"
            >
              <Plus className="size-4" />
              New macro
            </Button>
          </div>
        </div>

        <EntitySearch value={search} onChange={setSearch} placeholder="Search by name…" />

        <div className="mt-4">
          {macrosQuery.isLoading && items.length === 0 ? (
            <EntityListLoading desktopColumns="grid-cols-[1fr_90px_24px]" />
          ) : items.length === 0 ? (
            <EntityEmpty
              icon={searchActive ? Search : ScrollText}
              title={searchActive ? "No macros found" : "No macros here yet"}
              body={
                searchActive
                  ? `Nothing matches "${debouncedSearch}".`
                  : `Add a macro for ${field.name} to reuse boilerplate text.`
              }
              action={
                searchActive ? (
                  <Button variant="outline" onClick={() => setSearch("")} className="h-9 rounded-lg px-4 text-[13px]">
                    Clear search
                  </Button>
                ) : (
                  <Button onClick={() => setEditor({ mode: "create" })} className="h-9 rounded-lg px-4 text-[13px]">
                    <Plus className="mr-1.5 size-4" />
                    Add macro
                  </Button>
                )
              }
            />
          ) : (
            <>
              <div className="space-y-2 md:hidden">
                {items.map((m) => (
                  <MobileCard key={m.id} macro={m} onEdit={() => setEditor({ mode: "edit", macro: m })} />
                ))}
              </div>

              <EntityListCard className="hidden md:block">
                <EntityListHeader className="grid-cols-[1fr_90px_24px]">
                  <span>Macro</span>
                  <span>Status</span>
                  <span />
                </EntityListHeader>
                {items.map((m, i) => (
                  <DesktopRow
                    key={m.id}
                    macro={m}
                    isLast={i === items.length - 1}
                    onEdit={() => setEditor({ mode: "edit", macro: m })}
                    onDelete={() => setEditor({ mode: "delete", macro: m })}
                  />
                ))}
              </EntityListCard>

              <EntityPager
                page={data?.pageNumber ?? 1}
                totalPages={data?.totalPages ?? 1}
                hasPrev={!!data?.hasPrevious}
                hasNext={!!data?.hasNext}
                onPrev={() => setPageNumber((p) => Math.max(1, p - 1))}
                onNext={() => setPageNumber((p) => p + 1)}
              />
            </>
          )}

          {macrosQuery.isError && (
            <div
              role="alert"
              className="mt-3 rounded-lg border border-[oklch(from_var(--color-destructive)_l_c_h_/_0.30)] bg-[oklch(from_var(--color-destructive)_l_c_h_/_0.06)] px-3 py-2 text-sm text-[var(--color-destructive)]"
            >
              {describe(macrosQuery.error)}
            </div>
          )}
        </div>
      </div>

      <MacroEditorDialog state={editor} field={field} onClose={() => setEditor({ mode: "closed" })} />
      <DeleteMacroDialog state={editor} onClose={() => setEditor({ mode: "closed" })} />
    </div>
  );
}

function SelectorCard({
  title,
  hint,
  children,
}: {
  title: string;
  hint: string;
  children: React.ReactNode;
}) {
  return (
    <div className="rounded-xl border border-[var(--color-border)] p-3 sm:p-4">
      <h2 className="text-[14px] font-semibold text-[var(--color-foreground)]">{title}</h2>
      <p className="mb-2 text-[12px] text-[var(--color-muted-foreground)]">{hint}</p>
      <div className="max-h-[260px] space-y-1 overflow-y-auto pr-1">{children}</div>
    </div>
  );
}

function SelectorRow({
  label,
  sub,
  icon: Icon,
  selected,
  onClick,
}: {
  label: string;
  sub?: string;
  icon?: typeof Layers;
  selected: boolean;
  onClick: () => void;
}) {
  return (
    <button
      type="button"
      onClick={onClick}
      aria-pressed={selected}
      className={cn(
        "flex w-full cursor-pointer items-center gap-2 rounded-lg px-3 py-2 text-left text-[13px] transition-colors",
        selected
          ? "bg-[var(--color-primary)] text-[var(--color-primary-foreground)]"
          : "text-[var(--color-foreground)] hover:bg-[var(--color-muted)]",
      )}
    >
      {Icon ? <Icon className="size-3.5 shrink-0 opacity-80" /> : null}
      <span className="min-w-0 flex-1">
        <span className="block truncate font-medium">{label}</span>
        {sub ? (
          <span
            className={cn(
              "block truncate text-[11px]",
              selected ? "opacity-80" : "text-[var(--color-muted-foreground)]",
            )}
          >
            {sub}
          </span>
        ) : null}
      </span>
    </button>
  );
}

function SelectorSkeleton() {
  return (
    <div className="space-y-1.5 py-1">
      {[0, 1, 2].map((i) => (
        <div key={i} className="h-8 animate-pulse rounded-lg bg-[var(--color-muted)]" />
      ))}
    </div>
  );
}

function MobileCard({ macro, onEdit }: { macro: MacroDto; onEdit: () => void }) {
  return (
    <EntityMobileCard
      href="#"
      onClick={(e) => {
        e.preventDefault();
        onEdit();
      }}
      aria-label={`Edit macro ${macro.name}`}
    >
      <div className="flex items-center justify-between">
        <div className="flex min-w-0 items-center gap-3">
          <EntityInitialsAvatar name={macro.name} size={40} />
          <p className="truncate text-[14px] font-medium text-[var(--color-foreground)]">{macro.name}</p>
        </div>
        <EntityStatusBadge tone={macro.isActive ? "success" : "default"}>
          {macro.isActive ? "Active" : "Inactive"}
        </EntityStatusBadge>
      </div>
    </EntityMobileCard>
  );
}

function DesktopRow({
  macro,
  isLast,
  onEdit,
  onDelete,
}: {
  macro: MacroDto;
  isLast: boolean;
  onEdit: () => void;
  onDelete: () => void;
}) {
  return (
    <EntityListRow className="grid-cols-[1fr_90px_24px]" isLast={isLast}>
      <div className="flex min-w-0 items-center gap-3">
        <EntityInitialsAvatar name={macro.name} size={36} />
        <div className="min-w-0">
          <div className="truncate text-[14px] font-medium text-[var(--color-foreground)] transition-colors group-hover:text-[var(--color-primary)]">
            {macro.name}
          </div>
          {macro.text ? (
            <div className="truncate text-[12px] text-[var(--color-muted-foreground)]">{macro.text}</div>
          ) : null}
        </div>
      </div>
      <div className="flex items-center">
        <EntityStatusBadge tone={macro.isActive ? "success" : "default"}>
          {macro.isActive ? "Active" : "Inactive"}
        </EntityStatusBadge>
      </div>
      <div className="flex items-center justify-end gap-1">
        <button
          type="button"
          aria-label={`Edit ${macro.name}`}
          onClick={onEdit}
          className="grid size-7 cursor-pointer place-items-center rounded-md text-[var(--color-muted-foreground)] opacity-0 transition-all hover:bg-[var(--color-muted)] hover:text-[var(--color-foreground)] group-hover:opacity-100"
        >
          <Pencil className="size-3.5" />
        </button>
        <button
          type="button"
          aria-label={`Delete ${macro.name}`}
          onClick={onDelete}
          className="grid size-7 cursor-pointer place-items-center rounded-md text-[var(--color-muted-foreground)] opacity-0 transition-all hover:bg-[var(--color-muted)] hover:text-[var(--color-destructive)] group-hover:opacity-100"
        >
          <Trash2 className="size-3.5" />
        </button>
        <ChevronRight className="size-4 text-[var(--color-border)] transition-colors group-hover:text-[var(--color-muted-foreground)]" />
      </div>
    </EntityListRow>
  );
}

function MacroEditorDialog({
  state,
  field,
  onClose,
}: {
  state: EditorState;
  field: SelectedField;
  onClose: () => void;
}) {
  const isOpen = state.mode === "create" || state.mode === "edit";
  const macro = state.mode === "edit" ? state.macro : undefined;
  const queryClient = useQueryClient();

  const initial = useMemo(
    () => ({ name: macro?.name ?? "", text: macro?.text ?? "", isActive: macro?.isActive ?? true }),
    [macro],
  );

  const [form, setForm] = useState(initial);
  useEffect(() => {
    if (isOpen) setForm(initial);
  }, [isOpen, initial]);

  const invalidate = () => queryClient.invalidateQueries({ queryKey: ["administration", "macros"] });

  const createMutation = useMutation({
    mutationFn: (input: CreateMacroInput) => createMacro(input),
    onSuccess: () => {
      toast.success("Macro created");
      invalidate();
      onClose();
    },
    onError: (err) => toast.error("Create failed", { description: describe(err) }),
  });

  const updateMutation = useMutation({
    mutationFn: (input: UpdateMacroInput) => updateMacro(input),
    onSuccess: () => {
      toast.success("Macro updated");
      invalidate();
      onClose();
    },
    onError: (err) => toast.error("Update failed", { description: describe(err) }),
  });

  const isPending = createMutation.isPending || updateMutation.isPending;
  const trimmedName = form.name.trim();

  // Where a new macro lands: the currently selected field (null = General).
  const targetFieldName = macro
    ? (macro.reportFieldName ?? "All (General)")
    : field.name;

  const onSubmit = (e: FormEvent<HTMLFormElement>) => {
    e.preventDefault();
    if (!trimmedName) return;
    const text = form.text.trim() || null;
    if (state.mode === "edit" && macro) {
      updateMutation.mutate({
        macroId: macro.id,
        name: trimmedName,
        text,
        reportFieldId: macro.reportFieldId ?? null,
        isActive: form.isActive,
      });
    } else {
      createMutation.mutate({ name: trimmedName, text, reportFieldId: field.id ?? null });
    }
  };

  return (
    <Dialog open={isOpen} onOpenChange={(o) => (!o ? onClose() : undefined)}>
      <DialogContent className="!max-w-lg">
        <form onSubmit={onSubmit}>
          <DialogHeader>
            <DialogTitle>{macro ? "Edit macro" : "Add a macro"}</DialogTitle>
            <DialogDescription>
              {macro
                ? `Update details for ${macro.name}.`
                : `Add a reusable text snippet for ${targetFieldName}.`}
            </DialogDescription>
          </DialogHeader>

          <DialogBody className="space-y-5">
            <div className="flex items-center gap-2 rounded-lg border border-[var(--color-border)] bg-[var(--color-muted)] px-3 py-2 text-[12px]">
              <Layers className="size-3.5 text-[var(--color-muted-foreground)]" />
              <span className="text-[var(--color-muted-foreground)]">Field:</span>
              <span className="font-medium text-[var(--color-foreground)]">{targetFieldName}</span>
            </div>

            <Field id="macro-name" label="Name" required>
              <Input
                id="macro-name"
                value={form.name}
                onChange={(e) => setForm((f) => ({ ...f, name: e.target.value }))}
                placeholder="Normal Exam"
                autoFocus
                required
                maxLength={200}
              />
            </Field>

            <Field id="macro-text" label="Text" hint="The boilerplate inserted when this macro is applied.">
              <textarea
                id="macro-text"
                value={form.text}
                onChange={(e) => setForm((f) => ({ ...f, text: e.target.value }))}
                placeholder="Patient is well-appearing and in no acute distress…"
                rows={6}
                maxLength={8000}
                className={textareaClass}
              />
            </Field>

            {macro && (
              <div className="flex items-center justify-between rounded-lg border border-[var(--color-border)] px-3 py-2.5">
                <div>
                  <p className="text-[13px] font-medium text-[var(--color-foreground)]">Active</p>
                  <p className="text-[12px] text-[var(--color-muted-foreground)]">
                    Inactive macros are hidden from selection lists.
                  </p>
                </div>
                <Switch
                  checked={form.isActive}
                  onCheckedChange={(v) => setForm((f) => ({ ...f, isActive: v }))}
                  aria-label="Macro active"
                />
              </div>
            )}
          </DialogBody>

          <DialogFooter>
            <DialogClose asChild>
              <Button type="button" variant="outline" disabled={isPending}>
                Cancel
              </Button>
            </DialogClose>
            <Button type="submit" disabled={isPending || !trimmedName}>
              {isPending ? "Saving…" : macro ? "Save changes" : "Add macro"}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}

function DeleteMacroDialog({ state, onClose }: { state: EditorState; onClose: () => void }) {
  const isOpen = state.mode === "delete";
  const macro = state.mode === "delete" ? state.macro : undefined;
  const queryClient = useQueryClient();

  const deleteMutation = useMutation({
    mutationFn: (id: string) => deleteMacro(id),
    onSuccess: () => {
      toast.success("Macro deleted");
      queryClient.invalidateQueries({ queryKey: ["administration", "macros"] });
      onClose();
    },
    onError: (err) => toast.error("Delete failed", { description: describe(err) }),
  });

  return (
    <Dialog open={isOpen} onOpenChange={(o) => (!o ? onClose() : undefined)}>
      <DialogContent>
        <DialogHeader>
          <DialogTitle className="text-[var(--color-destructive)]">Delete macro</DialogTitle>
          <DialogDescription>
            This removes{" "}
            <span className="font-medium text-[var(--color-foreground)]">{macro?.name}</span>{" "}
            <span className="opacity-70">(created {macro && formatDate(macro.createdAtUtc)})</span>.
          </DialogDescription>
        </DialogHeader>
        <DialogFooter>
          <DialogClose asChild>
            <Button type="button" variant="outline" disabled={deleteMutation.isPending}>
              Cancel
            </Button>
          </DialogClose>
          <Button
            variant="destructive"
            onClick={() => macro && deleteMutation.mutate(macro.id)}
            disabled={deleteMutation.isPending || !macro}
          >
            {deleteMutation.isPending ? "Deleting…" : "Delete macro"}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
