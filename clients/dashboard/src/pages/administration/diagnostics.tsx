import { useEffect, useMemo, useState, type FormEvent } from "react";
import {
  keepPreviousData,
  useMutation,
  useQuery,
  useQueryClient,
} from "@tanstack/react-query";
import { ChevronRight, Pencil, Plus, Search, Stethoscope, Trash2 } from "lucide-react";
import { toast } from "sonner";
import {
  createDiagnostic,
  deleteDiagnostic,
  listDiagnostics,
  updateDiagnostic,
  useCodeSourceOptions,
  type DiagnosticDto,
  type CreateDiagnosticInput,
  type UpdateDiagnosticInput,
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
  Combobox,
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
import { describe } from "@/lib/list-helpers";
import { cn } from "@/lib/cn";

const textareaClass = cn(
  "flex w-full rounded-lg border border-[var(--color-input)] bg-transparent px-3 py-2 text-sm shadow-xs",
  "placeholder:text-[oklch(from_var(--color-muted-foreground)_l_c_h_/_0.6)]",
  "focus-visible:border-[var(--color-ring)] focus-visible:outline-none focus-visible:ring-[3px] focus-visible:ring-[oklch(from_var(--color-ring)_l_c_h_/_0.5)]",
);

const PAGE_SIZE = 20;
const DEFAULT_CODE_SOURCE_ID = 7; // ICD-10-CM (see CodeSourceConfiguration seed).
const COLS = "grid-cols-[140px_1fr_110px_70px_90px_24px]";

type EditorState =
  | { mode: "closed" }
  | { mode: "create" }
  | { mode: "edit"; diagnostic: DiagnosticDto }
  | { mode: "delete"; diagnostic: DiagnosticDto };

export function DiagnosticsPage() {
  const [search, setSearch] = useState("");
  const [debouncedSearch, setDebouncedSearch] = useState("");
  const [codeSourceFilter, setCodeSourceFilter] = useState<string | null>(null);
  const [pageNumber, setPageNumber] = useState(1);
  const [editor, setEditor] = useState<EditorState>({ mode: "closed" });

  const codeSourceOptions = useCodeSourceOptions();

  useEffect(() => {
    const t = setTimeout(() => {
      setDebouncedSearch(search.trim());
      setPageNumber(1);
    }, 250);
    return () => clearTimeout(t);
  }, [search]);

  const query = useQuery({
    queryKey: ["administration", "diagnostics", { search: debouncedSearch, codeSourceFilter, pageNumber }],
    queryFn: () =>
      listDiagnostics({
        search: debouncedSearch || undefined,
        codeSourceId: codeSourceFilter ? Number(codeSourceFilter) : null,
        pageNumber,
        pageSize: PAGE_SIZE,
      }),
    placeholderData: keepPreviousData,
  });

  const data = query.data;
  const items = data?.items ?? [];
  const searchActive = debouncedSearch.length > 0 || !!codeSourceFilter;

  return (
    <div className="space-y-4 sm:space-y-6">
      <EntityPageHeader
        icon={Stethoscope}
        title="Diagnostic Details"
        total={data?.totalCount ?? null}
        unit="code"
        description="The global ICD diagnostics catalog (ICD-10-CM). Searchable code set used across patient charts."
      >
        <Button
          onClick={() => setEditor({ mode: "create" })}
          className="h-9 flex-1 gap-1.5 rounded-lg px-4 text-[13px] font-semibold sm:flex-none"
        >
          <Plus className="size-4" />
          New diagnostic
        </Button>
      </EntityPageHeader>

      <div className="flex flex-wrap items-center gap-3">
        <div className="min-w-0 flex-1">
          <EntitySearch value={search} onChange={setSearch} placeholder="Search by code or description…" />
        </div>
        <div className="w-48">
          <Combobox
            id="dx-source-filter"
            label="Source"
            variant="filter"
            value={codeSourceFilter}
            onChange={(v) => {
              setCodeSourceFilter(v);
              setPageNumber(1);
            }}
            options={codeSourceOptions ?? []}
            clearable
            placeholder="All sources"
          />
        </div>
      </div>

      {query.isLoading && items.length === 0 ? (
        <EntityListLoading desktopColumns={COLS} />
      ) : items.length === 0 ? (
        <EntityEmpty
          icon={searchActive ? Search : Stethoscope}
          title={searchActive ? "No diagnostics found" : "No diagnostics yet"}
          body={
            searchActive
              ? "Nothing matches the current search/filter."
              : "The ICD-10 catalog is empty — seed it or add a code."
          }
          action={
            searchActive ? (
              <Button
                variant="outline"
                onClick={() => {
                  setSearch("");
                  setCodeSourceFilter(null);
                }}
                className="h-9 rounded-lg px-4 text-[13px]"
              >
                Clear filters
              </Button>
            ) : (
              <Button onClick={() => setEditor({ mode: "create" })} className="h-9 rounded-lg px-4 text-[13px]">
                <Plus className="mr-1.5 size-4" />
                Add diagnostic
              </Button>
            )
          }
        />
      ) : (
        <div>
          <div className="mb-3 flex items-center justify-between">
            <p className="text-[12px] font-medium text-[var(--color-muted-foreground)]">
              {data?.totalCount ?? 0} code{(data?.totalCount ?? 0) !== 1 ? "s" : ""} found
            </p>
          </div>

          <div className="space-y-2 md:hidden">
            {items.map((d) => (
              <MobileCard key={d.id} diagnostic={d} onEdit={() => setEditor({ mode: "edit", diagnostic: d })} />
            ))}
          </div>

          <EntityListCard className="hidden md:block">
            <EntityListHeader className={COLS}>
              <span>Code</span>
              <span>Description</span>
              <span>Source</span>
              <span>Billable</span>
              <span>Status</span>
              <span />
            </EntityListHeader>
            {items.map((d, i) => (
              <DesktopRow
                key={d.id}
                diagnostic={d}
                isLast={i === items.length - 1}
                onEdit={() => setEditor({ mode: "edit", diagnostic: d })}
                onDelete={() => setEditor({ mode: "delete", diagnostic: d })}
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
        </div>
      )}

      {query.isError && (
        <div
          role="alert"
          className="rounded-lg border border-[oklch(from_var(--color-destructive)_l_c_h_/_0.30)] bg-[oklch(from_var(--color-destructive)_l_c_h_/_0.06)] px-3 py-2 text-sm text-[var(--color-destructive)]"
        >
          {describe(query.error)}
        </div>
      )}

      <DiagnosticEditorDialog
        state={editor}
        codeSourceOptions={codeSourceOptions ?? []}
        onClose={() => setEditor({ mode: "closed" })}
      />
      <DeleteDiagnosticDialog state={editor} onClose={() => setEditor({ mode: "closed" })} />
    </div>
  );
}

function MobileCard({ diagnostic, onEdit }: { diagnostic: DiagnosticDto; onEdit: () => void }) {
  return (
    <EntityMobileCard
      href="#"
      onClick={(e) => {
        e.preventDefault();
        onEdit();
      }}
      aria-label={`Edit diagnostic ${diagnostic.code}`}
    >
      <div className="flex items-center justify-between">
        <div className="flex min-w-0 items-center gap-3">
          <EntityInitialsAvatar name={diagnostic.code} size={40} />
          <div className="min-w-0">
            <p className="truncate text-[14px] font-medium text-[var(--color-foreground)]">{diagnostic.code}</p>
            {diagnostic.description && (
              <p className="mt-0.5 truncate text-[12px] text-[var(--color-muted-foreground)]">{diagnostic.description}</p>
            )}
          </div>
        </div>
        <EntityStatusBadge tone={diagnostic.isActive ? "success" : "default"}>
          {diagnostic.isActive ? "Active" : "Inactive"}
        </EntityStatusBadge>
      </div>
    </EntityMobileCard>
  );
}

function DesktopRow({
  diagnostic,
  isLast,
  onEdit,
  onDelete,
}: {
  diagnostic: DiagnosticDto;
  isLast: boolean;
  onEdit: () => void;
  onDelete: () => void;
}) {
  return (
    <EntityListRow className={COLS} isLast={isLast}>
      <div className="min-w-0 truncate text-[14px] font-medium text-[var(--color-foreground)] transition-colors group-hover:text-[var(--color-primary)]">
        {diagnostic.code}
      </div>
      <div className="min-w-0 truncate text-[12px] text-[var(--color-muted-foreground)]">
        {diagnostic.description ?? "—"}
      </div>
      <div className="flex items-center text-[12px] text-[var(--color-muted-foreground)]">
        {diagnostic.codeSourceName ?? "—"}
      </div>
      <div className="flex items-center text-[12px] text-[var(--color-muted-foreground)]">
        {diagnostic.isBillable == null ? "—" : diagnostic.isBillable ? "Yes" : "No"}
      </div>
      <div className="flex items-center">
        <EntityStatusBadge tone={diagnostic.isActive ? "success" : "default"}>
          {diagnostic.isActive ? "Active" : "Inactive"}
        </EntityStatusBadge>
      </div>
      <div className="flex items-center justify-end gap-1">
        <button
          type="button"
          aria-label={`Edit ${diagnostic.code}`}
          onClick={onEdit}
          className="grid size-7 cursor-pointer place-items-center rounded-md text-[var(--color-muted-foreground)] opacity-0 transition-all hover:bg-[var(--color-muted)] hover:text-[var(--color-foreground)] group-hover:opacity-100"
        >
          <Pencil className="size-3.5" />
        </button>
        <button
          type="button"
          aria-label={`Delete ${diagnostic.code}`}
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

function DiagnosticEditorDialog({
  state,
  codeSourceOptions,
  onClose,
}: {
  state: EditorState;
  codeSourceOptions: { value: string; label: string }[];
  onClose: () => void;
}) {
  const isOpen = state.mode === "create" || state.mode === "edit";
  const diagnostic = state.mode === "edit" ? state.diagnostic : undefined;
  const queryClient = useQueryClient();

  const initial = useMemo(
    () => ({
      code: diagnostic?.code ?? "",
      description: diagnostic?.description ?? "",
      longDescription: diagnostic?.longDescription ?? "",
      codeSourceId: String(diagnostic?.codeSourceId ?? DEFAULT_CODE_SOURCE_ID),
      isChiropractic: diagnostic?.isChiropractic ?? false,
      isBillable: diagnostic?.isBillable ?? false,
      isActive: diagnostic?.isActive ?? true,
    }),
    [diagnostic],
  );

  const [form, setForm] = useState(initial);
  useEffect(() => {
    if (isOpen) setForm(initial);
  }, [isOpen, initial]);

  const set = <K extends keyof typeof form>(key: K, value: (typeof form)[K]) =>
    setForm((f) => ({ ...f, [key]: value }));

  const invalidate = () => {
    void queryClient.invalidateQueries({ queryKey: ["administration", "diagnostics"] });
    // Live lookup refresh (Part B): the ICD catalog feeds the diagnostic
    // codes dialog (["diagnostics","by-category"/"search",…]) and the
    // problem dialog's dx search (["dx-icd-search",…]).
    void queryClient.invalidateQueries({ queryKey: ["diagnostics"] });
    void queryClient.invalidateQueries({ queryKey: ["dx-icd-search"] });
  };

  const createMutation = useMutation({
    mutationFn: (input: CreateDiagnosticInput) => createDiagnostic(input),
    onSuccess: () => {
      toast.success("Diagnostic created");
      invalidate();
      onClose();
    },
    onError: (err) => toast.error("Create failed", { description: describe(err) }),
  });

  const updateMutation = useMutation({
    mutationFn: (input: UpdateDiagnosticInput) => updateDiagnostic(input),
    onSuccess: () => {
      toast.success("Diagnostic updated");
      invalidate();
      onClose();
    },
    onError: (err) => toast.error("Update failed", { description: describe(err) }),
  });

  const isPending = createMutation.isPending || updateMutation.isPending;
  const trimmedCode = form.code.trim();

  const onSubmit = (e: FormEvent<HTMLFormElement>) => {
    e.preventDefault();
    if (!trimmedCode || !form.codeSourceId) return;
    const payload = {
      code: trimmedCode,
      description: form.description.trim() || null,
      longDescription: form.longDescription.trim() || null,
      codeSourceId: Number(form.codeSourceId),
      isChiropractic: form.isChiropractic,
      isBillable: form.isBillable,
    };
    if (state.mode === "edit" && diagnostic) {
      updateMutation.mutate({ diagnosticId: diagnostic.id, isActive: form.isActive, ...payload });
    } else {
      createMutation.mutate(payload);
    }
  };

  return (
    <Dialog open={isOpen} onOpenChange={(o) => (!o ? onClose() : undefined)}>
      <DialogContent className="!max-w-lg">
        <form onSubmit={onSubmit}>
          <DialogHeader>
            <DialogTitle>{diagnostic ? "Edit diagnostic" : "Add a diagnostic"}</DialogTitle>
            <DialogDescription>
              {diagnostic ? `Update details for ${diagnostic.code}.` : "Add an ICD diagnostic code."}
            </DialogDescription>
          </DialogHeader>

          <DialogBody className="space-y-5">
            <div className="grid gap-4 sm:grid-cols-[1fr_1fr]">
              <Field id="dx-code" label="Code" required>
                <Input
                  id="dx-code"
                  value={form.code}
                  onChange={(e) => set("code", e.target.value)}
                  placeholder="M54.5"
                  autoFocus
                  required
                  maxLength={16}
                />
              </Field>
              <Field id="dx-source" label="Source" required>
                <Combobox
                  id="dx-source"
                  label="Source"
                  value={form.codeSourceId}
                  onChange={(v) => set("codeSourceId", v ?? "")}
                  options={codeSourceOptions}
                  placeholder="Select…"
                  required
                />
              </Field>
            </div>

            <Field id="dx-description" label="Description">
              <Input
                id="dx-description"
                value={form.description}
                onChange={(e) => set("description", e.target.value)}
                placeholder="Low back pain"
                maxLength={512}
              />
            </Field>

            <Field id="dx-long" label="Long description">
              <textarea
                id="dx-long"
                value={form.longDescription}
                onChange={(e) => set("longDescription", e.target.value)}
                placeholder="Low back pain, unspecified…"
                rows={3}
                maxLength={1024}
                className={textareaClass}
              />
            </Field>

            <div className="flex items-center justify-between rounded-lg border border-[var(--color-border)] px-3 py-2.5">
              <p className="text-[13px] font-medium text-[var(--color-foreground)]">Billable</p>
              <Switch checked={form.isBillable} onCheckedChange={(v) => set("isBillable", v)} aria-label="Billable code" />
            </div>

            <div className="flex items-center justify-between rounded-lg border border-[var(--color-border)] px-3 py-2.5">
              <p className="text-[13px] font-medium text-[var(--color-foreground)]">Chiropractic</p>
              <Switch
                checked={form.isChiropractic}
                onCheckedChange={(v) => set("isChiropractic", v)}
                aria-label="Chiropractic diagnostic"
              />
            </div>

            {diagnostic && (
              <div className="flex items-center justify-between rounded-lg border border-[var(--color-border)] px-3 py-2.5">
                <div>
                  <p className="text-[13px] font-medium text-[var(--color-foreground)]">Active</p>
                  <p className="text-[12px] text-[var(--color-muted-foreground)]">
                    Inactive codes are hidden from selection lists.
                  </p>
                </div>
                <Switch checked={form.isActive} onCheckedChange={(v) => set("isActive", v)} aria-label="Diagnostic active" />
              </div>
            )}
          </DialogBody>

          <DialogFooter>
            <DialogClose asChild>
              <Button type="button" variant="outline" disabled={isPending}>
                Cancel
              </Button>
            </DialogClose>
            <Button type="submit" disabled={isPending || !trimmedCode || !form.codeSourceId}>
              {isPending ? "Saving…" : diagnostic ? "Save changes" : "Add diagnostic"}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}

function DeleteDiagnosticDialog({ state, onClose }: { state: EditorState; onClose: () => void }) {
  const isOpen = state.mode === "delete";
  const diagnostic = state.mode === "delete" ? state.diagnostic : undefined;
  const queryClient = useQueryClient();

  const deleteMutation = useMutation({
    mutationFn: (id: number) => deleteDiagnostic(id),
    onSuccess: () => {
      toast.success("Diagnostic deleted");
      queryClient.invalidateQueries({ queryKey: ["administration", "diagnostics"] });
      queryClient.invalidateQueries({ queryKey: ["diagnostics"] });
      queryClient.invalidateQueries({ queryKey: ["dx-icd-search"] });
      onClose();
    },
    onError: (err) => toast.error("Delete failed", { description: describe(err) }),
  });

  return (
    <Dialog open={isOpen} onOpenChange={(o) => (!o ? onClose() : undefined)}>
      <DialogContent>
        <DialogHeader>
          <DialogTitle className="text-[var(--color-destructive)]">Delete diagnostic</DialogTitle>
          <DialogDescription>
            This removes{" "}
            <span className="font-medium text-[var(--color-foreground)]">{diagnostic?.code}</span>. Records
            referencing it may need to be reassigned.
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
            onClick={() => diagnostic && deleteMutation.mutate(diagnostic.id)}
            disabled={deleteMutation.isPending || !diagnostic}
          >
            {deleteMutation.isPending ? "Deleting…" : "Delete diagnostic"}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
