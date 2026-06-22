import { useEffect, useMemo, useState, type FormEvent } from "react";
import {
  keepPreviousData,
  useMutation,
  useQuery,
  useQueryClient,
} from "@tanstack/react-query";
import { AlertTriangle, ChevronRight, Pencil, Plus, Search, Trash2 } from "lucide-react";
import { toast } from "sonner";
import {
  createIncidentType,
  deleteIncidentType,
  listIncidentTypes,
  updateIncidentType,
  type IncidentTypeDto,
  type CreateIncidentTypeInput,
  type UpdateIncidentTypeInput,
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
  | { mode: "edit"; type: IncidentTypeDto }
  | { mode: "delete"; type: IncidentTypeDto };

export function IncidentTypesPage() {
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
    queryKey: ["administration", "incident-types", { search: debouncedSearch, pageNumber, pageSize: PAGE_SIZE }],
    queryFn: () => listIncidentTypes({ search: debouncedSearch || undefined, pageNumber, pageSize: PAGE_SIZE }),
    placeholderData: keepPreviousData,
  });

  const data = query.data;
  const items = data?.items ?? [];
  const searchActive = debouncedSearch.length > 0;

  return (
    <div className="space-y-4 sm:space-y-6">
      <EntityPageHeader
        icon={AlertTriangle}
        title="Incident Types"
        total={data?.totalCount ?? null}
        unit="type"
        description="Categories used to classify patient incident reports."
      >
        <Button
          onClick={() => setEditor({ mode: "create" })}
          className="h-9 flex-1 gap-1.5 rounded-lg px-4 text-[13px] font-semibold sm:flex-none"
        >
          <Plus className="size-4" />
          New type
        </Button>
      </EntityPageHeader>

      <EntitySearch value={search} onChange={setSearch} placeholder="Search by name…" />

      {query.isLoading && items.length === 0 ? (
        <EntityListLoading desktopColumns="grid-cols-[1fr_90px_24px]" />
      ) : items.length === 0 ? (
        <EntityEmpty
          icon={searchActive ? Search : AlertTriangle}
          title={searchActive ? "No incident types found" : "No incident types yet"}
          body={
            searchActive
              ? `Nothing matches "${debouncedSearch}". Try a different term or clear the search.`
              : "Add your first incident type to start classifying incident reports."
          }
          action={
            searchActive ? (
              <Button variant="outline" onClick={() => setSearch("")} className="h-9 rounded-lg px-4 text-[13px]">
                Clear search
              </Button>
            ) : (
              <Button onClick={() => setEditor({ mode: "create" })} className="h-9 rounded-lg px-4 text-[13px]">
                <Plus className="mr-1.5 size-4" />
                Add type
              </Button>
            )
          }
        />
      ) : (
        <div>
          <div className="mb-3 flex items-center justify-between">
            <p className="text-[12px] font-medium text-[var(--color-muted-foreground)]">
              {data?.totalCount ?? 0} type{(data?.totalCount ?? 0) !== 1 ? "s" : ""} found
            </p>
          </div>

          <div className="space-y-2 md:hidden">
            {items.map((t) => (
              <MobileCard key={t.id} type={t} onEdit={() => setEditor({ mode: "edit", type: t })} />
            ))}
          </div>

          <EntityListCard className="hidden md:block">
            <EntityListHeader className="grid-cols-[1fr_90px_24px]">
              <span>Type</span>
              <span>Status</span>
              <span />
            </EntityListHeader>
            {items.map((t, i) => (
              <DesktopRow
                key={t.id}
                type={t}
                isLast={i === items.length - 1}
                onEdit={() => setEditor({ mode: "edit", type: t })}
                onDelete={() => setEditor({ mode: "delete", type: t })}
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

      <IncidentTypeEditorDialog state={editor} onClose={() => setEditor({ mode: "closed" })} />
      <DeleteIncidentTypeDialog state={editor} onClose={() => setEditor({ mode: "closed" })} />
    </div>
  );
}

function MobileCard({ type, onEdit }: { type: IncidentTypeDto; onEdit: () => void }) {
  return (
    <EntityMobileCard
      href="#"
      onClick={(e) => {
        e.preventDefault();
        onEdit();
      }}
      aria-label={`Edit incident type ${type.name}`}
    >
      <div className="flex items-center justify-between">
        <div className="flex min-w-0 items-center gap-3">
          <EntityInitialsAvatar name={type.name} size={40} />
          <p className="truncate text-[14px] font-medium text-[var(--color-foreground)]">{type.name}</p>
        </div>
        <EntityStatusBadge tone={type.isActive ? "success" : "default"}>
          {type.isActive ? "Active" : "Inactive"}
        </EntityStatusBadge>
      </div>
    </EntityMobileCard>
  );
}

function DesktopRow({
  type,
  isLast,
  onEdit,
  onDelete,
}: {
  type: IncidentTypeDto;
  isLast: boolean;
  onEdit: () => void;
  onDelete: () => void;
}) {
  return (
    <EntityListRow className="grid-cols-[1fr_90px_24px]" isLast={isLast}>
      <div className="flex min-w-0 items-center gap-3">
        <EntityInitialsAvatar name={type.name} size={36} />
        <div className="truncate text-[14px] font-medium text-[var(--color-foreground)] transition-colors group-hover:text-[var(--color-primary)]">
          {type.name}
        </div>
      </div>
      <div className="flex items-center">
        <EntityStatusBadge tone={type.isActive ? "success" : "default"}>
          {type.isActive ? "Active" : "Inactive"}
        </EntityStatusBadge>
      </div>
      <div className="flex items-center justify-end gap-1">
        <button
          type="button"
          aria-label={`Edit ${type.name}`}
          onClick={onEdit}
          className="grid size-7 cursor-pointer place-items-center rounded-md text-[var(--color-muted-foreground)] opacity-0 transition-all hover:bg-[var(--color-muted)] hover:text-[var(--color-foreground)] group-hover:opacity-100"
        >
          <Pencil className="size-3.5" />
        </button>
        <button
          type="button"
          aria-label={`Delete ${type.name}`}
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

function IncidentTypeEditorDialog({ state, onClose }: { state: EditorState; onClose: () => void }) {
  const isOpen = state.mode === "create" || state.mode === "edit";
  const type = state.mode === "edit" ? state.type : undefined;
  const queryClient = useQueryClient();

  const initial = useMemo(
    () => ({ name: type?.name ?? "", isActive: type?.isActive ?? true }),
    [type],
  );

  const [form, setForm] = useState(initial);
  useEffect(() => {
    if (isOpen) setForm(initial);
  }, [isOpen, initial]);

  const invalidate = () =>
    queryClient.invalidateQueries({ queryKey: ["administration", "incident-types"] });

  const createMutation = useMutation({
    mutationFn: (input: CreateIncidentTypeInput) => createIncidentType(input),
    onSuccess: () => {
      toast.success("Incident type created");
      invalidate();
      onClose();
    },
    onError: (err) => toast.error("Create failed", { description: describe(err) }),
  });

  const updateMutation = useMutation({
    mutationFn: (input: UpdateIncidentTypeInput) => updateIncidentType(input),
    onSuccess: () => {
      toast.success("Incident type updated");
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
    if (state.mode === "edit" && type) {
      updateMutation.mutate({ typeId: type.id, name: trimmedName, isActive: form.isActive });
    } else {
      createMutation.mutate({ name: trimmedName });
    }
  };

  return (
    <Dialog open={isOpen} onOpenChange={(o) => (!o ? onClose() : undefined)}>
      <DialogContent className="!max-w-md">
        <form onSubmit={onSubmit}>
          <DialogHeader>
            <DialogTitle>{type ? "Edit incident type" : "Add an incident type"}</DialogTitle>
            <DialogDescription>
              {type ? `Update details for ${type.name}.` : "Add an incident type to your organization."}
            </DialogDescription>
          </DialogHeader>

          <DialogBody className="space-y-5">
            <Field id="inc-type-name" label="Name" required>
              <Input
                id="inc-type-name"
                value={form.name}
                onChange={(e) => setForm((f) => ({ ...f, name: e.target.value }))}
                placeholder="Slip and Fall"
                autoFocus
                required
                maxLength={200}
              />
            </Field>

            {type && (
              <div className="flex items-center justify-between rounded-lg border border-[var(--color-border)] px-3 py-2.5">
                <div>
                  <p className="text-[13px] font-medium text-[var(--color-foreground)]">Active</p>
                  <p className="text-[12px] text-[var(--color-muted-foreground)]">
                    Inactive types are hidden from selection lists.
                  </p>
                </div>
                <Switch
                  checked={form.isActive}
                  onCheckedChange={(v) => setForm((f) => ({ ...f, isActive: v }))}
                  aria-label="Incident type active"
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
              {isPending ? "Saving…" : type ? "Save changes" : "Add type"}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}

function DeleteIncidentTypeDialog({ state, onClose }: { state: EditorState; onClose: () => void }) {
  const isOpen = state.mode === "delete";
  const type = state.mode === "delete" ? state.type : undefined;
  const queryClient = useQueryClient();

  const deleteMutation = useMutation({
    mutationFn: (id: string) => deleteIncidentType(id),
    onSuccess: () => {
      toast.success("Incident type deleted");
      queryClient.invalidateQueries({ queryKey: ["administration", "incident-types"] });
      onClose();
    },
    onError: (err) => toast.error("Delete failed", { description: describe(err) }),
  });

  return (
    <Dialog open={isOpen} onOpenChange={(o) => (!o ? onClose() : undefined)}>
      <DialogContent>
        <DialogHeader>
          <DialogTitle className="text-[var(--color-destructive)]">Delete incident type</DialogTitle>
          <DialogDescription>
            This removes{" "}
            <span className="font-medium text-[var(--color-foreground)]">{type?.name}</span>{" "}
            <span className="opacity-70">(created {type && formatDate(type.createdAtUtc)})</span>.
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
            onClick={() => type && deleteMutation.mutate(type.id)}
            disabled={deleteMutation.isPending || !type}
          >
            {deleteMutation.isPending ? "Deleting…" : "Delete type"}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
