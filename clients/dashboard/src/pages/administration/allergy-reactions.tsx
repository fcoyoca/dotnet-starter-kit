import { useEffect, useMemo, useState, type FormEvent } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { AlertTriangle, ChevronRight, Pencil, Plus, Search, Trash2 } from "lucide-react";
import { toast } from "sonner";
import {
  createAllergyReaction,
  deleteAllergyReaction,
  listAllergyReactions,
  updateAllergyReaction,
  type AllergyReactionDto,
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

const COLS = "grid-cols-[1fr_140px_90px_24px]";

type EditorState =
  | { mode: "closed" }
  | { mode: "create" }
  | { mode: "edit"; reaction: AllergyReactionDto }
  | { mode: "delete"; reaction: AllergyReactionDto };

export function AllergyReactionsPage() {
  const [search, setSearch] = useState("");
  const [editor, setEditor] = useState<EditorState>({ mode: "closed" });

  const query = useQuery({
    queryKey: ["administration", "allergy-reactions-page"],
    queryFn: () => listAllergyReactions(),
  });

  const all = query.data ?? [];
  const term = search.trim().toLowerCase();
  const items = term ? all.filter((r) => r.term.toLowerCase().includes(term)) : all;
  const searchActive = term.length > 0;

  return (
    <div className="space-y-4 sm:space-y-6">
      <EntityPageHeader
        icon={AlertTriangle}
        title="Allergy Reactions"
        total={query.data ? all.length : null}
        unit="reaction"
        description="Curated SNOMED reaction options offered by the patient chart's allergy dialog."
      >
        <Button
          onClick={() => setEditor({ mode: "create" })}
          className="h-9 flex-1 gap-1.5 rounded-lg px-4 text-[13px] font-semibold sm:flex-none"
        >
          <Plus className="size-4" />
          New reaction
        </Button>
      </EntityPageHeader>

      <EntitySearch value={search} onChange={setSearch} placeholder="Search by term…" />

      {query.isLoading ? (
        <EntityListLoading desktopColumns={COLS} />
      ) : items.length === 0 ? (
        <EntityEmpty
          icon={searchActive ? Search : AlertTriangle}
          title={searchActive ? "No reactions found" : "No reactions yet"}
          body={
            searchActive
              ? `Nothing matches "${search.trim()}". Try a different term or clear the search.`
              : "Add your first allergy reaction."
          }
          action={
            searchActive ? (
              <Button variant="outline" onClick={() => setSearch("")} className="h-9 rounded-lg px-4 text-[13px]">
                Clear search
              </Button>
            ) : (
              <Button onClick={() => setEditor({ mode: "create" })} className="h-9 rounded-lg px-4 text-[13px]">
                <Plus className="mr-1.5 size-4" />
                Add reaction
              </Button>
            )
          }
        />
      ) : (
        <div>
          <div className="mb-3 flex items-center justify-between">
            <p className="text-[12px] font-medium text-[var(--color-muted-foreground)]">
              {items.length} reaction{items.length !== 1 ? "s" : ""} {searchActive ? "found" : ""}
            </p>
          </div>

          <div className="space-y-2 md:hidden">
            {items.map((r) => (
              <MobileCard key={r.id} reaction={r} onEdit={() => setEditor({ mode: "edit", reaction: r })} />
            ))}
          </div>

          <EntityListCard className="hidden md:block">
            <EntityListHeader className={COLS}>
              <span>Term</span>
              <span>SNOMED code</span>
              <span>Status</span>
              <span />
            </EntityListHeader>
            {items.map((r, i) => (
              <DesktopRow
                key={r.id}
                reaction={r}
                isLast={i === items.length - 1}
                onEdit={() => setEditor({ mode: "edit", reaction: r })}
                onDelete={() => setEditor({ mode: "delete", reaction: r })}
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

      <AllergyReactionEditorDialog state={editor} onClose={() => setEditor({ mode: "closed" })} />
      <DeleteAllergyReactionDialog state={editor} onClose={() => setEditor({ mode: "closed" })} />
    </div>
  );
}

function MobileCard({ reaction, onEdit }: { reaction: AllergyReactionDto; onEdit: () => void }) {
  return (
    <EntityMobileCard
      href="#"
      onClick={(e) => {
        e.preventDefault();
        onEdit();
      }}
      aria-label={`Edit allergy reaction ${reaction.term}`}
    >
      <div className="flex items-center justify-between">
        <div className="flex min-w-0 items-center gap-3">
          <EntityInitialsAvatar name={reaction.term} size={40} />
          <div className="min-w-0">
            <p className="truncate text-[14px] font-medium text-[var(--color-foreground)]">{reaction.term}</p>
            {reaction.snomedCode && (
              <p className="mt-0.5 truncate text-[12px] text-[var(--color-muted-foreground)]">
                {reaction.snomedCode}
              </p>
            )}
          </div>
        </div>
        <EntityStatusBadge tone={reaction.isActive ? "success" : "default"}>
          {reaction.isActive ? "Active" : "Inactive"}
        </EntityStatusBadge>
      </div>
    </EntityMobileCard>
  );
}

function DesktopRow({
  reaction,
  isLast,
  onEdit,
  onDelete,
}: {
  reaction: AllergyReactionDto;
  isLast: boolean;
  onEdit: () => void;
  onDelete: () => void;
}) {
  return (
    <EntityListRow className={COLS} isLast={isLast}>
      <div className="flex min-w-0 items-center gap-3">
        <EntityInitialsAvatar name={reaction.term} size={36} />
        <div className="truncate text-[14px] font-medium text-[var(--color-foreground)] transition-colors group-hover:text-[var(--color-primary)]">
          {reaction.term}
        </div>
      </div>
      <div className="flex items-center text-[12px] text-[var(--color-muted-foreground)]">
        {reaction.snomedCode ?? "—"}
      </div>
      <div className="flex items-center">
        <EntityStatusBadge tone={reaction.isActive ? "success" : "default"}>
          {reaction.isActive ? "Active" : "Inactive"}
        </EntityStatusBadge>
      </div>
      <div className="flex items-center justify-end gap-1">
        <button
          type="button"
          aria-label={`Edit ${reaction.term}`}
          onClick={onEdit}
          className="grid size-7 cursor-pointer place-items-center rounded-md text-[var(--color-muted-foreground)] opacity-0 transition-all hover:bg-[var(--color-muted)] hover:text-[var(--color-foreground)] group-hover:opacity-100"
        >
          <Pencil className="size-3.5" />
        </button>
        <button
          type="button"
          aria-label={`Delete ${reaction.term}`}
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

function AllergyReactionEditorDialog({ state, onClose }: { state: EditorState; onClose: () => void }) {
  const isOpen = state.mode === "create" || state.mode === "edit";
  const reaction = state.mode === "edit" ? state.reaction : undefined;
  const queryClient = useQueryClient();

  const initial = useMemo(
    () => ({
      term: reaction?.term ?? "",
      snomedCode: reaction?.snomedCode ?? "",
      isActive: reaction?.isActive ?? true,
    }),
    [reaction],
  );

  const [form, setForm] = useState(initial);
  useEffect(() => {
    if (isOpen) setForm(initial);
  }, [isOpen, initial]);

  const invalidate = () => {
    queryClient.invalidateQueries({ queryKey: ["administration", "allergy-reactions-page"] });
    queryClient.invalidateQueries({ queryKey: ["allergy-reactions"] });
  };

  const createMutation = useMutation({
    mutationFn: (input: { term: string; snomedCode?: string | null }) => createAllergyReaction(input),
    onSuccess: () => {
      toast.success("Allergy reaction created");
      invalidate();
      onClose();
    },
    onError: (err) => toast.error("Create failed", { description: describe(err) }),
  });

  const updateMutation = useMutation({
    mutationFn: (input: { id: number; term: string; snomedCode?: string | null; isActive: boolean }) =>
      updateAllergyReaction(input),
    onSuccess: () => {
      toast.success("Allergy reaction updated");
      invalidate();
      onClose();
    },
    onError: (err) => toast.error("Update failed", { description: describe(err) }),
  });

  const isPending = createMutation.isPending || updateMutation.isPending;
  const trimmedTerm = form.term.trim();

  const onSubmit = (e: FormEvent<HTMLFormElement>) => {
    e.preventDefault();
    if (!trimmedTerm) return;
    const snomedCode = form.snomedCode.trim() || null;
    if (state.mode === "edit" && reaction) {
      updateMutation.mutate({ id: reaction.id, term: trimmedTerm, snomedCode, isActive: form.isActive });
    } else {
      createMutation.mutate({ term: trimmedTerm, snomedCode });
    }
  };

  return (
    <Dialog open={isOpen} onOpenChange={(o) => (!o ? onClose() : undefined)}>
      <DialogContent className="!max-w-md">
        <form onSubmit={onSubmit}>
          <DialogHeader>
            <DialogTitle>{reaction ? "Edit allergy reaction" : "Add an allergy reaction"}</DialogTitle>
            <DialogDescription>
              {reaction ? `Update details for ${reaction.term}.` : "Add a SNOMED-coded reaction option."}
            </DialogDescription>
          </DialogHeader>

          <DialogBody className="space-y-5">
            <Field id="ar-term" label="Term" required>
              <Input
                id="ar-term"
                value={form.term}
                onChange={(e) => setForm((f) => ({ ...f, term: e.target.value }))}
                placeholder="Hives"
                autoFocus
                required
                maxLength={256}
              />
            </Field>

            <Field id="ar-snomed" label="SNOMED Code">
              <Input
                id="ar-snomed"
                value={form.snomedCode}
                onChange={(e) => setForm((f) => ({ ...f, snomedCode: e.target.value }))}
                placeholder="126485001"
                maxLength={32}
              />
            </Field>

            {reaction && (
              <div className="flex items-center justify-between rounded-lg border border-[var(--color-border)] px-3 py-2.5">
                <div>
                  <p className="text-[13px] font-medium text-[var(--color-foreground)]">Active</p>
                  <p className="text-[12px] text-[var(--color-muted-foreground)]">
                    Inactive reactions are hidden from selection lists.
                  </p>
                </div>
                <Switch
                  checked={form.isActive}
                  onCheckedChange={(v) => setForm((f) => ({ ...f, isActive: v }))}
                  aria-label="Allergy reaction active"
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
            <Button type="submit" disabled={isPending || !trimmedTerm}>
              {isPending ? "Saving…" : reaction ? "Save changes" : "Add reaction"}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}

function DeleteAllergyReactionDialog({ state, onClose }: { state: EditorState; onClose: () => void }) {
  const isOpen = state.mode === "delete";
  const reaction = state.mode === "delete" ? state.reaction : undefined;
  const queryClient = useQueryClient();

  const deleteMutation = useMutation({
    mutationFn: (id: number) => deleteAllergyReaction(id),
    onSuccess: () => {
      toast.success("Allergy reaction deleted");
      queryClient.invalidateQueries({ queryKey: ["administration", "allergy-reactions-page"] });
      queryClient.invalidateQueries({ queryKey: ["allergy-reactions"] });
      onClose();
    },
    onError: (err) => toast.error("Delete failed", { description: describe(err) }),
  });

  return (
    <Dialog open={isOpen} onOpenChange={(o) => (!o ? onClose() : undefined)}>
      <DialogContent>
        <DialogHeader>
          <DialogTitle className="text-[var(--color-destructive)]">Delete allergy reaction</DialogTitle>
          <DialogDescription>
            This removes{" "}
            <span className="font-medium text-[var(--color-foreground)]">{reaction?.term}</span>. Patient allergies
            referencing it will keep their recorded reaction text.
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
            onClick={() => reaction && deleteMutation.mutate(reaction.id)}
            disabled={deleteMutation.isPending || !reaction}
          >
            {deleteMutation.isPending ? "Deleting…" : "Delete reaction"}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
