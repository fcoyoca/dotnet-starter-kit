import { useEffect, useMemo, useState, type FormEvent } from "react";
import {
  keepPreviousData,
  useMutation,
  useQuery,
  useQueryClient,
} from "@tanstack/react-query";
import { ChevronRight, ListChecks, Pencil, Plus, Search, Trash2 } from "lucide-react";
import { toast } from "sonner";
import {
  createProcedureCode,
  deleteProcedureCode,
  listProcedureCodes,
  updateProcedureCode,
  useCodeSourceOptions,
  useProcedureCategoryOptions,
  type ProcedureCodeDto,
  type CreateProcedureCodeInput,
  type UpdateProcedureCodeInput,
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

const PAGE_SIZE = 20;

const textareaClass = cn(
  "flex w-full rounded-lg border border-[var(--color-input)] bg-transparent px-3 py-2 text-sm shadow-xs",
  "placeholder:text-[oklch(from_var(--color-muted-foreground)_l_c_h_/_0.6)]",
  "focus-visible:border-[var(--color-ring)] focus-visible:outline-none focus-visible:ring-[3px] focus-visible:ring-[oklch(from_var(--color-ring)_l_c_h_/_0.5)]",
);

type EditorState =
  | { mode: "closed" }
  | { mode: "create" }
  | { mode: "edit"; code: ProcedureCodeDto }
  | { mode: "delete"; code: ProcedureCodeDto };

export function ProcedureCodesPage() {
  const [categoryId, setCategoryId] = useState<string | null>(null);
  const [search, setSearch] = useState("");
  const [debouncedSearch, setDebouncedSearch] = useState("");
  const [pageNumber, setPageNumber] = useState(1);
  const [editor, setEditor] = useState<EditorState>({ mode: "closed" });

  const categoryOptions = useProcedureCategoryOptions();
  const categoryName = categoryOptions?.find((o) => o.value === categoryId)?.label ?? "";

  useEffect(() => {
    const t = setTimeout(() => {
      setDebouncedSearch(search.trim());
      setPageNumber(1);
    }, 250);
    return () => clearTimeout(t);
  }, [search]);

  // Reset paging/search when switching categories.
  useEffect(() => {
    setPageNumber(1);
    setSearch("");
    setDebouncedSearch("");
  }, [categoryId]);

  const query = useQuery({
    queryKey: ["administration", "procedure-codes", { categoryId, search: debouncedSearch, pageNumber, pageSize: PAGE_SIZE }],
    queryFn: () =>
      listProcedureCodes({
        procedureCategoryId: categoryId,
        search: debouncedSearch || undefined,
        pageNumber,
        pageSize: PAGE_SIZE,
        sortBy: "code",
        sortDir: "asc",
      }),
    enabled: !!categoryId,
    placeholderData: keepPreviousData,
  });

  const data = query.data;
  const items = data?.items ?? [];
  const searchActive = debouncedSearch.length > 0;

  return (
    <div className="space-y-4 sm:space-y-6">
      <EntityPageHeader
        icon={ListChecks}
        title="Procedure Codes"
        total={categoryId ? (data?.totalCount ?? null) : null}
        unit="code"
        description="Procedure (CPT/HCPCS) codes live within a procedure category. Pick a category to view and manage its codes."
      >
        <Button
          onClick={() => setEditor({ mode: "create" })}
          disabled={!categoryId}
          className="h-9 flex-1 gap-1.5 rounded-lg px-4 text-[13px] font-semibold sm:flex-none"
        >
          <Plus className="size-4" />
          New code
        </Button>
      </EntityPageHeader>

      <div className="max-w-md">
        <Field id="pc-category-select" label="Procedure category" required>
          <Combobox
            id="pc-category-select"
            label="Procedure category"
            variant="field"
            searchable
            emptyOptionLabel="Select a category…"
            placeholder="Select a category…"
            value={categoryId}
            onChange={setCategoryId}
            options={categoryOptions ?? []}
          />
        </Field>
      </div>

      {!categoryId ? (
        <EntityEmpty
          icon={ListChecks}
          title="Select a procedure category"
          body="Choose a procedure category above to view and add the codes that belong to it."
        />
      ) : (
        <>
          <EntitySearch value={search} onChange={setSearch} placeholder={`Search codes in ${categoryName}…`} />

          {query.isLoading && items.length === 0 ? (
            <EntityListLoading desktopColumns="grid-cols-[140px_1fr_130px_90px_24px]" />
          ) : items.length === 0 ? (
            <EntityEmpty
              icon={searchActive ? Search : ListChecks}
              title={searchActive ? "No procedure codes found" : `No codes in ${categoryName} yet`}
              body={
                searchActive
                  ? `Nothing matches "${debouncedSearch}". Try a different term or clear the search.`
                  : "Add the first procedure code to this category."
              }
              action={
                searchActive ? (
                  <Button variant="outline" onClick={() => setSearch("")} className="h-9 rounded-lg px-4 text-[13px]">
                    Clear search
                  </Button>
                ) : (
                  <Button onClick={() => setEditor({ mode: "create" })} className="h-9 rounded-lg px-4 text-[13px]">
                    <Plus className="mr-1.5 size-4" />
                    Add code
                  </Button>
                )
              }
            />
          ) : (
            <div>
              <div className="mb-3 flex items-center justify-between">
                <p className="text-[12px] font-medium text-[var(--color-muted-foreground)]">
                  {data?.totalCount ?? 0} code{(data?.totalCount ?? 0) !== 1 ? "s" : ""} in {categoryName}
                </p>
              </div>

              <div className="space-y-2 md:hidden">
                {items.map((c) => (
                  <MobileCard key={c.id} code={c} onEdit={() => setEditor({ mode: "edit", code: c })} />
                ))}
              </div>

              <EntityListCard className="hidden md:block">
                <EntityListHeader className="grid-cols-[140px_1fr_130px_90px_24px]">
                  <span>Code</span>
                  <span>Name</span>
                  <span>Source</span>
                  <span>Status</span>
                  <span />
                </EntityListHeader>
                {items.map((c, i) => (
                  <DesktopRow
                    key={c.id}
                    code={c}
                    isLast={i === items.length - 1}
                    onEdit={() => setEditor({ mode: "edit", code: c })}
                    onDelete={() => setEditor({ mode: "delete", code: c })}
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
        </>
      )}

      {query.isError && (
        <div
          role="alert"
          className="rounded-lg border border-[oklch(from_var(--color-destructive)_l_c_h_/_0.30)] bg-[oklch(from_var(--color-destructive)_l_c_h_/_0.06)] px-3 py-2 text-sm text-[var(--color-destructive)]"
        >
          {describe(query.error)}
        </div>
      )}

      {categoryId && (
        <>
          <ProcedureCodeEditorDialog
            state={editor}
            categoryId={categoryId}
            categoryName={categoryName}
            onClose={() => setEditor({ mode: "closed" })}
          />
          <DeleteProcedureCodeDialog state={editor} onClose={() => setEditor({ mode: "closed" })} />
        </>
      )}
    </div>
  );
}

function MobileCard({ code, onEdit }: { code: ProcedureCodeDto; onEdit: () => void }) {
  return (
    <EntityMobileCard
      href="#"
      onClick={(e) => {
        e.preventDefault();
        onEdit();
      }}
      aria-label={`Edit procedure code ${code.code}`}
    >
      <div className="flex items-center justify-between">
        <div className="flex min-w-0 items-center gap-3">
          <EntityInitialsAvatar name={code.code} size={40} />
          <div className="min-w-0">
            <p className="truncate text-[14px] font-medium text-[var(--color-foreground)]">{code.code}</p>
            {code.name && (
              <p className="mt-0.5 truncate text-[12px] text-[var(--color-muted-foreground)]">{code.name}</p>
            )}
          </div>
        </div>
        <EntityStatusBadge tone={code.isActive ? "success" : "default"}>
          {code.isActive ? "Active" : "Inactive"}
        </EntityStatusBadge>
      </div>
    </EntityMobileCard>
  );
}

function DesktopRow({
  code,
  isLast,
  onEdit,
  onDelete,
}: {
  code: ProcedureCodeDto;
  isLast: boolean;
  onEdit: () => void;
  onDelete: () => void;
}) {
  return (
    <EntityListRow className="grid-cols-[140px_1fr_130px_90px_24px]" isLast={isLast}>
      <div className="min-w-0 truncate text-[14px] font-medium text-[var(--color-foreground)] transition-colors group-hover:text-[var(--color-primary)]">
        {code.code}
      </div>
      <div className="min-w-0 truncate text-[12px] text-[var(--color-muted-foreground)]">
        {code.name ?? code.description ?? "—"}
      </div>
      <div className="min-w-0 truncate text-[12px] text-[var(--color-muted-foreground)]">{code.codeSourceName ?? "—"}</div>
      <div className="flex items-center">
        <EntityStatusBadge tone={code.isActive ? "success" : "default"}>
          {code.isActive ? "Active" : "Inactive"}
        </EntityStatusBadge>
      </div>
      <div className="flex items-center justify-end gap-1">
        <button
          type="button"
          aria-label={`Edit ${code.code}`}
          onClick={onEdit}
          className="grid size-7 cursor-pointer place-items-center rounded-md text-[var(--color-muted-foreground)] opacity-0 transition-all hover:bg-[var(--color-muted)] hover:text-[var(--color-foreground)] group-hover:opacity-100"
        >
          <Pencil className="size-3.5" />
        </button>
        <button
          type="button"
          aria-label={`Delete ${code.code}`}
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

function ProcedureCodeEditorDialog({
  state,
  categoryId,
  categoryName,
  onClose,
}: {
  state: EditorState;
  categoryId: string;
  categoryName: string;
  onClose: () => void;
}) {
  const isOpen = state.mode === "create" || state.mode === "edit";
  const code = state.mode === "edit" ? state.code : undefined;
  const queryClient = useQueryClient();

  const initial = useMemo(
    () => ({
      code: code?.code ?? "",
      name: code?.name ?? "",
      description: code?.description ?? "",
      codeSourceId: code?.codeSourceId ?? null,
      macroText: code?.macroText ?? "",
      isActive: code?.isActive ?? true,
    }),
    [code],
  );

  const codeSourceOptions = useCodeSourceOptions() ?? [];
  const [form, setForm] = useState(initial);
  useEffect(() => {
    if (isOpen) setForm(initial);
  }, [isOpen, initial]);

  const set = <K extends keyof typeof form>(key: K, value: (typeof form)[K]) =>
    setForm((f) => ({ ...f, [key]: value }));

  const invalidate = () => {
    void queryClient.invalidateQueries({ queryKey: ["administration", "procedure-codes"] });
    // Live lookup refresh (Part B): procedure codes feed the SuperBill's
    // code picker (["procedure-codes","picker",…]).
    void queryClient.invalidateQueries({ queryKey: ["procedure-codes"] });
  };

  const createMutation = useMutation({
    mutationFn: (input: CreateProcedureCodeInput) => createProcedureCode(input),
    onSuccess: () => {
      toast.success("Procedure code created");
      invalidate();
      onClose();
    },
    onError: (err) => toast.error("Create failed", { description: describe(err) }),
  });

  const updateMutation = useMutation({
    mutationFn: (input: UpdateProcedureCodeInput) => updateProcedureCode(input),
    onSuccess: () => {
      toast.success("Procedure code updated");
      invalidate();
      onClose();
    },
    onError: (err) => toast.error("Update failed", { description: describe(err) }),
  });

  const isPending = createMutation.isPending || updateMutation.isPending;
  const trimmedCode = form.code.trim();

  const onSubmit = (e: FormEvent<HTMLFormElement>) => {
    e.preventDefault();
    if (!trimmedCode) return;
    const payload = {
      code: trimmedCode,
      procedureCategoryId: code?.procedureCategoryId ?? categoryId,
      name: form.name.trim() || null,
      description: form.description.trim() || null,
      codeSourceId: form.codeSourceId,
      macroText: form.macroText.trim() || null,
    };
    if (state.mode === "edit" && code) {
      updateMutation.mutate({ codeId: code.id, isActive: form.isActive, ...payload });
    } else {
      createMutation.mutate(payload);
    }
  };

  return (
    <Dialog open={isOpen} onOpenChange={(o) => (!o ? onClose() : undefined)}>
      <DialogContent className="!max-w-lg">
        <form onSubmit={onSubmit}>
          <DialogHeader>
            <DialogTitle>{code ? "Edit procedure code" : "Add a procedure code"}</DialogTitle>
            <DialogDescription>
              {code ? `Update details for ${code.code}.` : `Add a procedure code to ${categoryName}.`}
            </DialogDescription>
          </DialogHeader>

          <DialogBody className="space-y-5">
            <div className="grid grid-cols-[160px_1fr] gap-4">
              <Field id="pc-code" label="Code" required>
                <Input
                  id="pc-code"
                  value={form.code}
                  onChange={(e) => set("code", e.target.value)}
                  placeholder="99213"
                  autoFocus
                  required
                  maxLength={50}
                />
              </Field>
              <Field id="pc-name" label="Name">
                <Input
                  id="pc-name"
                  value={form.name}
                  onChange={(e) => set("name", e.target.value)}
                  placeholder="Office visit"
                  maxLength={200}
                />
              </Field>
            </div>

            <Field id="pc-description" label="Description">
              <Input
                id="pc-description"
                value={form.description}
                onChange={(e) => set("description", e.target.value)}
                placeholder="Established patient, low complexity"
                maxLength={1000}
              />
            </Field>

            <Field id="pc-source" label="Code source" hint="The code system, e.g. CPT or HCPCS.">
              <Combobox
                id="pc-source"
                label="Code source"
                variant="field"
                searchable
                clearable
                emptyOptionLabel="No code source"
                placeholder="Select a code source…"
                value={form.codeSourceId != null ? String(form.codeSourceId) : null}
                onChange={(v) => set("codeSourceId", v ? Number(v) : null)}
                options={codeSourceOptions}
              />
            </Field>

            <Field id="pc-macro" label="Macro text" hint="Boilerplate inserted when this code is selected.">
              <textarea
                id="pc-macro"
                value={form.macroText}
                onChange={(e) => set("macroText", e.target.value)}
                placeholder="Patient seen for…"
                rows={4}
                maxLength={4000}
                className={textareaClass}
              />
            </Field>

            {code && (
              <div className="flex items-center justify-between rounded-lg border border-[var(--color-border)] px-3 py-2.5">
                <div>
                  <p className="text-[13px] font-medium text-[var(--color-foreground)]">Active</p>
                  <p className="text-[12px] text-[var(--color-muted-foreground)]">
                    Inactive codes are hidden from billing selection lists.
                  </p>
                </div>
                <Switch checked={form.isActive} onCheckedChange={(v) => set("isActive", v)} aria-label="Procedure code active" />
              </div>
            )}
          </DialogBody>

          <DialogFooter>
            <DialogClose asChild>
              <Button type="button" variant="outline" disabled={isPending}>
                Cancel
              </Button>
            </DialogClose>
            <Button type="submit" disabled={isPending || !trimmedCode}>
              {isPending ? "Saving…" : code ? "Save changes" : "Add code"}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}

function DeleteProcedureCodeDialog({ state, onClose }: { state: EditorState; onClose: () => void }) {
  const isOpen = state.mode === "delete";
  const code = state.mode === "delete" ? state.code : undefined;
  const queryClient = useQueryClient();

  const deleteMutation = useMutation({
    mutationFn: (id: string) => deleteProcedureCode(id),
    onSuccess: () => {
      toast.success("Procedure code deleted");
      queryClient.invalidateQueries({ queryKey: ["administration", "procedure-codes"] });
      queryClient.invalidateQueries({ queryKey: ["procedure-codes"] });
      onClose();
    },
    onError: (err) => toast.error("Delete failed", { description: describe(err) }),
  });

  return (
    <Dialog open={isOpen} onOpenChange={(o) => (!o ? onClose() : undefined)}>
      <DialogContent>
        <DialogHeader>
          <DialogTitle className="text-[var(--color-destructive)]">Delete procedure code</DialogTitle>
          <DialogDescription>
            This removes{" "}
            <span className="font-medium text-[var(--color-foreground)]">{code?.code}</span>. Records referencing
            it may need to be reassigned.
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
            onClick={() => code && deleteMutation.mutate(code.id)}
            disabled={deleteMutation.isPending || !code}
          >
            {deleteMutation.isPending ? "Deleting…" : "Delete code"}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
