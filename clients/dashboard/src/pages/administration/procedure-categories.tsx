import { useEffect, useMemo, useState, type FormEvent } from "react";
import {
  keepPreviousData,
  useMutation,
  useQuery,
  useQueryClient,
} from "@tanstack/react-query";
import { ChevronRight, Layers, Pencil, Plus, Search, Trash2 } from "lucide-react";
import { toast } from "sonner";
import {
  createProcedureCategory,
  deleteProcedureCategory,
  listProcedureCategories,
  updateProcedureCategory,
  type ProcedureCategoryDto,
  type CreateProcedureCategoryInput,
  type UpdateProcedureCategoryInput,
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
import { describe } from "@/lib/list-helpers";

const PAGE_SIZE = 20;

type EditorState =
  | { mode: "closed" }
  | { mode: "create" }
  | { mode: "edit"; category: ProcedureCategoryDto }
  | { mode: "delete"; category: ProcedureCategoryDto };

export function ProcedureCategoriesPage() {
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
    queryKey: ["administration", "procedure-categories", { search: debouncedSearch, pageNumber, pageSize: PAGE_SIZE }],
    queryFn: () => listProcedureCategories({ search: debouncedSearch || undefined, pageNumber, pageSize: PAGE_SIZE }),
    placeholderData: keepPreviousData,
  });

  const data = query.data;
  const items = data?.items ?? [];
  const searchActive = debouncedSearch.length > 0;

  return (
    <div className="space-y-4 sm:space-y-6">
      <EntityPageHeader
        icon={Layers}
        title="Procedure Categories"
        total={data?.totalCount ?? null}
        unit="category"
        description="Groupings used to organize procedure (CPT) codes."
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
        <EntityListLoading desktopColumns="grid-cols-[1fr_90px_90px_24px]" />
      ) : items.length === 0 ? (
        <EntityEmpty
          icon={searchActive ? Search : Layers}
          title={searchActive ? "No procedure categories found" : "No procedure categories yet"}
          body={
            searchActive
              ? `Nothing matches "${debouncedSearch}". Try a different term or clear the search.`
              : "Add your first category to start grouping procedure codes."
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
              <MobileCard key={c.id} category={c} onEdit={() => setEditor({ mode: "edit", category: c })} />
            ))}
          </div>

          <EntityListCard className="hidden md:block">
            <EntityListHeader className="grid-cols-[1fr_90px_90px_24px]">
              <span>Category</span>
              <span>Imaging</span>
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

      <ProcedureCategoryEditorDialog state={editor} onClose={() => setEditor({ mode: "closed" })} />
      <DeleteProcedureCategoryDialog state={editor} onClose={() => setEditor({ mode: "closed" })} />
    </div>
  );
}

function MobileCard({ category, onEdit }: { category: ProcedureCategoryDto; onEdit: () => void }) {
  return (
    <EntityMobileCard
      href="#"
      onClick={(e) => {
        e.preventDefault();
        onEdit();
      }}
      aria-label={`Edit procedure category ${category.name}`}
    >
      <div className="flex items-center justify-between">
        <div className="flex min-w-0 items-center gap-3">
          <EntityInitialsAvatar name={category.name} size={40} />
          <div className="min-w-0">
            <p className="truncate text-[14px] font-medium text-[var(--color-foreground)]">{category.name}</p>
            {category.description && (
              <p className="mt-0.5 truncate text-[12px] text-[var(--color-muted-foreground)]">{category.description}</p>
            )}
          </div>
        </div>
        <EntityStatusBadge tone={category.isActive ? "success" : "default"}>
          {category.isActive ? "Active" : "Inactive"}
        </EntityStatusBadge>
      </div>
    </EntityMobileCard>
  );
}

function DesktopRow({
  category,
  isLast,
  onEdit,
  onDelete,
}: {
  category: ProcedureCategoryDto;
  isLast: boolean;
  onEdit: () => void;
  onDelete: () => void;
}) {
  return (
    <EntityListRow className="grid-cols-[1fr_90px_90px_24px]" isLast={isLast}>
      <div className="flex min-w-0 items-center gap-3">
        <EntityInitialsAvatar name={category.name} size={36} />
        <div className="truncate text-[14px] font-medium text-[var(--color-foreground)] transition-colors group-hover:text-[var(--color-primary)]">
          {category.name}
        </div>
      </div>
      <div className="flex items-center text-[12px] text-[var(--color-muted-foreground)]">
        {category.isImaging ? "Yes" : "No"}
      </div>
      <div className="flex items-center">
        <EntityStatusBadge tone={category.isActive ? "success" : "default"}>
          {category.isActive ? "Active" : "Inactive"}
        </EntityStatusBadge>
      </div>
      <div className="flex items-center justify-end gap-1">
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

function ProcedureCategoryEditorDialog({ state, onClose }: { state: EditorState; onClose: () => void }) {
  const isOpen = state.mode === "create" || state.mode === "edit";
  const category = state.mode === "edit" ? state.category : undefined;
  const queryClient = useQueryClient();

  const initial = useMemo(
    () => ({
      name: category?.name ?? "",
      description: category?.description ?? "",
      isImaging: category?.isImaging ?? false,
      isActive: category?.isActive ?? true,
    }),
    [category],
  );

  const [form, setForm] = useState(initial);
  useEffect(() => {
    if (isOpen) setForm(initial);
  }, [isOpen, initial]);

  const set = <K extends keyof typeof form>(key: K, value: (typeof form)[K]) =>
    setForm((f) => ({ ...f, [key]: value }));

  const invalidate = () => {
    queryClient.invalidateQueries({ queryKey: ["administration", "procedure-categories"] });
    queryClient.invalidateQueries({ queryKey: ["administration.procedureCategoryOptions"] });
  };

  const createMutation = useMutation({
    mutationFn: (input: CreateProcedureCategoryInput) => createProcedureCategory(input),
    onSuccess: () => {
      toast.success("Procedure category created");
      invalidate();
      onClose();
    },
    onError: (err) => toast.error("Create failed", { description: describe(err) }),
  });

  const updateMutation = useMutation({
    mutationFn: (input: UpdateProcedureCategoryInput) => updateProcedureCategory(input),
    onSuccess: () => {
      toast.success("Procedure category updated");
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
    const payload = {
      name: trimmedName,
      description: form.description.trim() || null,
      isImaging: form.isImaging,
    };
    if (state.mode === "edit" && category) {
      updateMutation.mutate({ categoryId: category.id, isActive: form.isActive, ...payload });
    } else {
      createMutation.mutate(payload);
    }
  };

  return (
    <Dialog open={isOpen} onOpenChange={(o) => (!o ? onClose() : undefined)}>
      <DialogContent className="!max-w-md">
        <form onSubmit={onSubmit}>
          <DialogHeader>
            <DialogTitle>{category ? "Edit procedure category" : "Add a procedure category"}</DialogTitle>
            <DialogDescription>
              {category ? `Update details for ${category.name}.` : "Add a procedure category to your organization."}
            </DialogDescription>
          </DialogHeader>

          <DialogBody className="space-y-5">
            <Field id="pc-cat-name" label="Name" required>
              <Input
                id="pc-cat-name"
                value={form.name}
                onChange={(e) => set("name", e.target.value)}
                placeholder="Radiology"
                autoFocus
                required
                maxLength={200}
              />
            </Field>

            <Field id="pc-cat-description" label="Description">
              <Input
                id="pc-cat-description"
                value={form.description}
                onChange={(e) => set("description", e.target.value)}
                placeholder="Imaging procedures"
                maxLength={500}
              />
            </Field>

            <div className="flex items-center justify-between rounded-lg border border-[var(--color-border)] px-3 py-2.5">
              <div>
                <p className="text-[13px] font-medium text-[var(--color-foreground)]">Imaging</p>
                <p className="text-[12px] text-[var(--color-muted-foreground)]">
                  Mark categories that cover imaging procedures.
                </p>
              </div>
              <Switch
                checked={form.isImaging}
                onCheckedChange={(v) => set("isImaging", v)}
                aria-label="Imaging category"
              />
            </div>

            {category && (
              <div className="flex items-center justify-between rounded-lg border border-[var(--color-border)] px-3 py-2.5">
                <div>
                  <p className="text-[13px] font-medium text-[var(--color-foreground)]">Active</p>
                  <p className="text-[12px] text-[var(--color-muted-foreground)]">
                    Inactive categories are hidden from selection lists.
                  </p>
                </div>
                <Switch checked={form.isActive} onCheckedChange={(v) => set("isActive", v)} aria-label="Procedure category active" />
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

function DeleteProcedureCategoryDialog({ state, onClose }: { state: EditorState; onClose: () => void }) {
  const isOpen = state.mode === "delete";
  const category = state.mode === "delete" ? state.category : undefined;
  const queryClient = useQueryClient();

  const deleteMutation = useMutation({
    mutationFn: (id: string) => deleteProcedureCategory(id),
    onSuccess: () => {
      toast.success("Procedure category deleted");
      queryClient.invalidateQueries({ queryKey: ["administration", "procedure-categories"] });
      queryClient.invalidateQueries({ queryKey: ["administration.procedureCategoryOptions"] });
      onClose();
    },
    onError: (err) => toast.error("Delete failed", { description: describe(err) }),
  });

  return (
    <Dialog open={isOpen} onOpenChange={(o) => (!o ? onClose() : undefined)}>
      <DialogContent>
        <DialogHeader>
          <DialogTitle className="text-[var(--color-destructive)]">Delete procedure category</DialogTitle>
          <DialogDescription>
            This removes{" "}
            <span className="font-medium text-[var(--color-foreground)]">{category?.name}</span>. Procedure codes
            referencing it will have their category cleared.
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
