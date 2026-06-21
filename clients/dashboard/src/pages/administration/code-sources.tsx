import { useEffect, useMemo, useState, type FormEvent } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { ChevronRight, Pencil, Plus, Search, Tags, Trash2 } from "lucide-react";
import { toast } from "sonner";
import {
  createCodeSource,
  deleteCodeSource,
  listCodeSources,
  updateCodeSource,
  type CodeSourceDto,
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
  EntitySearch,
  EntityStatusBadge,
  Field,
} from "@/components/list";
import { describe } from "@/lib/list-helpers";

type EditorState =
  | { mode: "closed" }
  | { mode: "create" }
  | { mode: "edit"; source: CodeSourceDto }
  | { mode: "delete"; source: CodeSourceDto };

export function CodeSourcesPage() {
  const [search, setSearch] = useState("");
  const [editor, setEditor] = useState<EditorState>({ mode: "closed" });

  const query = useQuery({
    queryKey: ["administration", "code-sources-page"],
    queryFn: () => listCodeSources(),
  });

  const all = query.data ?? [];
  const term = search.trim().toLowerCase();
  const items = term ? all.filter((s) => s.name.toLowerCase().includes(term)) : all;
  const searchActive = term.length > 0;

  return (
    <div className="space-y-4 sm:space-y-6">
      <EntityPageHeader
        icon={Tags}
        title="Code Sources"
        total={query.data ? all.length : null}
        unit="source"
        description="Code systems a procedure code can belong to, e.g. CPT or HCPCS."
      >
        <Button
          onClick={() => setEditor({ mode: "create" })}
          className="h-9 flex-1 gap-1.5 rounded-lg px-4 text-[13px] font-semibold sm:flex-none"
        >
          <Plus className="size-4" />
          New code source
        </Button>
      </EntityPageHeader>

      <EntitySearch value={search} onChange={setSearch} placeholder="Search by name…" />

      {query.isLoading ? (
        <EntityListLoading desktopColumns="grid-cols-[1fr_90px_24px]" />
      ) : items.length === 0 ? (
        <EntityEmpty
          icon={searchActive ? Search : Tags}
          title={searchActive ? "No code sources found" : "No code sources yet"}
          body={
            searchActive
              ? `Nothing matches "${search.trim()}". Try a different term or clear the search.`
              : "Add your first code source."
          }
          action={
            searchActive ? (
              <Button variant="outline" onClick={() => setSearch("")} className="h-9 rounded-lg px-4 text-[13px]">
                Clear search
              </Button>
            ) : (
              <Button onClick={() => setEditor({ mode: "create" })} className="h-9 rounded-lg px-4 text-[13px]">
                <Plus className="mr-1.5 size-4" />
                Add code source
              </Button>
            )
          }
        />
      ) : (
        <div>
          <div className="mb-3 flex items-center justify-between">
            <p className="text-[12px] font-medium text-[var(--color-muted-foreground)]">
              {items.length} source{items.length !== 1 ? "s" : ""} {searchActive ? "found" : ""}
            </p>
          </div>

          <div className="space-y-2 md:hidden">
            {items.map((s) => (
              <MobileCard key={s.id} source={s} onEdit={() => setEditor({ mode: "edit", source: s })} />
            ))}
          </div>

          <EntityListCard className="hidden md:block">
            <EntityListHeader className="grid-cols-[1fr_90px_24px]">
              <span>Code source</span>
              <span>Status</span>
              <span />
            </EntityListHeader>
            {items.map((s, i) => (
              <DesktopRow
                key={s.id}
                source={s}
                isLast={i === items.length - 1}
                onEdit={() => setEditor({ mode: "edit", source: s })}
                onDelete={() => setEditor({ mode: "delete", source: s })}
              />
            ))}
          </EntityListCard>
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

      <CodeSourceEditorDialog state={editor} onClose={() => setEditor({ mode: "closed" })} />
      <DeleteCodeSourceDialog state={editor} onClose={() => setEditor({ mode: "closed" })} />
    </div>
  );
}

function MobileCard({ source, onEdit }: { source: CodeSourceDto; onEdit: () => void }) {
  return (
    <EntityMobileCard
      href="#"
      onClick={(e) => {
        e.preventDefault();
        onEdit();
      }}
      aria-label={`Edit code source ${source.name}`}
    >
      <div className="flex items-center justify-between">
        <div className="flex min-w-0 items-center gap-3">
          <EntityInitialsAvatar name={source.name} size={40} />
          <p className="truncate text-[14px] font-medium text-[var(--color-foreground)]">{source.name}</p>
        </div>
        <EntityStatusBadge tone={source.isActive ? "success" : "default"}>
          {source.isActive ? "Active" : "Inactive"}
        </EntityStatusBadge>
      </div>
    </EntityMobileCard>
  );
}

function DesktopRow({
  source,
  isLast,
  onEdit,
  onDelete,
}: {
  source: CodeSourceDto;
  isLast: boolean;
  onEdit: () => void;
  onDelete: () => void;
}) {
  return (
    <EntityListRow className="grid-cols-[1fr_90px_24px]" isLast={isLast}>
      <div className="flex min-w-0 items-center gap-3">
        <EntityInitialsAvatar name={source.name} size={36} />
        <div className="truncate text-[14px] font-medium text-[var(--color-foreground)] transition-colors group-hover:text-[var(--color-primary)]">
          {source.name}
        </div>
      </div>
      <div className="flex items-center">
        <EntityStatusBadge tone={source.isActive ? "success" : "default"}>
          {source.isActive ? "Active" : "Inactive"}
        </EntityStatusBadge>
      </div>
      <div className="flex items-center justify-end gap-1">
        <button
          type="button"
          aria-label={`Edit ${source.name}`}
          onClick={onEdit}
          className="grid size-7 cursor-pointer place-items-center rounded-md text-[var(--color-muted-foreground)] opacity-0 transition-all hover:bg-[var(--color-muted)] hover:text-[var(--color-foreground)] group-hover:opacity-100"
        >
          <Pencil className="size-3.5" />
        </button>
        <button
          type="button"
          aria-label={`Delete ${source.name}`}
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

function CodeSourceEditorDialog({ state, onClose }: { state: EditorState; onClose: () => void }) {
  const isOpen = state.mode === "create" || state.mode === "edit";
  const source = state.mode === "edit" ? state.source : undefined;
  const queryClient = useQueryClient();

  const initial = useMemo(
    () => ({ name: source?.name ?? "", isActive: source?.isActive ?? true }),
    [source],
  );

  const [form, setForm] = useState(initial);
  useEffect(() => {
    if (isOpen) setForm(initial);
  }, [isOpen, initial]);

  const invalidate = () => {
    queryClient.invalidateQueries({ queryKey: ["administration", "code-sources-page"] });
    queryClient.invalidateQueries({ queryKey: ["administration.codeSources"] });
  };

  const createMutation = useMutation({
    mutationFn: (name: string) => createCodeSource(name),
    onSuccess: () => {
      toast.success("Code source created");
      invalidate();
      onClose();
    },
    onError: (err) => toast.error("Create failed", { description: describe(err) }),
  });

  const updateMutation = useMutation({
    mutationFn: (input: { id: number; name: string; isActive: boolean }) => updateCodeSource(input),
    onSuccess: () => {
      toast.success("Code source updated");
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
    if (state.mode === "edit" && source) {
      updateMutation.mutate({ id: source.id, name: trimmedName, isActive: form.isActive });
    } else {
      createMutation.mutate(trimmedName);
    }
  };

  return (
    <Dialog open={isOpen} onOpenChange={(o) => (!o ? onClose() : undefined)}>
      <DialogContent className="!max-w-md">
        <form onSubmit={onSubmit}>
          <DialogHeader>
            <DialogTitle>{source ? "Edit code source" : "Add a code source"}</DialogTitle>
            <DialogDescription>
              {source ? `Update details for ${source.name}.` : "Add a procedure code system."}
            </DialogDescription>
          </DialogHeader>

          <DialogBody className="space-y-5">
            <Field id="cs-name" label="Name" required>
              <Input
                id="cs-name"
                value={form.name}
                onChange={(e) => setForm((f) => ({ ...f, name: e.target.value }))}
                placeholder="CPT"
                autoFocus
                required
                maxLength={128}
              />
            </Field>

            {source && (
              <div className="flex items-center justify-between rounded-lg border border-[var(--color-border)] px-3 py-2.5">
                <div>
                  <p className="text-[13px] font-medium text-[var(--color-foreground)]">Active</p>
                  <p className="text-[12px] text-[var(--color-muted-foreground)]">
                    Inactive sources are hidden from selection lists.
                  </p>
                </div>
                <Switch
                  checked={form.isActive}
                  onCheckedChange={(v) => setForm((f) => ({ ...f, isActive: v }))}
                  aria-label="Code source active"
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
              {isPending ? "Saving…" : source ? "Save changes" : "Add code source"}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}

function DeleteCodeSourceDialog({ state, onClose }: { state: EditorState; onClose: () => void }) {
  const isOpen = state.mode === "delete";
  const source = state.mode === "delete" ? state.source : undefined;
  const queryClient = useQueryClient();

  const deleteMutation = useMutation({
    mutationFn: (id: number) => deleteCodeSource(id),
    onSuccess: () => {
      toast.success("Code source deleted");
      queryClient.invalidateQueries({ queryKey: ["administration", "code-sources-page"] });
      queryClient.invalidateQueries({ queryKey: ["administration.codeSources"] });
      onClose();
    },
    onError: (err) => toast.error("Delete failed", { description: describe(err) }),
  });

  return (
    <Dialog open={isOpen} onOpenChange={(o) => (!o ? onClose() : undefined)}>
      <DialogContent>
        <DialogHeader>
          <DialogTitle className="text-[var(--color-destructive)]">Delete code source</DialogTitle>
          <DialogDescription>
            This removes{" "}
            <span className="font-medium text-[var(--color-foreground)]">{source?.name}</span>. Procedure codes
            using it will have their code source cleared.
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
            onClick={() => source && deleteMutation.mutate(source.id)}
            disabled={deleteMutation.isPending || !source}
          >
            {deleteMutation.isPending ? "Deleting…" : "Delete code source"}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
