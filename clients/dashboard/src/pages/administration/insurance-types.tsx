import { useEffect, useMemo, useState, type FormEvent } from "react";
import {
  keepPreviousData,
  useMutation,
  useQuery,
  useQueryClient,
} from "@tanstack/react-query";
import { ChevronRight, Pencil, Plus, Search, ShieldPlus, Trash2 } from "lucide-react";
import { toast } from "sonner";
import {
  createInsuranceType,
  deleteInsuranceType,
  listInsuranceTypeProcedures,
  listInsuranceTypes,
  listProcedureCodes,
  setInsuranceTypeProcedures,
  updateInsuranceType,
  useProcedureCategoryOptions,
  type InsuranceTypeDto,
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
import { describe, formatDate } from "@/lib/list-helpers";

const PAGE_SIZE = 20;

/** Sentinel value for the "All categories" picker option (maps to a null category on the type). */
const ALL_CATEGORIES = "__all__";

type EditorState =
  | { mode: "closed" }
  | { mode: "create" }
  | { mode: "edit"; type: InsuranceTypeDto }
  | { mode: "delete"; type: InsuranceTypeDto };

export function InsuranceTypesPage() {
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
    queryKey: ["administration", "insurance-types", { search: debouncedSearch, pageNumber, pageSize: PAGE_SIZE }],
    queryFn: () => listInsuranceTypes({ search: debouncedSearch || undefined, pageNumber, pageSize: PAGE_SIZE }),
    placeholderData: keepPreviousData,
  });

  const data = query.data;
  const items = data?.items ?? [];
  const searchActive = debouncedSearch.length > 0;

  return (
    <div className="space-y-4 sm:space-y-6">
      <EntityPageHeader
        icon={ShieldPlus}
        title="Insurance Types"
        total={data?.totalCount ?? null}
        unit="type"
        description="Categories of insurance (PPO, HMO, Medicare, Workers' Comp). Companies are grouped under these types."
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
          icon={searchActive ? Search : ShieldPlus}
          title={searchActive ? "No insurance types found" : "No insurance types yet"}
          body={
            searchActive
              ? `Nothing matches "${debouncedSearch}". Try a different term or clear the search.`
              : "Add your first insurance type to start grouping payers."
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

      <InsuranceTypeEditorDialog state={editor} onClose={() => setEditor({ mode: "closed" })} />
      <DeleteInsuranceTypeDialog state={editor} onClose={() => setEditor({ mode: "closed" })} />
    </div>
  );
}

function MobileCard({ type, onEdit }: { type: InsuranceTypeDto; onEdit: () => void }) {
  return (
    <EntityMobileCard
      href="#"
      onClick={(e) => {
        e.preventDefault();
        onEdit();
      }}
      aria-label={`Edit insurance type ${type.name}`}
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
  type: InsuranceTypeDto;
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

function InsuranceTypeEditorDialog({ state, onClose }: { state: EditorState; onClose: () => void }) {
  const isOpen = state.mode === "create" || state.mode === "edit";
  const type = state.mode === "edit" ? state.type : undefined;
  const queryClient = useQueryClient();

  const categoryOptions = useProcedureCategoryOptions() ?? [];
  const categoryPickerOptions = [{ value: ALL_CATEGORIES, label: "All categories" }, ...categoryOptions];

  const initial = useMemo(
    () => ({
      name: type?.name ?? "",
      isActive: type?.isActive ?? true,
      procedureCategoryId: type?.procedureCategoryId ?? null,
    }),
    [type],
  );

  const [form, setForm] = useState(initial);
  // Local working set of associated codes (codeId -> price); captured on Save.
  const [selections, setSelections] = useState<Record<string, number>>({});

  // Existing associations for an edit, used to seed the selection set.
  const assocQuery = useQuery({
    queryKey: ["administration", "insurance-type-procedures", type?.id],
    queryFn: () => listInsuranceTypeProcedures(type!.id),
    enabled: isOpen && !!type,
  });

  // Reset the form AND the working selection whenever the dialog opens or targets a different type, so a
  // previous type's checked codes never linger into another type (initial is memoized per-type).
  useEffect(() => {
    if (isOpen) {
      setForm(initial);
      setSelections({});
    }
  }, [isOpen, initial]);

  // Seed the selection from the edited type's existing associations once they load.
  useEffect(() => {
    if (!isOpen || !type || !assocQuery.data) return;
    const seed: Record<string, number> = {};
    for (const a of assocQuery.data) seed[a.procedureCodeId] = a.price;
    setSelections(seed);
  }, [isOpen, type, assocQuery.data]);

  const invalidate = () => {
    queryClient.invalidateQueries({ queryKey: ["administration", "insurance-types"] });
    queryClient.invalidateQueries({ queryKey: ["administration.insuranceTypeOptions"] });
    queryClient.invalidateQueries({ queryKey: ["administration", "insurance-type-procedures"] });
  };

  const saveMutation = useMutation({
    mutationFn: async (payload: {
      name: string;
      isActive: boolean;
      procedureCategoryId: string | null;
      items: { procedureCodeId: string; price: number }[];
    }) => {
      if (type) {
        await updateInsuranceType({
          insuranceTypeId: type.id,
          name: payload.name,
          isActive: payload.isActive,
          procedureCategoryId: payload.procedureCategoryId,
        });
        await setInsuranceTypeProcedures(type.id, payload.items);
      } else {
        const newId = await createInsuranceType({ name: payload.name, procedureCategoryId: payload.procedureCategoryId });
        await setInsuranceTypeProcedures(newId, payload.items);
      }
    },
    onSuccess: () => {
      toast.success(type ? "Insurance type updated" : "Insurance type created");
      invalidate();
      onClose();
    },
    onError: (err) => toast.error("Save failed", { description: describe(err) }),
  });

  const isPending = saveMutation.isPending;
  const trimmedName = form.name.trim();

  const toggleSelection = (codeId: string, checked: boolean, price: number) =>
    setSelections((s) => {
      const next = { ...s };
      if (checked) next[codeId] = price;
      else delete next[codeId];
      return next;
    });

  const setSelectionPrice = (codeId: string, price: number) =>
    setSelections((s) => (codeId in s ? { ...s, [codeId]: price } : s));

  const onSubmit = (e: FormEvent<HTMLFormElement>) => {
    e.preventDefault();
    if (!trimmedName) return;
    const items = Object.entries(selections).map(([procedureCodeId, price]) => ({ procedureCodeId, price }));
    saveMutation.mutate({ name: trimmedName, isActive: form.isActive, procedureCategoryId: form.procedureCategoryId, items });
  };

  return (
    <Dialog open={isOpen} onOpenChange={(o) => (!o ? onClose() : undefined)}>
      <DialogContent className="!max-w-2xl">
        <form onSubmit={onSubmit}>
          <DialogHeader>
            <DialogTitle>{type ? "Edit insurance type" : "Add an insurance type"}</DialogTitle>
            <DialogDescription>
              {type ? `Update details for ${type.name}.` : "Add an insurance category to your organization."}
            </DialogDescription>
          </DialogHeader>

          <DialogBody className="space-y-5">
            <Field id="ins-type-name" label="Name" required>
              <Input
                id="ins-type-name"
                value={form.name}
                onChange={(e) => setForm((f) => ({ ...f, name: e.target.value }))}
                placeholder="PPO"
                autoFocus
                required
                maxLength={150}
              />
            </Field>

            <Field id="ins-type-category" label="Procedure category" hint="Scopes the associated procedure codes below.">
              <Combobox
                id="ins-type-category"
                label="Procedure category"
                variant="field"
                searchable
                emptyOptionLabel="All categories"
                placeholder="All categories"
                value={form.procedureCategoryId ?? ALL_CATEGORIES}
                onChange={(v) =>
                  setForm((f) => ({ ...f, procedureCategoryId: v === ALL_CATEGORIES || v === null ? null : v }))
                }
                options={categoryPickerOptions}
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
                  aria-label="Insurance type active"
                />
              </div>
            )}

            <AssociatedProcedureCodes
              key={type?.id ?? "create"}
              categoryId={form.procedureCategoryId}
              selections={selections}
              onToggle={toggleSelection}
              onPriceChange={setSelectionPrice}
              loadingExisting={!!type && assocQuery.isLoading}
            />
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

function DeleteInsuranceTypeDialog({ state, onClose }: { state: EditorState; onClose: () => void }) {
  const isOpen = state.mode === "delete";
  const type = state.mode === "delete" ? state.type : undefined;
  const queryClient = useQueryClient();

  const deleteMutation = useMutation({
    mutationFn: (id: string) => deleteInsuranceType(id),
    onSuccess: () => {
      toast.success("Insurance type deleted");
      queryClient.invalidateQueries({ queryKey: ["administration", "insurance-types"] });
      queryClient.invalidateQueries({ queryKey: ["administration.insuranceTypeOptions"] });
      onClose();
    },
    onError: (err) => toast.error("Delete failed", { description: describe(err) }),
  });

  return (
    <Dialog open={isOpen} onOpenChange={(o) => (!o ? onClose() : undefined)}>
      <DialogContent>
        <DialogHeader>
          <DialogTitle className="text-[var(--color-destructive)]">Delete insurance type</DialogTitle>
          <DialogDescription>
            This removes{" "}
            <span className="font-medium text-[var(--color-foreground)]">{type?.name}</span>{" "}
            <span className="opacity-70">(created {type && formatDate(type.createdAtUtc)})</span>. Companies
            referencing it will have their type cleared.
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

/**
 * Associated Procedure Codes — legacy InsuranceTypes > ItpPrice. Lists the active procedure codes within
 * the type's selected procedure category (or all when "All categories" is chosen) with a checkbox
 * (selected?) and a price input. Controlled: the selection set lives in the parent and is captured when
 * the insurance type is saved (batch). Editing a price commits to the parent on blur.
 */
function AssociatedProcedureCodes({
  categoryId,
  selections,
  onToggle,
  onPriceChange,
  loadingExisting,
}: {
  categoryId: string | null;
  selections: Record<string, number>;
  onToggle: (codeId: string, checked: boolean, price: number) => void;
  onPriceChange: (codeId: string, price: number) => void;
  loadingExisting?: boolean;
}) {
  const [search, setSearch] = useState("");
  const [priceDrafts, setPriceDrafts] = useState<Record<string, string>>({});

  const codesQuery = useQuery({
    queryKey: ["administration", "procedure-codes", "picker", categoryId],
    queryFn: () =>
      listProcedureCodes({
        isActive: true,
        procedureCategoryId: categoryId,
        pageSize: 200,
        sortBy: "code",
        sortDir: "asc",
      }),
    staleTime: 5 * 60 * 1000,
  });

  const codes = codesQuery.data?.items ?? [];
  const term = search.trim().toLowerCase();
  const filtered = codes.filter((c) => {
    if (!term) return true;
    return (
      c.code.toLowerCase().includes(term) ||
      (c.name?.toLowerCase().includes(term) ?? false) ||
      (c.description?.toLowerCase().includes(term) ?? false)
    );
  });

  const draftFor = (codeId: string) => priceDrafts[codeId] ?? (selections[codeId]?.toString() ?? "0");

  const handleToggle = (codeId: string, checked: boolean) => {
    onToggle(codeId, checked, Number(draftFor(codeId)) || 0);
    if (!checked) {
      setPriceDrafts((d) => {
        const next = { ...d };
        delete next[codeId];
        return next;
      });
    }
  };

  const commitPrice = (codeId: string) => {
    if (!(codeId in selections)) return;
    const next = Number(draftFor(codeId));
    if (Number.isNaN(next) || next < 0) return;
    onPriceChange(codeId, next);
  };

  const selectedCount = Object.keys(selections).length;
  const isLoading = codesQuery.isLoading || loadingExisting;

  return (
    <div className="space-y-3 border-t border-[var(--color-border)] pt-4">
      <div>
        <p className="text-[13px] font-semibold text-[var(--color-foreground)]">Associated Procedure Codes</p>
        <p className="text-[12px] text-[var(--color-muted-foreground)]">
          Select the procedure codes covered by this insurance type and set a price for each. Saved when you
          press Save.
          {selectedCount > 0 ? ` ${selectedCount} selected.` : ""}
        </p>
      </div>

      <Input
        value={search}
        onChange={(e) => setSearch(e.target.value)}
        placeholder="Search code, name or description…"
      />

      {codesQuery.isError ? (
        <div
          role="alert"
          className="rounded-lg border border-[oklch(from_var(--color-destructive)_l_c_h_/_0.30)] bg-[oklch(from_var(--color-destructive)_l_c_h_/_0.06)] px-3 py-2 text-sm text-[var(--color-destructive)]"
        >
          {describe(codesQuery.error)}
        </div>
      ) : isLoading ? (
        <p className="px-1 py-6 text-center text-[13px] text-[var(--color-muted-foreground)]">Loading procedure codes…</p>
      ) : filtered.length === 0 ? (
        <p className="px-1 py-6 text-center text-[13px] text-[var(--color-muted-foreground)]">
          {codes.length === 0 ? "No procedure codes yet. Add some under Procedure Codes." : "No codes match your search."}
        </p>
      ) : (
        <div className="max-h-72 overflow-auto rounded-lg border border-[var(--color-border)]">
          <table className="w-full text-[13px]">
            <thead className="sticky top-0 bg-[var(--color-muted)] text-[12px] font-medium text-[var(--color-muted-foreground)]">
              <tr>
                <th className="w-10 px-2 py-2 text-left" />
                <th className="px-2 py-2 text-left">Code</th>
                <th className="px-2 py-2 text-left">Description</th>
                <th className="px-2 py-2 text-left">Category</th>
                <th className="w-28 px-2 py-2 text-right">Price</th>
              </tr>
            </thead>
            <tbody>
              {filtered.map((c) => {
                const selected = c.id in selections;
                return (
                  <tr key={c.id} className="border-t border-[var(--color-border)]">
                    <td className="px-2 py-1.5">
                      <input
                        type="checkbox"
                        checked={selected}
                        onChange={(e) => handleToggle(c.id, e.target.checked)}
                        aria-label={`Select ${c.code}`}
                        className="size-4 cursor-pointer accent-[var(--color-primary)]"
                      />
                    </td>
                    <td className="px-2 py-1.5 font-medium text-[var(--color-foreground)]">{c.code}</td>
                    <td className="max-w-[1px] truncate px-2 py-1.5 text-[var(--color-muted-foreground)]">
                      {c.name ?? c.description ?? "—"}
                    </td>
                    <td className="px-2 py-1.5 text-[var(--color-muted-foreground)]">{c.procedureCategoryName ?? "—"}</td>
                    <td className="px-2 py-1.5 text-right">
                      <input
                        type="number"
                        min={0}
                        step="0.01"
                        value={draftFor(c.id)}
                        disabled={!selected}
                        aria-label={`Price for ${c.code}`}
                        onChange={(e) => setPriceDrafts((d) => ({ ...d, [c.id]: e.target.value }))}
                        onBlur={() => commitPrice(c.id)}
                        className="w-24 rounded-md border border-[var(--color-input)] bg-transparent px-2 py-1 text-right text-[13px] disabled:opacity-40 focus-visible:border-[var(--color-ring)] focus-visible:outline-none focus-visible:ring-[3px] focus-visible:ring-[oklch(from_var(--color-ring)_l_c_h_/_0.5)]"
                      />
                    </td>
                  </tr>
                );
              })}
            </tbody>
          </table>
        </div>
      )}
    </div>
  );
}
