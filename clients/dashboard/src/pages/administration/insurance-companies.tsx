import { useEffect, useMemo, useState, type FormEvent } from "react";
import {
  keepPreviousData,
  useMutation,
  useQuery,
  useQueryClient,
} from "@tanstack/react-query";
import { Building, ChevronRight, Pencil, Plus, Search, Trash2 } from "lucide-react";
import { toast } from "sonner";
import {
  createInsuranceCompany,
  deleteInsuranceCompany,
  listInsuranceCompanies,
  updateInsuranceCompany,
  useInsuranceTypeOptions,
  type InsuranceCompanyDto,
  type CreateInsuranceCompanyInput,
  type UpdateInsuranceCompanyInput,
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

const PAGE_SIZE = 20;

type EditorState =
  | { mode: "closed" }
  | { mode: "create" }
  | { mode: "edit"; company: InsuranceCompanyDto }
  | { mode: "delete"; company: InsuranceCompanyDto };

export function InsuranceCompaniesPage() {
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
    queryKey: ["administration", "insurance-companies", { search: debouncedSearch, pageNumber, pageSize: PAGE_SIZE }],
    queryFn: () =>
      listInsuranceCompanies({ search: debouncedSearch || undefined, pageNumber, pageSize: PAGE_SIZE, sortBy: "name", sortDir: "asc" }),
    placeholderData: keepPreviousData,
  });

  const data = query.data;
  const items = data?.items ?? [];
  const searchActive = debouncedSearch.length > 0;

  return (
    <div className="space-y-4 sm:space-y-6">
      <EntityPageHeader
        icon={Building}
        title="Insurance Companies"
        total={data?.totalCount ?? null}
        unit="company"
        description="Payers your organization bills. Each can belong to an insurance type and carry a claims address."
      >
        <Button
          onClick={() => setEditor({ mode: "create" })}
          className="h-9 flex-1 gap-1.5 rounded-lg px-4 text-[13px] font-semibold sm:flex-none"
        >
          <Plus className="size-4" />
          New company
        </Button>
      </EntityPageHeader>

      <EntitySearch value={search} onChange={setSearch} placeholder="Search by name or city…" />

      {query.isLoading && items.length === 0 ? (
        <EntityListLoading desktopColumns="grid-cols-[1fr_150px_160px_90px_24px]" />
      ) : items.length === 0 ? (
        <EntityEmpty
          icon={searchActive ? Search : Building}
          title={searchActive ? "No insurance companies found" : "No insurance companies yet"}
          body={
            searchActive
              ? `Nothing matches "${debouncedSearch}". Try a different term or clear the search.`
              : "Add your first payer to start billing against it."
          }
          action={
            searchActive ? (
              <Button variant="outline" onClick={() => setSearch("")} className="h-9 rounded-lg px-4 text-[13px]">
                Clear search
              </Button>
            ) : (
              <Button onClick={() => setEditor({ mode: "create" })} className="h-9 rounded-lg px-4 text-[13px]">
                <Plus className="mr-1.5 size-4" />
                Add company
              </Button>
            )
          }
        />
      ) : (
        <div>
          <div className="mb-3 flex items-center justify-between">
            <p className="text-[12px] font-medium text-[var(--color-muted-foreground)]">
              {data?.totalCount ?? 0} compan{(data?.totalCount ?? 0) !== 1 ? "ies" : "y"} found
            </p>
          </div>

          <div className="space-y-2 md:hidden">
            {items.map((c) => (
              <MobileCard key={c.id} company={c} onEdit={() => setEditor({ mode: "edit", company: c })} />
            ))}
          </div>

          <EntityListCard className="hidden md:block">
            <EntityListHeader className="grid-cols-[1fr_150px_160px_90px_24px]">
              <span>Company</span>
              <span>Type</span>
              <span>Location</span>
              <span>Status</span>
              <span />
            </EntityListHeader>
            {items.map((c, i) => (
              <DesktopRow
                key={c.id}
                company={c}
                isLast={i === items.length - 1}
                onEdit={() => setEditor({ mode: "edit", company: c })}
                onDelete={() => setEditor({ mode: "delete", company: c })}
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

      <InsuranceCompanyEditorDialog state={editor} onClose={() => setEditor({ mode: "closed" })} />
      <DeleteInsuranceCompanyDialog state={editor} onClose={() => setEditor({ mode: "closed" })} />
    </div>
  );
}

function MobileCard({ company, onEdit }: { company: InsuranceCompanyDto; onEdit: () => void }) {
  return (
    <EntityMobileCard
      href="#"
      onClick={(e) => {
        e.preventDefault();
        onEdit();
      }}
      aria-label={`Edit insurance company ${company.name}`}
    >
      <div className="flex items-center justify-between">
        <div className="flex min-w-0 items-center gap-3">
          <EntityInitialsAvatar name={company.name} size={40} />
          <div className="min-w-0">
            <p className="truncate text-[14px] font-medium text-[var(--color-foreground)]">{company.name}</p>
            {company.insuranceTypeName && (
              <p className="mt-0.5 truncate text-[12px] text-[var(--color-muted-foreground)]">{company.insuranceTypeName}</p>
            )}
          </div>
        </div>
        <EntityStatusBadge tone={company.isActive ? "success" : "default"}>
          {company.isActive ? "Active" : "Inactive"}
        </EntityStatusBadge>
      </div>
    </EntityMobileCard>
  );
}

function DesktopRow({
  company,
  isLast,
  onEdit,
  onDelete,
}: {
  company: InsuranceCompanyDto;
  isLast: boolean;
  onEdit: () => void;
  onDelete: () => void;
}) {
  const location = [company.city, company.state].filter(Boolean).join(", ");
  return (
    <EntityListRow className="grid-cols-[1fr_150px_160px_90px_24px]" isLast={isLast}>
      <div className="flex min-w-0 items-center gap-3">
        <EntityInitialsAvatar name={company.name} size={36} />
        <div className="truncate text-[14px] font-medium text-[var(--color-foreground)] transition-colors group-hover:text-[var(--color-primary)]">
          {company.name}
        </div>
      </div>
      <div className="min-w-0 truncate text-[12px] text-[var(--color-muted-foreground)]">
        {company.insuranceTypeName ?? "—"}
      </div>
      <div className="min-w-0 truncate text-[12px] text-[var(--color-muted-foreground)]">{location || "—"}</div>
      <div className="flex items-center">
        <EntityStatusBadge tone={company.isActive ? "success" : "default"}>
          {company.isActive ? "Active" : "Inactive"}
        </EntityStatusBadge>
      </div>
      <div className="flex items-center justify-end gap-1">
        <button
          type="button"
          aria-label={`Edit ${company.name}`}
          onClick={onEdit}
          className="grid size-7 cursor-pointer place-items-center rounded-md text-[var(--color-muted-foreground)] opacity-0 transition-all hover:bg-[var(--color-muted)] hover:text-[var(--color-foreground)] group-hover:opacity-100"
        >
          <Pencil className="size-3.5" />
        </button>
        <button
          type="button"
          aria-label={`Delete ${company.name}`}
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

function InsuranceCompanyEditorDialog({ state, onClose }: { state: EditorState; onClose: () => void }) {
  const isOpen = state.mode === "create" || state.mode === "edit";
  const company = state.mode === "edit" ? state.company : undefined;
  const queryClient = useQueryClient();
  const typeOptions = useInsuranceTypeOptions() ?? [];

  const initial = useMemo(
    () => ({
      name: company?.name ?? "",
      insuranceTypeId: company?.insuranceTypeId ?? null,
      formularyTiers: company?.formularyTiers ?? 0,
      address1: company?.address1 ?? "",
      address2: company?.address2 ?? "",
      city: company?.city ?? "",
      state: company?.state ?? "",
      zip: company?.zip ?? "",
      phone: company?.phone ?? "",
      isActive: company?.isActive ?? true,
    }),
    [company],
  );

  const [form, setForm] = useState(initial);
  useEffect(() => {
    if (isOpen) setForm(initial);
  }, [isOpen, initial]);

  const set = <K extends keyof typeof form>(key: K, value: (typeof form)[K]) =>
    setForm((f) => ({ ...f, [key]: value }));

  const invalidate = () => queryClient.invalidateQueries({ queryKey: ["administration", "insurance-companies"] });

  const createMutation = useMutation({
    mutationFn: (input: CreateInsuranceCompanyInput) => createInsuranceCompany(input),
    onSuccess: () => {
      toast.success("Insurance company created");
      invalidate();
      onClose();
    },
    onError: (err) => toast.error("Create failed", { description: describe(err) }),
  });

  const updateMutation = useMutation({
    mutationFn: (input: UpdateInsuranceCompanyInput) => updateInsuranceCompany(input),
    onSuccess: () => {
      toast.success("Insurance company updated");
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
      insuranceTypeId: form.insuranceTypeId,
      formularyTiers: form.formularyTiers,
      address1: form.address1.trim() || null,
      address2: form.address2.trim() || null,
      city: form.city.trim() || null,
      state: form.state.trim() || null,
      zip: form.zip.trim() || null,
      phone: form.phone.trim() || null,
    };
    if (state.mode === "edit" && company) {
      updateMutation.mutate({ companyId: company.id, isActive: form.isActive, ...payload });
    } else {
      createMutation.mutate(payload);
    }
  };

  return (
    <Dialog open={isOpen} onOpenChange={(o) => (!o ? onClose() : undefined)}>
      <DialogContent className="!max-w-lg">
        <form onSubmit={onSubmit}>
          <DialogHeader>
            <DialogTitle>{company ? "Edit insurance company" : "Add an insurance company"}</DialogTitle>
            <DialogDescription>
              {company ? `Update details for ${company.name}.` : "Add a payer to your organization."}
            </DialogDescription>
          </DialogHeader>

          <DialogBody className="space-y-5">
            <Field id="ins-co-name" label="Name" required>
              <Input
                id="ins-co-name"
                value={form.name}
                onChange={(e) => set("name", e.target.value)}
                placeholder="Blue Shield"
                autoFocus
                required
                maxLength={200}
              />
            </Field>

            <div className="grid grid-cols-2 gap-4">
              <Field id="ins-co-type" label="Insurance type">
                <Combobox
                  id="ins-co-type"
                  label="Insurance type"
                  variant="field"
                  searchable
                  clearable
                  emptyOptionLabel="No type"
                  placeholder="Select a type…"
                  value={form.insuranceTypeId}
                  onChange={(v) => set("insuranceTypeId", v)}
                  options={typeOptions}
                />
              </Field>
              <Field id="ins-co-tiers" label="Formulary tiers" hint="0–20 drug tiers.">
                <Input
                  id="ins-co-tiers"
                  type="number"
                  min={0}
                  max={20}
                  value={form.formularyTiers}
                  onChange={(e) => set("formularyTiers", Math.min(20, Math.max(0, Number(e.target.value) || 0)))}
                />
              </Field>
            </div>

            <Field id="ins-co-address1" label="Address line 1">
              <Input id="ins-co-address1" value={form.address1} onChange={(e) => set("address1", e.target.value)} placeholder="1 Market St" maxLength={100} />
            </Field>
            <Field id="ins-co-address2" label="Address line 2">
              <Input id="ins-co-address2" value={form.address2} onChange={(e) => set("address2", e.target.value)} placeholder="Suite 100" maxLength={100} />
            </Field>

            <div className="grid grid-cols-[1fr_90px_120px] gap-4">
              <Field id="ins-co-city" label="City">
                <Input id="ins-co-city" value={form.city} onChange={(e) => set("city", e.target.value)} placeholder="San Francisco" maxLength={100} />
              </Field>
              <Field id="ins-co-state" label="State">
                <Input id="ins-co-state" value={form.state} onChange={(e) => set("state", e.target.value)} placeholder="CA" maxLength={50} />
              </Field>
              <Field id="ins-co-zip" label="ZIP">
                <Input id="ins-co-zip" value={form.zip} onChange={(e) => set("zip", e.target.value)} placeholder="94105" maxLength={10} />
              </Field>
            </div>

            <Field id="ins-co-phone" label="Phone">
              <Input id="ins-co-phone" value={form.phone} onChange={(e) => set("phone", e.target.value)} placeholder="555-2000" maxLength={20} type="tel" />
            </Field>

            {company && (
              <div className="flex items-center justify-between rounded-lg border border-[var(--color-border)] px-3 py-2.5">
                <div>
                  <p className="text-[13px] font-medium text-[var(--color-foreground)]">Active</p>
                  <p className="text-[12px] text-[var(--color-muted-foreground)]">
                    Inactive companies are hidden from billing selection lists.
                  </p>
                </div>
                <Switch checked={form.isActive} onCheckedChange={(v) => set("isActive", v)} aria-label="Insurance company active" />
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
              {isPending ? "Saving…" : company ? "Save changes" : "Add company"}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}

function DeleteInsuranceCompanyDialog({ state, onClose }: { state: EditorState; onClose: () => void }) {
  const isOpen = state.mode === "delete";
  const company = state.mode === "delete" ? state.company : undefined;
  const queryClient = useQueryClient();

  const deleteMutation = useMutation({
    mutationFn: (id: string) => deleteInsuranceCompany(id),
    onSuccess: () => {
      toast.success("Insurance company deleted");
      queryClient.invalidateQueries({ queryKey: ["administration", "insurance-companies"] });
      onClose();
    },
    onError: (err) => toast.error("Delete failed", { description: describe(err) }),
  });

  return (
    <Dialog open={isOpen} onOpenChange={(o) => (!o ? onClose() : undefined)}>
      <DialogContent>
        <DialogHeader>
          <DialogTitle className="text-[var(--color-destructive)]">Delete insurance company</DialogTitle>
          <DialogDescription>
            This removes{" "}
            <span className="font-medium text-[var(--color-foreground)]">{company?.name}</span>. Records referencing
            this payer may need to be reassigned.
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
            onClick={() => company && deleteMutation.mutate(company.id)}
            disabled={deleteMutation.isPending || !company}
          >
            {deleteMutation.isPending ? "Deleting…" : "Delete company"}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
