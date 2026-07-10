import { useEffect, useMemo, useState, type FormEvent } from "react";
import {
  keepPreviousData,
  useMutation,
  useQuery,
  useQueryClient,
} from "@tanstack/react-query";
import { ChevronRight, FolderTree, ListChecks, Pencil, Plus, Save, Search, Trash2, X } from "lucide-react";
import { toast } from "sonner";
import {
  createDiagnosticCategory,
  deleteDiagnosticCategory,
  listDiagnosticCategories,
  listDiagnosticCategoryCodes,
  listDiagnostics,
  setDiagnosticCategoryCodes,
  updateDiagnosticCategory,
  type DiagnosticCategoryDto,
  type CreateDiagnosticCategoryInput,
  type DiagnosticDto,
  type UpdateDiagnosticCategoryInput,
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

const PAGE_SIZE = 20;

type EditorState =
  | { mode: "closed" }
  | { mode: "create" }
  | { mode: "edit"; category: DiagnosticCategoryDto }
  | { mode: "delete"; category: DiagnosticCategoryDto }
  | { mode: "codes"; category: DiagnosticCategoryDto };

export function DiagnosticCategoriesPage() {
  const [search, setSearch] = useState("");
  const [debouncedSearch, setDebouncedSearch] = useState("");
  const [pageNumber, setPageNumber] = useState(1);
  const [editor, setEditor] = useState<EditorState>({ mode: "closed" });

  useEffect(() => {
    const t = setTimeout(() => {
      setDebouncedSearch(search.trim());
      setPageNumber(1);
    }, 250);
    return () => clearTimeout(t);
  }, [search]);

  const query = useQuery({
    queryKey: ["administration", "diagnostic-categories", { search: debouncedSearch, pageNumber, pageSize: PAGE_SIZE }],
    queryFn: () => listDiagnosticCategories({ search: debouncedSearch || undefined, pageNumber, pageSize: PAGE_SIZE }),
    placeholderData: keepPreviousData,
  });

  const data = query.data;
  const items = data?.items ?? [];
  const searchActive = debouncedSearch.length > 0;

  return (
    <div className="space-y-4 sm:space-y-6">
      <EntityPageHeader
        icon={FolderTree}
        title="Diagnostic Categories"
        total={data?.totalCount ?? null}
        unit="category"
        description="Groupings used to organize diagnostic (ICD) codes for quick selection."
      >
        <Button
          onClick={() => setEditor({ mode: "create" })}
          className="h-9 flex-1 gap-1.5 rounded-lg px-4 text-[13px] font-semibold sm:flex-none"
        >
          <Plus className="size-4" />
          New category
        </Button>
      </EntityPageHeader>

      <EntitySearch value={search} onChange={setSearch} placeholder="Search by name…" />

      {query.isLoading && items.length === 0 ? (
        <EntityListLoading desktopColumns="grid-cols-[1fr_90px_24px]" />
      ) : items.length === 0 ? (
        <EntityEmpty
          icon={searchActive ? Search : FolderTree}
          title={searchActive ? "No diagnostic categories found" : "No diagnostic categories yet"}
          body={
            searchActive
              ? `Nothing matches "${debouncedSearch}". Try a different term or clear the search.`
              : "Add your first category to start grouping diagnostic codes."
          }
          action={
            searchActive ? (
              <Button variant="outline" onClick={() => setSearch("")} className="h-9 rounded-lg px-4 text-[13px]">
                Clear search
              </Button>
            ) : (
              <Button onClick={() => setEditor({ mode: "create" })} className="h-9 rounded-lg px-4 text-[13px]">
                <Plus className="mr-1.5 size-4" />
                Add category
              </Button>
            )
          }
        />
      ) : (
        <div>
          <div className="mb-3 flex items-center justify-between">
            <p className="text-[12px] font-medium text-[var(--color-muted-foreground)]">
              {data?.totalCount ?? 0} categor{(data?.totalCount ?? 0) !== 1 ? "ies" : "y"} found
            </p>
          </div>

          <div className="space-y-2 md:hidden">
            {items.map((c) => (
              <MobileCard
                key={c.id}
                category={c}
                onEdit={() => setEditor({ mode: "edit", category: c })}
                onManageCodes={() => setEditor({ mode: "codes", category: c })}
              />
            ))}
          </div>

          <EntityListCard className="hidden md:block">
            <EntityListHeader className="grid-cols-[1fr_90px_24px]">
              <span>Category</span>
              <span>Status</span>
              <span />
            </EntityListHeader>
            {items.map((c, i) => (
              <DesktopRow
                key={c.id}
                category={c}
                isLast={i === items.length - 1}
                onEdit={() => setEditor({ mode: "edit", category: c })}
                onDelete={() => setEditor({ mode: "delete", category: c })}
                onManageCodes={() => setEditor({ mode: "codes", category: c })}
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

      <DiagnosticCategoryEditorDialog state={editor} onClose={() => setEditor({ mode: "closed" })} />
      <DeleteDiagnosticCategoryDialog state={editor} onClose={() => setEditor({ mode: "closed" })} />
      <DiagnosticCategoryCodesDialog state={editor} onClose={() => setEditor({ mode: "closed" })} />
    </div>
  );
}

function MobileCard({
  category,
  onEdit,
  onManageCodes,
}: {
  category: DiagnosticCategoryDto;
  onEdit: () => void;
  onManageCodes: () => void;
}) {
  return (
    <EntityMobileCard
      href="#"
      onClick={(e) => {
        e.preventDefault();
        onEdit();
      }}
      aria-label={`Edit diagnostic category ${category.name}`}
    >
      <div className="flex items-center justify-between">
        <div className="flex min-w-0 items-center gap-3">
          <EntityInitialsAvatar name={category.name} size={40} />
          <p className="truncate text-[14px] font-medium text-[var(--color-foreground)]">{category.name}</p>
        </div>
        <div className="flex items-center gap-2">
          <EntityStatusBadge tone={category.isActive ? "success" : "default"}>
            {category.isActive ? "Active" : "Inactive"}
          </EntityStatusBadge>
          <button
            type="button"
            aria-label={`Manage codes for ${category.name}`}
            onClick={(e) => {
              e.preventDefault();
              e.stopPropagation();
              onManageCodes();
            }}
            className="grid size-7 shrink-0 cursor-pointer place-items-center rounded-md text-[var(--color-muted-foreground)] hover:bg-[var(--color-muted)] hover:text-[var(--color-foreground)]"
          >
            <ListChecks className="size-3.5" />
          </button>
        </div>
      </div>
    </EntityMobileCard>
  );
}

function DesktopRow({
  category,
  isLast,
  onEdit,
  onDelete,
  onManageCodes,
}: {
  category: DiagnosticCategoryDto;
  isLast: boolean;
  onEdit: () => void;
  onDelete: () => void;
  onManageCodes: () => void;
}) {
  return (
    <EntityListRow className="grid-cols-[1fr_90px_24px]" isLast={isLast}>
      <div className="flex min-w-0 items-center gap-3">
        <EntityInitialsAvatar name={category.name} size={36} />
        <div className="truncate text-[14px] font-medium text-[var(--color-foreground)] transition-colors group-hover:text-[var(--color-primary)]">
          {category.name}
        </div>
      </div>
      <div className="flex items-center">
        <EntityStatusBadge tone={category.isActive ? "success" : "default"}>
          {category.isActive ? "Active" : "Inactive"}
        </EntityStatusBadge>
      </div>
      <div className="flex items-center justify-end gap-1">
        <button
          type="button"
          aria-label={`Manage codes for ${category.name}`}
          onClick={onManageCodes}
          className="grid size-7 cursor-pointer place-items-center rounded-md text-[var(--color-muted-foreground)] opacity-0 transition-all hover:bg-[var(--color-muted)] hover:text-[var(--color-foreground)] group-hover:opacity-100"
        >
          <ListChecks className="size-3.5" />
        </button>
        <button
          type="button"
          aria-label={`Edit ${category.name}`}
          onClick={onEdit}
          className="grid size-7 cursor-pointer place-items-center rounded-md text-[var(--color-muted-foreground)] opacity-0 transition-all hover:bg-[var(--color-muted)] hover:text-[var(--color-foreground)] group-hover:opacity-100"
        >
          <Pencil className="size-3.5" />
        </button>
        <button
          type="button"
          aria-label={`Delete ${category.name}`}
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

function DiagnosticCategoryEditorDialog({ state, onClose }: { state: EditorState; onClose: () => void }) {
  const isOpen = state.mode === "create" || state.mode === "edit";
  const category = state.mode === "edit" ? state.category : undefined;
  const queryClient = useQueryClient();

  const initial = useMemo(
    () => ({ name: category?.name ?? "", isActive: category?.isActive ?? true }),
    [category],
  );

  const [form, setForm] = useState(initial);
  useEffect(() => {
    if (isOpen) setForm(initial);
  }, [isOpen, initial]);

  const invalidate = () => {
    void queryClient.invalidateQueries({ queryKey: ["administration", "diagnostic-categories"] });
    // Live lookup refresh (Part B): category names feed the diagnostic
    // codes dialog's category dropdown (["diagnostic-categories","dx-dialog"]).
    void queryClient.invalidateQueries({ queryKey: ["diagnostic-categories"] });
  };

  const createMutation = useMutation({
    mutationFn: (input: CreateDiagnosticCategoryInput) => createDiagnosticCategory(input),
    onSuccess: () => {
      toast.success("Diagnostic category created");
      invalidate();
      onClose();
    },
    onError: (err) => toast.error("Create failed", { description: describe(err) }),
  });

  const updateMutation = useMutation({
    mutationFn: (input: UpdateDiagnosticCategoryInput) => updateDiagnosticCategory(input),
    onSuccess: () => {
      toast.success("Diagnostic category updated");
      invalidate();
      onClose();
    },
    onError: (err) => toast.error("Update failed", { description: describe(err) }),
  });

  const isPending = createMutation.isPending || updateMutation.isPending;
  const trimmedName = form.name.trim();

  const onSubmit = (e: FormEvent<HTMLFormElement>) => {
    e.preventDefault();
    if (!trimmedName) return;
    if (state.mode === "edit" && category) {
      updateMutation.mutate({ categoryId: category.id, name: trimmedName, isActive: form.isActive });
    } else {
      createMutation.mutate({ name: trimmedName });
    }
  };

  return (
    <Dialog open={isOpen} onOpenChange={(o) => (!o ? onClose() : undefined)}>
      <DialogContent className="!max-w-md">
        <form onSubmit={onSubmit}>
          <DialogHeader>
            <DialogTitle>{category ? "Edit diagnostic category" : "Add a diagnostic category"}</DialogTitle>
            <DialogDescription>
              {category ? `Update details for ${category.name}.` : "Add a diagnostic category to your organization."}
            </DialogDescription>
          </DialogHeader>

          <DialogBody className="space-y-5">
            <Field id="dx-cat-name" label="Name" required>
              <Input
                id="dx-cat-name"
                value={form.name}
                onChange={(e) => setForm((f) => ({ ...f, name: e.target.value }))}
                placeholder="Cardiology"
                autoFocus
                required
                maxLength={200}
              />
            </Field>

            {category && (
              <div className="flex items-center justify-between rounded-lg border border-[var(--color-border)] px-3 py-2.5">
                <div>
                  <p className="text-[13px] font-medium text-[var(--color-foreground)]">Active</p>
                  <p className="text-[12px] text-[var(--color-muted-foreground)]">
                    Inactive categories are hidden from selection lists.
                  </p>
                </div>
                <Switch
                  checked={form.isActive}
                  onCheckedChange={(v) => setForm((f) => ({ ...f, isActive: v }))}
                  aria-label="Diagnostic category active"
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
              {isPending ? "Saving…" : category ? "Save changes" : "Add category"}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}

function DeleteDiagnosticCategoryDialog({ state, onClose }: { state: EditorState; onClose: () => void }) {
  const isOpen = state.mode === "delete";
  const category = state.mode === "delete" ? state.category : undefined;
  const queryClient = useQueryClient();

  const deleteMutation = useMutation({
    mutationFn: (id: string) => deleteDiagnosticCategory(id),
    onSuccess: () => {
      toast.success("Diagnostic category deleted");
      queryClient.invalidateQueries({ queryKey: ["administration", "diagnostic-categories"] });
      queryClient.invalidateQueries({ queryKey: ["diagnostic-categories"] });
      onClose();
    },
    onError: (err) => toast.error("Delete failed", { description: describe(err) }),
  });

  return (
    <Dialog open={isOpen} onOpenChange={(o) => (!o ? onClose() : undefined)}>
      <DialogContent>
        <DialogHeader>
          <DialogTitle className="text-[var(--color-destructive)]">Delete diagnostic category</DialogTitle>
          <DialogDescription>
            This removes{" "}
            <span className="font-medium text-[var(--color-foreground)]">{category?.name}</span>{" "}
            <span className="opacity-70">(created {category && formatDate(category.createdAtUtc)})</span>.
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
            onClick={() => category && deleteMutation.mutate(category.id)}
            disabled={deleteMutation.isPending || !category}
          >
            {deleteMutation.isPending ? "Deleting…" : "Delete category"}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}

/** One row of an associated (or about-to-be-associated) diagnostic code in the category-codes editor. */
type CategoryCodeRow = { diagnosticId: number; code: string; description: string | null };

/**
 * Category ↔ code association editor — legacy `ascDiagnosticCategories`. Left panel searches the
 * global diagnostic (ICD) catalog and adds rows; right panel is the working association set with
 * per-row remove. Nothing persists until Save, which replaces the category's full code set.
 */
function DiagnosticCategoryCodesDialog({ state, onClose }: { state: EditorState; onClose: () => void }) {
  const isOpen = state.mode === "codes";
  const category = state.mode === "codes" ? state.category : undefined;
  const queryClient = useQueryClient();

  const [rows, setRows] = useState<CategoryCodeRow[]>([]);
  // The category id `rows` was last hydrated for — comparing this to the current category (rather
  // than a plain boolean) keeps the skeleton showing across a category switch even on the render
  // that happens before the effect below has a chance to run, so the previous category's rows can
  // never flash for a frame.
  const [hydratedFor, setHydratedFor] = useState<string | null>(null);
  const [searchInput, setSearchInput] = useState("");
  const [committedSearch, setCommittedSearch] = useState("");

  const hydrated = !!category && hydratedFor === category.id;

  // One-shot fetch (not a live useQuery) so a background refetch can never clobber the user's
  // in-progress edits; a reopen always reflects the latest saved set (AssociatedProcedureCodes precedent).
  useEffect(() => {
    if (!isOpen || !category) return;
    setRows([]);
    setSearchInput("");
    setCommittedSearch("");
    let cancelled = false;
    queryClient
      .fetchQuery({
        queryKey: ["administration", "diagnostic-category-codes", category.id],
        queryFn: () => listDiagnosticCategoryCodes(category.id),
        staleTime: 0,
      })
      .then((data) => {
        if (cancelled) return;
        setRows(
          data.map((d) => ({
            diagnosticId: d.diagnosticId,
            code: d.code,
            description: d.description ?? null,
          })),
        );
      })
      .catch((err) => {
        if (!cancelled) toast.error("Could not load associated codes", { description: describe(err) });
      })
      .finally(() => {
        if (!cancelled) setHydratedFor(category.id);
      });
    return () => {
      cancelled = true;
    };
  }, [isOpen, category, queryClient]);

  const searchQuery = useQuery({
    queryKey: ["administration", "diagnostics", "search", committedSearch],
    queryFn: () => listDiagnostics({ search: committedSearch, pageSize: 50 }),
    enabled: isOpen && committedSearch.length >= 2,
  });
  const results = searchQuery.data?.items ?? [];

  const runSearch = () => {
    const trimmed = searchInput.trim();
    if (trimmed.length < 2) {
      toast.warning("Please enter at least 2 characters.");
      return;
    }
    setCommittedSearch(trimmed);
  };

  const addRow = (d: DiagnosticDto) => {
    if (rows.some((r) => r.diagnosticId === d.id)) {
      toast.warning("Code already associated.");
      return;
    }
    setRows((prev) => [...prev, { diagnosticId: d.id, code: d.code, description: d.description ?? null }]);
  };

  const removeRow = (diagnosticId: number) =>
    setRows((prev) => prev.filter((r) => r.diagnosticId !== diagnosticId));

  const saveMutation = useMutation({
    mutationFn: () =>
      setDiagnosticCategoryCodes({
        categoryId: category!.id,
        diagnosticIds: rows.map((r) => r.diagnosticId),
      }),
    onSuccess: () => {
      toast.success("Category codes saved.");
      void queryClient.invalidateQueries({
        queryKey: ["administration", "diagnostic-category-codes", category!.id],
      });
      void queryClient.invalidateQueries({ queryKey: ["diagnostics", "by-category", category!.id] });
      onClose();
    },
    onError: (err) => toast.error("Save failed", { description: describe(err) }),
  });

  return (
    <Dialog open={isOpen} onOpenChange={(o) => (!o ? onClose() : undefined)}>
      <DialogContent className="!max-w-3xl">
        <DialogHeader>
          <DialogTitle>{category ? `Codes — ${category.name}` : "Codes"}</DialogTitle>
          <DialogDescription>Associate diagnostic (ICD) codes with this category.</DialogDescription>
        </DialogHeader>

        <DialogBody className="space-y-4">
          {!hydrated ? (
            <div className="skeleton h-24 rounded-lg" />
          ) : (
            <div className="grid gap-4 sm:grid-cols-2">
              <div className="space-y-1.5">
                <p className="text-[13px] font-semibold">Search codes</p>
                <div className="flex gap-2">
                  <Input
                    value={searchInput}
                    onChange={(e) => setSearchInput(e.target.value)}
                    onKeyDown={(e) => {
                      if (e.key === "Enter") {
                        e.preventDefault();
                        runSearch();
                      }
                    }}
                    placeholder="Code or description…"
                  />
                  <Button type="button" variant="outline" size="sm" onClick={runSearch}>
                    <Search className="size-4" />
                  </Button>
                </div>
                {searchQuery.isLoading ? (
                  <div className="skeleton h-40 rounded-lg" />
                ) : (
                  <div className="max-h-64 overflow-auto rounded-lg border border-[var(--color-border)]">
                    <table className="w-full text-[13px]">
                      <thead className="sticky top-0 bg-[var(--color-card)]">
                        <tr className="border-b border-[var(--color-border)] text-left text-[11px] uppercase tracking-wide text-[var(--color-muted-foreground)]">
                          <th className="px-3 py-2 w-24">Code</th>
                          <th className="px-3 py-2">Description</th>
                          <th className="px-3 py-2 w-10" />
                        </tr>
                      </thead>
                      <tbody>
                        {results.length === 0 ? (
                          <tr>
                            <td
                              colSpan={3}
                              className="px-3 py-6 text-center text-[13px] text-[var(--color-muted-foreground)]"
                            >
                              {committedSearch.length >= 2 ? "No matches." : "Search for codes to add."}
                            </td>
                          </tr>
                        ) : (
                          results.map((d) => (
                            <tr
                              key={d.id}
                              className="border-b border-[var(--color-border)] last:border-b-0 hover:bg-[var(--color-accent)]"
                            >
                              <td className="px-3 py-2 font-medium">{d.code}</td>
                              <td className="px-3 py-2">{d.description ?? "—"}</td>
                              <td className="px-3 py-2">
                                <button
                                  type="button"
                                  title={`Add ${d.code}`}
                                  aria-label={`Add ${d.code}`}
                                  onClick={() => addRow(d)}
                                  className="inline-flex size-7 items-center justify-center rounded-md border border-[var(--color-border)] transition-colors hover:bg-[var(--color-card)]"
                                >
                                  <Plus className="size-3.5" />
                                </button>
                              </td>
                            </tr>
                          ))
                        )}
                      </tbody>
                    </table>
                  </div>
                )}
              </div>

              <div className="space-y-1.5">
                <p className="text-[13px] font-semibold">
                  Associated codes{rows.length > 0 ? ` (${rows.length})` : ""}
                </p>
                {rows.length === 0 ? (
                  <p className="rounded-lg border border-[var(--color-border)] px-3 py-4 text-center text-[13px] text-[var(--color-muted-foreground)]">
                    No codes associated yet.
                  </p>
                ) : (
                  <div className="max-h-64 overflow-auto rounded-lg border border-[var(--color-border)]">
                    <table className="w-full text-[13px]">
                      <thead className="sticky top-0 bg-[var(--color-card)]">
                        <tr className="border-b border-[var(--color-border)] text-left text-[11px] uppercase tracking-wide text-[var(--color-muted-foreground)]">
                          <th className="px-3 py-2 w-24">Code</th>
                          <th className="px-3 py-2">Description</th>
                          <th className="px-3 py-2 w-10" />
                        </tr>
                      </thead>
                      <tbody>
                        {rows.map((r) => (
                          <tr key={r.diagnosticId} className="border-b border-[var(--color-border)] last:border-b-0">
                            <td className="px-3 py-2 font-medium">{r.code}</td>
                            <td className="px-3 py-2">{r.description ?? "—"}</td>
                            <td className="px-3 py-2">
                              <button
                                type="button"
                                title="Remove"
                                aria-label={`Remove ${r.code}`}
                                onClick={() => removeRow(r.diagnosticId)}
                                className="inline-flex size-7 items-center justify-center rounded-md border border-[var(--color-border)] transition-colors hover:bg-[var(--color-accent)]"
                              >
                                <X className="size-3.5" />
                              </button>
                            </td>
                          </tr>
                        ))}
                      </tbody>
                    </table>
                  </div>
                )}
              </div>
            </div>
          )}
        </DialogBody>

        <DialogFooter>
          <Button type="button" onClick={() => saveMutation.mutate()} disabled={saveMutation.isPending || !hydrated}>
            <Save className="size-4" />
            {saveMutation.isPending ? "Saving…" : "Save"}
          </Button>
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
