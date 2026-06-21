import { useEffect, useMemo, useState, type FormEvent } from "react";
import {
  keepPreviousData,
  useMutation,
  useQuery,
  useQueryClient,
} from "@tanstack/react-query";
import { ChevronRight, Network, Pencil, Plus, Search, Trash2 } from "lucide-react";
import { toast } from "sonner";
import {
  createDepartment,
  deleteDepartment,
  listDepartments,
  updateDepartment,
  type DepartmentDto,
  type CreateDepartmentInput,
  type UpdateDepartmentInput,
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
  | { mode: "edit"; department: DepartmentDto }
  | { mode: "delete"; department: DepartmentDto };

export function DepartmentsPage() {
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
    queryKey: ["administration", "departments", { search: debouncedSearch, pageNumber, pageSize: PAGE_SIZE }],
    queryFn: () =>
      listDepartments({
        search: debouncedSearch || undefined,
        pageNumber,
        pageSize: PAGE_SIZE,
      }),
    placeholderData: keepPreviousData,
  });

  const data = query.data;
  const items = data?.items ?? [];
  const searchActive = debouncedSearch.length > 0;

  return (
    <div className="space-y-4 sm:space-y-6">
      <EntityPageHeader
        icon={Network}
        title="Departments"
        total={data?.totalCount ?? null}
        unit="department"
        description="Organize your practice into departments. Display order controls how they sort in selection lists."
      >
        <Button
          onClick={() => setEditor({ mode: "create" })}
          className="h-9 flex-1 gap-1.5 rounded-lg px-4 text-[13px] font-semibold sm:flex-none"
        >
          <Plus className="size-4" />
          New department
        </Button>
      </EntityPageHeader>

      <EntitySearch value={search} onChange={setSearch} placeholder="Search by name…" />

      {query.isLoading && items.length === 0 ? (
        <EntityListLoading desktopColumns="grid-cols-[1fr_120px_90px_24px]" />
      ) : items.length === 0 ? (
        <EntityEmpty
          icon={searchActive ? Search : Network}
          title={searchActive ? "No departments found" : "No departments yet"}
          body={
            searchActive
              ? `Nothing matches "${debouncedSearch}". Try a different term or clear the search.`
              : "Add your first department to organize providers and scheduling."
          }
          action={
            searchActive ? (
              <Button variant="outline" onClick={() => setSearch("")} className="h-9 rounded-lg px-4 text-[13px]">
                Clear search
              </Button>
            ) : (
              <Button onClick={() => setEditor({ mode: "create" })} className="h-9 rounded-lg px-4 text-[13px]">
                <Plus className="mr-1.5 size-4" />
                Add department
              </Button>
            )
          }
        />
      ) : (
        <div>
          <div className="mb-3 flex items-center justify-between">
            <p className="text-[12px] font-medium text-[var(--color-muted-foreground)]">
              {data?.totalCount ?? 0} department{(data?.totalCount ?? 0) !== 1 ? "s" : ""} found
            </p>
          </div>

          <div className="space-y-2 md:hidden">
            {items.map((d) => (
              <MobileCard key={d.id} department={d} onEdit={() => setEditor({ mode: "edit", department: d })} />
            ))}
          </div>

          <EntityListCard className="hidden md:block">
            <EntityListHeader className="grid-cols-[1fr_120px_90px_24px]">
              <span>Department</span>
              <span>Order</span>
              <span>Status</span>
              <span />
            </EntityListHeader>
            {items.map((d, i) => (
              <DesktopRow
                key={d.id}
                department={d}
                isLast={i === items.length - 1}
                onEdit={() => setEditor({ mode: "edit", department: d })}
                onDelete={() => setEditor({ mode: "delete", department: d })}
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

      <DepartmentEditorDialog state={editor} onClose={() => setEditor({ mode: "closed" })} />
      <DeleteDepartmentDialog state={editor} onClose={() => setEditor({ mode: "closed" })} />
    </div>
  );
}

function MobileCard({ department, onEdit }: { department: DepartmentDto; onEdit: () => void }) {
  return (
    <EntityMobileCard
      href="#"
      onClick={(e) => {
        e.preventDefault();
        onEdit();
      }}
      aria-label={`Edit department ${department.name}`}
    >
      <div className="flex items-center justify-between">
        <div className="flex min-w-0 items-center gap-3">
          <EntityInitialsAvatar name={department.name} size={40} />
          <p className="truncate text-[14px] font-medium text-[var(--color-foreground)]">{department.name}</p>
        </div>
        <EntityStatusBadge tone={department.isActive ? "success" : "default"}>
          {department.isActive ? "Active" : "Inactive"}
        </EntityStatusBadge>
      </div>
    </EntityMobileCard>
  );
}

function DesktopRow({
  department,
  isLast,
  onEdit,
  onDelete,
}: {
  department: DepartmentDto;
  isLast: boolean;
  onEdit: () => void;
  onDelete: () => void;
}) {
  return (
    <EntityListRow className="grid-cols-[1fr_120px_90px_24px]" isLast={isLast}>
      <div className="flex min-w-0 items-center gap-3">
        <EntityInitialsAvatar name={department.name} size={36} />
        <div className="truncate text-[14px] font-medium text-[var(--color-foreground)] transition-colors group-hover:text-[var(--color-primary)]">
          {department.name}
        </div>
      </div>
      <div className="text-[12px] text-[var(--color-muted-foreground)] tabular-nums">{department.displayOrder}</div>
      <div className="flex items-center">
        <EntityStatusBadge tone={department.isActive ? "success" : "default"}>
          {department.isActive ? "Active" : "Inactive"}
        </EntityStatusBadge>
      </div>
      <div className="flex items-center justify-end gap-1">
        <button
          type="button"
          aria-label={`Edit ${department.name}`}
          onClick={onEdit}
          className="grid size-7 cursor-pointer place-items-center rounded-md text-[var(--color-muted-foreground)] opacity-0 transition-all hover:bg-[var(--color-muted)] hover:text-[var(--color-foreground)] group-hover:opacity-100"
        >
          <Pencil className="size-3.5" />
        </button>
        <button
          type="button"
          aria-label={`Delete ${department.name}`}
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

function DepartmentEditorDialog({ state, onClose }: { state: EditorState; onClose: () => void }) {
  const isOpen = state.mode === "create" || state.mode === "edit";
  const department = state.mode === "edit" ? state.department : undefined;
  const queryClient = useQueryClient();

  const initial = useMemo(
    () => ({
      name: department?.name ?? "",
      displayOrder: department?.displayOrder ?? 0,
      isActive: department?.isActive ?? true,
    }),
    [department],
  );

  const [form, setForm] = useState(initial);
  useEffect(() => {
    if (isOpen) setForm(initial);
  }, [isOpen, initial]);

  const invalidate = () => queryClient.invalidateQueries({ queryKey: ["administration", "departments"] });

  const createMutation = useMutation({
    mutationFn: (input: CreateDepartmentInput) => createDepartment(input),
    onSuccess: () => {
      toast.success("Department created");
      invalidate();
      onClose();
    },
    onError: (err) => toast.error("Create failed", { description: describe(err) }),
  });

  const updateMutation = useMutation({
    mutationFn: (input: UpdateDepartmentInput) => updateDepartment(input),
    onSuccess: () => {
      toast.success("Department updated");
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
    const payload = { name: trimmedName, displayOrder: form.displayOrder };
    if (state.mode === "edit" && department) {
      updateMutation.mutate({ departmentId: department.id, isActive: form.isActive, ...payload });
    } else {
      createMutation.mutate(payload);
    }
  };

  return (
    <Dialog open={isOpen} onOpenChange={(o) => (!o ? onClose() : undefined)}>
      <DialogContent className="!max-w-md">
        <form onSubmit={onSubmit}>
          <DialogHeader>
            <DialogTitle>{department ? "Edit department" : "Add a department"}</DialogTitle>
            <DialogDescription>
              {department ? `Update details for ${department.name}.` : "Add a department to your organization."}
            </DialogDescription>
          </DialogHeader>

          <DialogBody className="space-y-5">
            <Field id="dept-name" label="Name" required>
              <Input
                id="dept-name"
                value={form.name}
                onChange={(e) => setForm((f) => ({ ...f, name: e.target.value }))}
                placeholder="Cardiology"
                autoFocus
                required
                maxLength={200}
              />
            </Field>

            <Field id="dept-order" label="Display order" hint="Lower numbers sort first.">
              <Input
                id="dept-order"
                type="number"
                min={0}
                value={form.displayOrder}
                onChange={(e) => setForm((f) => ({ ...f, displayOrder: Math.max(0, Number(e.target.value) || 0) }))}
              />
            </Field>

            {department && (
              <div className="flex items-center justify-between rounded-lg border border-[var(--color-border)] px-3 py-2.5">
                <div>
                  <p className="text-[13px] font-medium text-[var(--color-foreground)]">Active</p>
                  <p className="text-[12px] text-[var(--color-muted-foreground)]">
                    Inactive departments are hidden from selection lists.
                  </p>
                </div>
                <Switch
                  checked={form.isActive}
                  onCheckedChange={(v) => setForm((f) => ({ ...f, isActive: v }))}
                  aria-label="Department active"
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
              {isPending ? "Saving…" : department ? "Save changes" : "Add department"}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}

function DeleteDepartmentDialog({ state, onClose }: { state: EditorState; onClose: () => void }) {
  const isOpen = state.mode === "delete";
  const department = state.mode === "delete" ? state.department : undefined;
  const queryClient = useQueryClient();

  const deleteMutation = useMutation({
    mutationFn: (id: string) => deleteDepartment(id),
    onSuccess: () => {
      toast.success("Department deleted");
      queryClient.invalidateQueries({ queryKey: ["administration", "departments"] });
      onClose();
    },
    onError: (err) => toast.error("Delete failed", { description: describe(err) }),
  });

  return (
    <Dialog open={isOpen} onOpenChange={(o) => (!o ? onClose() : undefined)}>
      <DialogContent>
        <DialogHeader>
          <DialogTitle className="text-[var(--color-destructive)]">Delete department</DialogTitle>
          <DialogDescription>
            This removes{" "}
            <span className="font-medium text-[var(--color-foreground)]">{department?.name}</span>{" "}
            <span className="opacity-70">(created {department && formatDate(department.createdAtUtc)})</span>.
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
            onClick={() => department && deleteMutation.mutate(department.id)}
            disabled={deleteMutation.isPending || !department}
          >
            {deleteMutation.isPending ? "Deleting…" : "Delete department"}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
