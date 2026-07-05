import { useEffect, useMemo, useState, type FormEvent } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { Beaker, ChevronRight, Pencil, Plus, Search, Trash2 } from "lucide-react";
import { toast } from "sonner";
import {
  createMedicationDoseUnit,
  deleteMedicationDoseUnit,
  listMedicationDoseUnits,
  updateMedicationDoseUnit,
  type MedicationDoseUnitDto,
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
  | { mode: "edit"; unit: MedicationDoseUnitDto }
  | { mode: "delete"; unit: MedicationDoseUnitDto };

export function MedicationDoseUnitsPage() {
  const [search, setSearch] = useState("");
  const [editor, setEditor] = useState<EditorState>({ mode: "closed" });

  const query = useQuery({
    queryKey: ["administration", "medication-dose-units-page"],
    queryFn: () => listMedicationDoseUnits(),
  });

  const all = query.data ?? [];
  const term = search.trim().toLowerCase();
  const items = term ? all.filter((u) => u.name.toLowerCase().includes(term)) : all;
  const searchActive = term.length > 0;

  return (
    <div className="space-y-4 sm:space-y-6">
      <EntityPageHeader
        icon={Beaker}
        title="Medication Dose Units"
        total={query.data ? all.length : null}
        unit="unit"
        description="Dose unit options offered by the patient chart's medication dialog."
      >
        <Button
          onClick={() => setEditor({ mode: "create" })}
          className="h-9 flex-1 gap-1.5 rounded-lg px-4 text-[13px] font-semibold sm:flex-none"
        >
          <Plus className="size-4" />
          New unit
        </Button>
      </EntityPageHeader>

      <EntitySearch value={search} onChange={setSearch} placeholder="Search by name…" />

      {query.isLoading ? (
        <EntityListLoading desktopColumns="grid-cols-[1fr_90px_24px]" />
      ) : items.length === 0 ? (
        <EntityEmpty
          icon={searchActive ? Search : Beaker}
          title={searchActive ? "No dose units found" : "No dose units yet"}
          body={
            searchActive
              ? `Nothing matches "${search.trim()}". Try a different term or clear the search.`
              : "Add your first medication dose unit."
          }
          action={
            searchActive ? (
              <Button variant="outline" onClick={() => setSearch("")} className="h-9 rounded-lg px-4 text-[13px]">
                Clear search
              </Button>
            ) : (
              <Button onClick={() => setEditor({ mode: "create" })} className="h-9 rounded-lg px-4 text-[13px]">
                <Plus className="mr-1.5 size-4" />
                Add dose unit
              </Button>
            )
          }
        />
      ) : (
        <div>
          <div className="mb-3 flex items-center justify-between">
            <p className="text-[12px] font-medium text-[var(--color-muted-foreground)]">
              {items.length} unit{items.length !== 1 ? "s" : ""} {searchActive ? "found" : ""}
            </p>
          </div>

          <div className="space-y-2 md:hidden">
            {items.map((u) => (
              <MobileCard key={u.id} unit={u} onEdit={() => setEditor({ mode: "edit", unit: u })} />
            ))}
          </div>

          <EntityListCard className="hidden md:block">
            <EntityListHeader className="grid-cols-[1fr_90px_24px]">
              <span>Dose unit</span>
              <span>Status</span>
              <span />
            </EntityListHeader>
            {items.map((u, i) => (
              <DesktopRow
                key={u.id}
                unit={u}
                isLast={i === items.length - 1}
                onEdit={() => setEditor({ mode: "edit", unit: u })}
                onDelete={() => setEditor({ mode: "delete", unit: u })}
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

      <MedicationDoseUnitEditorDialog state={editor} onClose={() => setEditor({ mode: "closed" })} />
      <DeleteMedicationDoseUnitDialog state={editor} onClose={() => setEditor({ mode: "closed" })} />
    </div>
  );
}

function MobileCard({ unit, onEdit }: { unit: MedicationDoseUnitDto; onEdit: () => void }) {
  return (
    <EntityMobileCard
      href="#"
      onClick={(e) => {
        e.preventDefault();
        onEdit();
      }}
      aria-label={`Edit dose unit ${unit.name}`}
    >
      <div className="flex items-center justify-between">
        <div className="flex min-w-0 items-center gap-3">
          <EntityInitialsAvatar name={unit.name} size={40} />
          <p className="truncate text-[14px] font-medium text-[var(--color-foreground)]">{unit.name}</p>
        </div>
        <EntityStatusBadge tone={unit.isActive ? "success" : "default"}>
          {unit.isActive ? "Active" : "Inactive"}
        </EntityStatusBadge>
      </div>
    </EntityMobileCard>
  );
}

function DesktopRow({
  unit,
  isLast,
  onEdit,
  onDelete,
}: {
  unit: MedicationDoseUnitDto;
  isLast: boolean;
  onEdit: () => void;
  onDelete: () => void;
}) {
  return (
    <EntityListRow className="grid-cols-[1fr_90px_24px]" isLast={isLast}>
      <div className="flex min-w-0 items-center gap-3">
        <EntityInitialsAvatar name={unit.name} size={36} />
        <div className="truncate text-[14px] font-medium text-[var(--color-foreground)] transition-colors group-hover:text-[var(--color-primary)]">
          {unit.name}
        </div>
      </div>
      <div className="flex items-center">
        <EntityStatusBadge tone={unit.isActive ? "success" : "default"}>
          {unit.isActive ? "Active" : "Inactive"}
        </EntityStatusBadge>
      </div>
      <div className="flex items-center justify-end gap-1">
        <button
          type="button"
          aria-label={`Edit ${unit.name}`}
          onClick={onEdit}
          className="grid size-7 cursor-pointer place-items-center rounded-md text-[var(--color-muted-foreground)] opacity-0 transition-all hover:bg-[var(--color-muted)] hover:text-[var(--color-foreground)] group-hover:opacity-100"
        >
          <Pencil className="size-3.5" />
        </button>
        <button
          type="button"
          aria-label={`Delete ${unit.name}`}
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

function MedicationDoseUnitEditorDialog({ state, onClose }: { state: EditorState; onClose: () => void }) {
  const isOpen = state.mode === "create" || state.mode === "edit";
  const unit = state.mode === "edit" ? state.unit : undefined;
  const queryClient = useQueryClient();

  const initial = useMemo(() => ({ name: unit?.name ?? "", isActive: unit?.isActive ?? true }), [unit]);

  const [form, setForm] = useState(initial);
  useEffect(() => {
    if (isOpen) setForm(initial);
  }, [isOpen, initial]);

  const invalidate = () => {
    queryClient.invalidateQueries({ queryKey: ["administration", "medication-dose-units-page"] });
    queryClient.invalidateQueries({ queryKey: ["medication-dose-units"] });
  };

  const createMutation = useMutation({
    mutationFn: (name: string) => createMedicationDoseUnit({ name }),
    onSuccess: () => {
      toast.success("Dose unit created");
      invalidate();
      onClose();
    },
    onError: (err) => toast.error("Create failed", { description: describe(err) }),
  });

  const updateMutation = useMutation({
    mutationFn: (input: { id: number; name: string; isActive: boolean }) => updateMedicationDoseUnit(input),
    onSuccess: () => {
      toast.success("Dose unit updated");
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
    if (state.mode === "edit" && unit) {
      updateMutation.mutate({ id: unit.id, name: trimmedName, isActive: form.isActive });
    } else {
      createMutation.mutate(trimmedName);
    }
  };

  return (
    <Dialog open={isOpen} onOpenChange={(o) => (!o ? onClose() : undefined)}>
      <DialogContent className="!max-w-md">
        <form onSubmit={onSubmit}>
          <DialogHeader>
            <DialogTitle>{unit ? "Edit dose unit" : "Add a dose unit"}</DialogTitle>
            <DialogDescription>
              {unit ? `Update details for ${unit.name}.` : "Add a medication dose unit."}
            </DialogDescription>
          </DialogHeader>

          <DialogBody className="space-y-5">
            <Field id="mdu-name" label="Name" required>
              <Input
                id="mdu-name"
                value={form.name}
                onChange={(e) => setForm((f) => ({ ...f, name: e.target.value }))}
                placeholder="mg"
                autoFocus
                required
                maxLength={64}
              />
            </Field>

            {unit && (
              <div className="flex items-center justify-between rounded-lg border border-[var(--color-border)] px-3 py-2.5">
                <div>
                  <p className="text-[13px] font-medium text-[var(--color-foreground)]">Active</p>
                  <p className="text-[12px] text-[var(--color-muted-foreground)]">
                    Inactive units are hidden from selection lists.
                  </p>
                </div>
                <Switch
                  checked={form.isActive}
                  onCheckedChange={(v) => setForm((f) => ({ ...f, isActive: v }))}
                  aria-label="Dose unit active"
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
              {isPending ? "Saving…" : unit ? "Save changes" : "Add dose unit"}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}

function DeleteMedicationDoseUnitDialog({ state, onClose }: { state: EditorState; onClose: () => void }) {
  const isOpen = state.mode === "delete";
  const unit = state.mode === "delete" ? state.unit : undefined;
  const queryClient = useQueryClient();

  const deleteMutation = useMutation({
    mutationFn: (id: number) => deleteMedicationDoseUnit(id),
    onSuccess: () => {
      toast.success("Dose unit deleted");
      queryClient.invalidateQueries({ queryKey: ["administration", "medication-dose-units-page"] });
      queryClient.invalidateQueries({ queryKey: ["medication-dose-units"] });
      onClose();
    },
    onError: (err) => toast.error("Delete failed", { description: describe(err) }),
  });

  return (
    <Dialog open={isOpen} onOpenChange={(o) => (!o ? onClose() : undefined)}>
      <DialogContent>
        <DialogHeader>
          <DialogTitle className="text-[var(--color-destructive)]">Delete dose unit</DialogTitle>
          <DialogDescription>
            This removes{" "}
            <span className="font-medium text-[var(--color-foreground)]">{unit?.name}</span>. Medications
            referencing it will keep their recorded dose text.
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
            onClick={() => unit && deleteMutation.mutate(unit.id)}
            disabled={deleteMutation.isPending || !unit}
          >
            {deleteMutation.isPending ? "Deleting…" : "Delete dose unit"}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
