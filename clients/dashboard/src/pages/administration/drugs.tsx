import { useEffect, useMemo, useState, type FormEvent } from "react";
import {
  keepPreviousData,
  useMutation,
  useQuery,
  useQueryClient,
} from "@tanstack/react-query";
import { ChevronRight, Download, Pencil, Pill, Plus, Search, Trash2 } from "lucide-react";
import { toast } from "sonner";
import {
  createDrug,
  deleteDrug,
  importDrugs,
  listDrugs,
  searchRxNav,
  updateDrug,
  type DrugDto,
  type DrugInput,
  type RxNavDrug,
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
const COLS = "grid-cols-[1fr_110px_90px_90px_24px]";

type EditorState =
  | { mode: "closed" }
  | { mode: "create" }
  | { mode: "edit"; drug: DrugDto }
  | { mode: "delete"; drug: DrugDto };

export function DrugsPage() {
  const [search, setSearch] = useState("");
  const [debouncedSearch, setDebouncedSearch] = useState("");
  const [pageNumber, setPageNumber] = useState(1);
  const [editor, setEditor] = useState<EditorState>({ mode: "closed" });
  const [importOpen, setImportOpen] = useState(false);

  useEffect(() => {
    const t = setTimeout(() => {
      setDebouncedSearch(search.trim());
      setPageNumber(1);
    }, 250);
    return () => clearTimeout(t);
  }, [search]);

  const query = useQuery({
    queryKey: ["administration", "drugs-page", { search: debouncedSearch, pageNumber }],
    queryFn: () => listDrugs({ search: debouncedSearch || undefined, pageNumber, pageSize: PAGE_SIZE }),
    placeholderData: keepPreviousData,
  });

  const data = query.data;
  const items = data?.items ?? [];
  const searchActive = debouncedSearch.length > 0;

  return (
    <div className="space-y-4 sm:space-y-6">
      <EntityPageHeader
        icon={Pill}
        title="Drugs"
        total={data?.totalCount ?? null}
        unit="drug"
        description="RxNorm-backed drug catalog used by the patient chart's allergy and medication pickers."
      >
        <Button
          variant="outline"
          onClick={() => setImportOpen(true)}
          className="h-9 flex-1 gap-1.5 rounded-lg px-4 text-[13px] font-semibold sm:flex-none"
        >
          <Download className="size-4" />
          Import from RxNav
        </Button>
        <Button
          onClick={() => setEditor({ mode: "create" })}
          className="h-9 flex-1 gap-1.5 rounded-lg px-4 text-[13px] font-semibold sm:flex-none"
        >
          <Plus className="size-4" />
          New drug
        </Button>
      </EntityPageHeader>

      <EntitySearch value={search} onChange={setSearch} placeholder="Search by name…" />

      {query.isLoading && items.length === 0 ? (
        <EntityListLoading desktopColumns={COLS} />
      ) : items.length === 0 ? (
        <EntityEmpty
          icon={searchActive ? Search : Pill}
          title={searchActive ? "No drugs found" : "No drugs yet"}
          body={
            searchActive
              ? `Nothing matches "${search.trim()}". Try a different term or clear the search.`
              : "Add a drug or import concepts from RxNav."
          }
          action={
            searchActive ? (
              <Button variant="outline" onClick={() => setSearch("")} className="h-9 rounded-lg px-4 text-[13px]">
                Clear search
              </Button>
            ) : (
              <Button onClick={() => setEditor({ mode: "create" })} className="h-9 rounded-lg px-4 text-[13px]">
                <Plus className="mr-1.5 size-4" />
                Add drug
              </Button>
            )
          }
        />
      ) : (
        <div>
          <div className="mb-3 flex items-center justify-between">
            <p className="text-[12px] font-medium text-[var(--color-muted-foreground)]">
              {data?.totalCount ?? 0} drug{(data?.totalCount ?? 0) !== 1 ? "s" : ""} found
            </p>
          </div>

          <div className="space-y-2 md:hidden">
            {items.map((d) => (
              <MobileCard key={d.id} drug={d} onEdit={() => setEditor({ mode: "edit", drug: d })} />
            ))}
          </div>

          <EntityListCard className="hidden md:block">
            <EntityListHeader className={COLS}>
              <span>Name</span>
              <span>RxCUI</span>
              <span>TTY</span>
              <span>Status</span>
              <span />
            </EntityListHeader>
            {items.map((d, i) => (
              <DesktopRow
                key={d.id}
                drug={d}
                isLast={i === items.length - 1}
                onEdit={() => setEditor({ mode: "edit", drug: d })}
                onDelete={() => setEditor({ mode: "delete", drug: d })}
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

      <DrugEditorDialog state={editor} onClose={() => setEditor({ mode: "closed" })} />
      <DeleteDrugDialog state={editor} onClose={() => setEditor({ mode: "closed" })} />
      <RxNavImportDialog open={importOpen} onClose={() => setImportOpen(false)} />
    </div>
  );
}

function MobileCard({ drug, onEdit }: { drug: DrugDto; onEdit: () => void }) {
  return (
    <EntityMobileCard
      href="#"
      onClick={(e) => {
        e.preventDefault();
        onEdit();
      }}
      aria-label={`Edit drug ${drug.name}`}
    >
      <div className="flex items-center justify-between">
        <div className="flex min-w-0 items-center gap-3">
          <EntityInitialsAvatar name={drug.name} size={40} />
          <div className="min-w-0">
            <p className="truncate text-[14px] font-medium text-[var(--color-foreground)]">{drug.name}</p>
            {drug.rxCui && (
              <p className="mt-0.5 truncate text-[12px] text-[var(--color-muted-foreground)]">
                RxCUI {drug.rxCui}
                {drug.tty ? ` · ${drug.tty}` : ""}
              </p>
            )}
          </div>
        </div>
        <EntityStatusBadge tone={drug.isActive ? "success" : "default"}>
          {drug.isActive ? "Active" : "Inactive"}
        </EntityStatusBadge>
      </div>
    </EntityMobileCard>
  );
}

function DesktopRow({
  drug,
  isLast,
  onEdit,
  onDelete,
}: {
  drug: DrugDto;
  isLast: boolean;
  onEdit: () => void;
  onDelete: () => void;
}) {
  return (
    <EntityListRow className={COLS} isLast={isLast}>
      <div className="min-w-0 truncate text-[14px] font-medium text-[var(--color-foreground)] transition-colors group-hover:text-[var(--color-primary)]">
        {drug.name}
      </div>
      <div className="flex items-center text-[12px] text-[var(--color-muted-foreground)]">
        {drug.rxCui ?? "—"}
      </div>
      <div className="flex items-center text-[12px] text-[var(--color-muted-foreground)]">{drug.tty ?? "—"}</div>
      <div className="flex items-center">
        <EntityStatusBadge tone={drug.isActive ? "success" : "default"}>
          {drug.isActive ? "Active" : "Inactive"}
        </EntityStatusBadge>
      </div>
      <div className="flex items-center justify-end gap-1">
        <button
          type="button"
          aria-label={`Edit ${drug.name}`}
          onClick={onEdit}
          className="grid size-7 cursor-pointer place-items-center rounded-md text-[var(--color-muted-foreground)] opacity-0 transition-all hover:bg-[var(--color-muted)] hover:text-[var(--color-foreground)] group-hover:opacity-100"
        >
          <Pencil className="size-3.5" />
        </button>
        <button
          type="button"
          aria-label={`Delete ${drug.name}`}
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

function DrugEditorDialog({ state, onClose }: { state: EditorState; onClose: () => void }) {
  const isOpen = state.mode === "create" || state.mode === "edit";
  const drug = state.mode === "edit" ? state.drug : undefined;
  const queryClient = useQueryClient();

  const initial = useMemo(
    () => ({
      name: drug?.name ?? "",
      rxAui: drug?.rxAui ?? "",
      rxCui: drug?.rxCui ?? "",
      tty: drug?.tty ?? "",
      sab: drug?.sab ?? "",
      code: drug?.code ?? "",
      isActive: drug?.isActive ?? true,
    }),
    [drug],
  );

  const [form, setForm] = useState(initial);
  useEffect(() => {
    if (isOpen) setForm(initial);
  }, [isOpen, initial]);

  const set = <K extends keyof typeof form>(key: K, value: (typeof form)[K]) =>
    setForm((f) => ({ ...f, [key]: value }));

  const invalidate = () => queryClient.invalidateQueries({ queryKey: ["administration", "drugs-page"] });

  const createMutation = useMutation({
    mutationFn: (input: DrugInput) => createDrug(input),
    onSuccess: () => {
      toast.success("Drug created");
      invalidate();
      onClose();
    },
    onError: (err) => toast.error("Create failed", { description: describe(err) }),
  });

  const updateMutation = useMutation({
    mutationFn: (input: DrugInput & { id: number; isActive: boolean }) => updateDrug(input),
    onSuccess: () => {
      toast.success("Drug updated");
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
    const payload: DrugInput = {
      name: trimmedName,
      rxAui: form.rxAui.trim() || null,
      rxCui: form.rxCui.trim() || null,
      tty: form.tty.trim() || null,
      sab: form.sab.trim() || null,
      code: form.code.trim() || null,
    };
    if (state.mode === "edit" && drug) {
      updateMutation.mutate({ ...payload, id: drug.id, isActive: form.isActive });
    } else {
      createMutation.mutate(payload);
    }
  };

  return (
    <Dialog open={isOpen} onOpenChange={(o) => (!o ? onClose() : undefined)}>
      <DialogContent className="!max-w-lg">
        <form onSubmit={onSubmit}>
          <DialogHeader>
            <DialogTitle>{drug ? "Edit drug" : "Add a drug"}</DialogTitle>
            <DialogDescription>
              {drug ? `Update details for ${drug.name}.` : "Add a drug catalog entry."}
            </DialogDescription>
          </DialogHeader>

          <DialogBody className="space-y-5">
            <Field id="drug-name" label="Name" required>
              <Input
                id="drug-name"
                value={form.name}
                onChange={(e) => set("name", e.target.value)}
                placeholder="Amoxicillin 500 MG Oral Capsule"
                autoFocus
                required
                maxLength={2048}
              />
            </Field>

            <div className="grid gap-4 sm:grid-cols-2">
              <Field id="drug-rxaui" label="RxAUI">
                <Input
                  id="drug-rxaui"
                  value={form.rxAui}
                  onChange={(e) => set("rxAui", e.target.value)}
                  maxLength={12}
                />
              </Field>
              <Field id="drug-rxcui" label="RxCUI">
                <Input
                  id="drug-rxcui"
                  value={form.rxCui}
                  onChange={(e) => set("rxCui", e.target.value)}
                  maxLength={12}
                />
              </Field>
              <Field id="drug-tty" label="TTY">
                <Input id="drug-tty" value={form.tty} onChange={(e) => set("tty", e.target.value)} maxLength={20} />
              </Field>
              <Field id="drug-sab" label="SAB">
                <Input id="drug-sab" value={form.sab} onChange={(e) => set("sab", e.target.value)} maxLength={40} />
              </Field>
            </div>

            <Field id="drug-code" label="Code">
              <Input id="drug-code" value={form.code} onChange={(e) => set("code", e.target.value)} maxLength={64} />
            </Field>

            {drug && (
              <div className="flex items-center justify-between rounded-lg border border-[var(--color-border)] px-3 py-2.5">
                <div>
                  <p className="text-[13px] font-medium text-[var(--color-foreground)]">Active</p>
                  <p className="text-[12px] text-[var(--color-muted-foreground)]">
                    Inactive drugs are hidden from selection lists.
                  </p>
                </div>
                <Switch checked={form.isActive} onCheckedChange={(v) => set("isActive", v)} aria-label="Drug active" />
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
              {isPending ? "Saving…" : drug ? "Save changes" : "Add drug"}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}

function DeleteDrugDialog({ state, onClose }: { state: EditorState; onClose: () => void }) {
  const isOpen = state.mode === "delete";
  const drug = state.mode === "delete" ? state.drug : undefined;
  const queryClient = useQueryClient();

  const deleteMutation = useMutation({
    mutationFn: (id: number) => deleteDrug(id),
    onSuccess: () => {
      toast.success("Drug deleted");
      queryClient.invalidateQueries({ queryKey: ["administration", "drugs-page"] });
      queryClient.invalidateQueries({ queryKey: ["drug-search"] });
      onClose();
    },
    onError: (err) => toast.error("Delete failed", { description: describe(err) }),
  });

  return (
    <Dialog open={isOpen} onOpenChange={(o) => (!o ? onClose() : undefined)}>
      <DialogContent>
        <DialogHeader>
          <DialogTitle className="text-[var(--color-destructive)]">Delete drug</DialogTitle>
          <DialogDescription>
            This removes{" "}
            <span className="font-medium text-[var(--color-foreground)]">{drug?.name}</span>. Records referencing
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
            onClick={() => drug && deleteMutation.mutate(drug.id)}
            disabled={deleteMutation.isPending || !drug}
          >
            {deleteMutation.isPending ? "Deleting…" : "Delete drug"}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}

function RxNavImportDialog({ open, onClose }: { open: boolean; onClose(): void }) {
  const queryClient = useQueryClient();
  const [term, setTerm] = useState("");
  const [submittedTerm, setSubmittedTerm] = useState("");
  const [selected, setSelected] = useState<Record<string, RxNavDrug>>({});

  const rxNavQuery = useQuery({
    queryKey: ["administration", "rxnav-search", submittedTerm],
    queryFn: () => searchRxNav(submittedTerm),
    enabled: open && submittedTerm.length >= 3,
  });

  const importMutation = useMutation({
    mutationFn: (items: RxNavDrug[]) => importDrugs(items),
    onSuccess: (count) => {
      toast.success(`Imported ${count} drug${count === 1 ? "" : "s"} from RxNav.`);
      setSelected({});
      void queryClient.invalidateQueries({ queryKey: ["administration", "drugs-page"] });
      void queryClient.invalidateQueries({ queryKey: ["drug-search"] });
      onClose();
    },
    onError: (err) => toast.error("Import failed.", { description: describe(err) }),
  });

  const results = rxNavQuery.data ?? [];
  const selectedList = Object.values(selected);

  return (
    <Dialog open={open} onOpenChange={(o) => (!o ? onClose() : undefined)}>
      <DialogContent className="!max-w-2xl">
        <DialogHeader>
          <DialogTitle>Import from RxNav</DialogTitle>
          <DialogDescription>
            Search the NIH RxNorm API and choose which concepts to add to the local catalog.
          </DialogDescription>
        </DialogHeader>
        <DialogBody className="space-y-3">
          <form
            className="flex gap-2"
            onSubmit={(e) => {
              e.preventDefault();
              setSubmittedTerm(term.trim());
            }}
          >
            <Input
              value={term}
              onChange={(e) => setTerm(e.target.value)}
              placeholder="Drug name (min 3 chars)…"
              autoFocus
            />
            <Button type="submit" disabled={term.trim().length < 3 || rxNavQuery.isFetching}>
              {rxNavQuery.isFetching ? "Searching…" : "Search"}
            </Button>
          </form>

          {rxNavQuery.isError && (
            <p className="text-[13px] text-[var(--color-destructive)]">
              RxNav search failed. {describe(rxNavQuery.error)}
            </p>
          )}

          {results.length > 0 && (
            <ul className="max-h-72 divide-y divide-[var(--color-border)] overflow-y-auto rounded-lg border border-[var(--color-border)]">
              {results.map((d) => (
                <li key={d.rxCui} className="flex items-start gap-2 px-3 py-2">
                  <input
                    type="checkbox"
                    checked={!!selected[d.rxCui]}
                    onChange={(e) =>
                      setSelected((prev) => {
                        const next = { ...prev };
                        if (e.target.checked) next[d.rxCui] = d;
                        else delete next[d.rxCui];
                        return next;
                      })
                    }
                    className="mt-0.5 rounded border-[var(--color-border)]"
                  />
                  <div className="min-w-0">
                    {/* Wrap, don't truncate — RxNorm concept names (e.g. multi-component
                        vaccines) routinely run several lines long. */}
                    <p className="break-words text-[13px]">{d.name}</p>
                    <p className="text-[12px] text-[var(--color-muted-foreground)]">
                      RxCUI {d.rxCui}
                      {d.tty ? ` · ${d.tty}` : ""}
                    </p>
                  </div>
                </li>
              ))}
            </ul>
          )}

          {submittedTerm && !rxNavQuery.isFetching && results.length === 0 && !rxNavQuery.isError && (
            <p className="text-[13px] text-[var(--color-muted-foreground)]">No matches.</p>
          )}
        </DialogBody>
        <DialogFooter>
          <DialogClose asChild>
            <Button type="button" variant="outline">
              Cancel
            </Button>
          </DialogClose>
          <Button
            type="button"
            disabled={selectedList.length === 0 || importMutation.isPending}
            onClick={() => importMutation.mutate(selectedList)}
          >
            {importMutation.isPending ? "Importing…" : `Import ${selectedList.length || ""}`.trim()}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
