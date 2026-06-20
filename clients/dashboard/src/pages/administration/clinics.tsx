import { useEffect, useMemo, useState, type FormEvent } from "react";
import {
  keepPreviousData,
  useMutation,
  useQuery,
  useQueryClient,
} from "@tanstack/react-query";
import { Building2, ChevronRight, Pencil, Plus, Search, Trash2 } from "lucide-react";
import { toast } from "sonner";
import {
  createClinic,
  deleteClinic,
  listClinics,
  updateClinic,
  type ClinicDto,
  type CreateClinicInput,
  type UpdateClinicInput,
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
  | { mode: "edit"; clinic: ClinicDto }
  | { mode: "delete"; clinic: ClinicDto };

// ───────────────────────────────────────────────────────────────────────
//  Page
// ───────────────────────────────────────────────────────────────────────

export function ClinicsPage() {
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
    queryKey: [
      "administration",
      "clinics",
      { search: debouncedSearch, pageNumber, pageSize: PAGE_SIZE },
    ],
    queryFn: () =>
      listClinics({
        search: debouncedSearch || undefined,
        pageNumber,
        pageSize: PAGE_SIZE,
        sortBy: "name",
        sortDir: "asc",
      }),
    placeholderData: keepPreviousData,
  });

  const data = query.data;
  const items = data?.items ?? [];
  const searchActive = debouncedSearch.length > 0;

  return (
    <div className="space-y-4 sm:space-y-6">
      <EntityPageHeader
        icon={Building2}
        title="Clinics"
        total={data?.totalCount ?? null}
        unit="clinic"
        description="Manage the clinic locations belonging to your organization. Each clinic carries its own code, address, and contact number."
      >
        <Button
          onClick={() => setEditor({ mode: "create" })}
          className="h-9 flex-1 gap-1.5 rounded-lg px-4 text-[13px] font-semibold sm:flex-none"
        >
          <Plus className="size-4" />
          New clinic
        </Button>
      </EntityPageHeader>

      <EntitySearch
        value={search}
        onChange={setSearch}
        placeholder="Search by name, code, or city…"
      />

      {query.isLoading && items.length === 0 ? (
        <EntityListLoading desktopColumns="grid-cols-[1fr_120px_200px_90px_24px]" />
      ) : items.length === 0 ? (
        <EntityEmpty
          icon={searchActive ? Search : Building2}
          title={searchActive ? "No clinics found" : "No clinics yet"}
          body={
            searchActive
              ? debouncedSearch
                ? `Nothing matches "${debouncedSearch}". Try a different term or clear the search.`
                : "No clinics match the current filters."
              : "Add your first clinic to start building your organization's locations."
          }
          action={
            searchActive ? (
              <Button
                variant="outline"
                onClick={() => setSearch("")}
                className="h-9 rounded-lg px-4 text-[13px]"
              >
                Clear search
              </Button>
            ) : (
              <Button
                onClick={() => setEditor({ mode: "create" })}
                className="h-9 rounded-lg px-4 text-[13px]"
              >
                <Plus className="mr-1.5 size-4" />
                Add clinic
              </Button>
            )
          }
        />
      ) : (
        <div>
          <div className="mb-3 flex items-center justify-between">
            <p className="text-[12px] font-medium text-[var(--color-muted-foreground)]">
              {data?.totalCount ?? 0} clinic
              {(data?.totalCount ?? 0) !== 1 ? "s" : ""} found
            </p>
          </div>

          {/* Mobile: card list */}
          <div className="space-y-2 md:hidden">
            {items.map((clinic) => (
              <MobileCard
                key={clinic.id}
                clinic={clinic}
                onEdit={() => setEditor({ mode: "edit", clinic })}
              />
            ))}
          </div>

          {/* Desktop: list card */}
          <EntityListCard className="hidden md:block">
            <EntityListHeader className="grid-cols-[1fr_120px_200px_90px_24px]">
              <span>Clinic</span>
              <span>Code</span>
              <span>Location</span>
              <span>Status</span>
              <span />
            </EntityListHeader>

            {items.map((clinic, i) => (
              <DesktopRow
                key={clinic.id}
                clinic={clinic}
                isLast={i === items.length - 1}
                onEdit={() => setEditor({ mode: "edit", clinic })}
                onDelete={() => setEditor({ mode: "delete", clinic })}
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

      <ClinicEditorDialog state={editor} onClose={() => setEditor({ mode: "closed" })} />
      <DeleteClinicDialog state={editor} onClose={() => setEditor({ mode: "closed" })} />
    </div>
  );
}

// ───────────────────────────────────────────────────────────────────────
//  Mobile card
// ───────────────────────────────────────────────────────────────────────

function MobileCard({ clinic, onEdit }: { clinic: ClinicDto; onEdit: () => void }) {
  return (
    <EntityMobileCard
      href="#"
      onClick={(e) => {
        e.preventDefault();
        onEdit();
      }}
      aria-label={`Edit clinic ${clinic.name}`}
    >
      <div className="flex items-center justify-between">
        <div className="flex min-w-0 items-center gap-3">
          <EntityInitialsAvatar name={clinic.name} size={40} />
          <div className="min-w-0">
            <p className="truncate text-[14px] font-medium text-[var(--color-foreground)]">
              {clinic.name}
            </p>
            <code className="mt-0.5 block truncate font-mono text-[11px] text-[var(--color-muted-foreground)]">
              {clinic.code}
            </code>
          </div>
        </div>
        <div className="flex shrink-0 items-center gap-2">
          <EntityStatusBadge tone={clinic.isActive ? "success" : "default"}>
            {clinic.isActive ? "Active" : "Inactive"}
          </EntityStatusBadge>
          <ChevronRight className="size-4 text-[var(--color-border)]" />
        </div>
      </div>
      <p className="mt-2 ml-[52px] line-clamp-2 text-[12px] text-[var(--color-muted-foreground)]">
        {clinic.address1}, {clinic.city}, {clinic.state} {clinic.zip}
      </p>
    </EntityMobileCard>
  );
}

// ───────────────────────────────────────────────────────────────────────
//  Desktop row
// ───────────────────────────────────────────────────────────────────────

function DesktopRow({
  clinic,
  isLast,
  onEdit,
  onDelete,
}: {
  clinic: ClinicDto;
  isLast: boolean;
  onEdit: () => void;
  onDelete: () => void;
}) {
  return (
    <EntityListRow className="grid-cols-[1fr_120px_200px_90px_24px]" isLast={isLast}>
      <div className="flex min-w-0 items-center gap-3">
        <EntityInitialsAvatar name={clinic.name} size={36} />
        <div className="min-w-0">
          <div className="truncate text-[14px] font-medium text-[var(--color-foreground)] transition-colors group-hover:text-[var(--color-primary)]">
            {clinic.name}
          </div>
          {clinic.phone && (
            <div className="mt-0.5 truncate text-[12px] text-[var(--color-muted-foreground)]">
              {clinic.phone}
            </div>
          )}
        </div>
      </div>

      <code
        title={clinic.code}
        className="truncate font-mono text-[12px] text-[var(--color-muted-foreground)]"
      >
        {clinic.code}
      </code>

      <div
        className="min-w-0 truncate text-[12px] text-[var(--color-muted-foreground)]"
        title={`${clinic.address1}, ${clinic.city}, ${clinic.state} ${clinic.zip}`}
      >
        {clinic.city}, {clinic.state} {clinic.zip}
      </div>

      <div className="flex items-center">
        <EntityStatusBadge tone={clinic.isActive ? "success" : "default"}>
          {clinic.isActive ? "Active" : "Inactive"}
        </EntityStatusBadge>
      </div>

      <div className="flex items-center justify-end gap-1">
        <button
          type="button"
          aria-label={`Edit ${clinic.name}`}
          onClick={onEdit}
          className="grid size-7 cursor-pointer place-items-center rounded-md text-[var(--color-muted-foreground)] opacity-0 transition-all hover:bg-[var(--color-muted)] hover:text-[var(--color-foreground)] group-hover:opacity-100"
        >
          <Pencil className="size-3.5" />
        </button>
        <button
          type="button"
          aria-label={`Delete ${clinic.name}`}
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

// ───────────────────────────────────────────────────────────────────────
//  Editor dialog
// ───────────────────────────────────────────────────────────────────────

function ClinicEditorDialog({ state, onClose }: { state: EditorState; onClose: () => void }) {
  const isOpen = state.mode === "create" || state.mode === "edit";
  const clinic = state.mode === "edit" ? state.clinic : undefined;
  const queryClient = useQueryClient();

  const initial = useMemo(
    () => ({
      code: clinic?.code ?? "",
      name: clinic?.name ?? "",
      address1: clinic?.address1 ?? "",
      address2: clinic?.address2 ?? "",
      city: clinic?.city ?? "",
      state: clinic?.state ?? "",
      zip: clinic?.zip ?? "",
      phone: clinic?.phone ?? "",
      isActive: clinic?.isActive ?? true,
    }),
    [clinic],
  );

  const [form, setForm] = useState(initial);

  useEffect(() => {
    if (isOpen) setForm(initial);
  }, [isOpen, initial]);

  const set = <K extends keyof typeof form>(key: K, value: (typeof form)[K]) =>
    setForm((f) => ({ ...f, [key]: value }));

  const invalidate = () =>
    queryClient.invalidateQueries({ queryKey: ["administration", "clinics"] });

  const createMutation = useMutation({
    mutationFn: (input: CreateClinicInput) => createClinic(input),
    onSuccess: () => {
      toast.success("Clinic created");
      invalidate();
      onClose();
    },
    onError: (err) => toast.error("Create failed", { description: describe(err) }),
  });

  const updateMutation = useMutation({
    mutationFn: (input: UpdateClinicInput) => updateClinic(input),
    onSuccess: () => {
      toast.success("Clinic updated");
      invalidate();
      onClose();
    },
    onError: (err) => toast.error("Update failed", { description: describe(err) }),
  });

  const isPending = createMutation.isPending || updateMutation.isPending;
  const valid =
    form.code.trim() &&
    form.name.trim() &&
    form.address1.trim() &&
    form.city.trim() &&
    form.state.trim() &&
    form.zip.trim();

  const onSubmit = (e: FormEvent<HTMLFormElement>) => {
    e.preventDefault();
    if (!valid) return;
    const payload = {
      code: form.code.trim(),
      name: form.name.trim(),
      address1: form.address1.trim(),
      address2: form.address2.trim() || null,
      city: form.city.trim(),
      state: form.state.trim(),
      zip: form.zip.trim(),
      phone: form.phone.trim() || null,
    };
    if (state.mode === "edit" && clinic) {
      updateMutation.mutate({ clinicId: clinic.id, isActive: form.isActive, ...payload });
    } else {
      createMutation.mutate(payload);
    }
  };

  return (
    <Dialog open={isOpen} onOpenChange={(o) => (!o ? onClose() : undefined)}>
      <DialogContent className="!max-w-lg">
        <form onSubmit={onSubmit}>
          <DialogHeader>
            <DialogTitle>{clinic ? "Edit clinic" : "Add a clinic"}</DialogTitle>
            <DialogDescription>
              {clinic
                ? `Update details for ${clinic.name}.`
                : "Add a clinic location to your organization."}
            </DialogDescription>
          </DialogHeader>

          <DialogBody className="space-y-5">
            <div className="grid grid-cols-2 gap-4">
              <Field id="clinic-code" label="Code" required>
                <Input
                  id="clinic-code"
                  value={form.code}
                  onChange={(e) => set("code", e.target.value)}
                  placeholder="MAIN"
                  autoFocus
                  required
                  maxLength={50}
                />
              </Field>
              <Field id="clinic-name" label="Name" required>
                <Input
                  id="clinic-name"
                  value={form.name}
                  onChange={(e) => set("name", e.target.value)}
                  placeholder="Main Clinic"
                  required
                  maxLength={200}
                />
              </Field>
            </div>

            <Field id="clinic-address1" label="Address line 1" required>
              <Input
                id="clinic-address1"
                value={form.address1}
                onChange={(e) => set("address1", e.target.value)}
                placeholder="123 Main St"
                required
                maxLength={100}
              />
            </Field>

            <Field id="clinic-address2" label="Address line 2">
              <Input
                id="clinic-address2"
                value={form.address2}
                onChange={(e) => set("address2", e.target.value)}
                placeholder="Suite 200"
                maxLength={100}
              />
            </Field>

            <div className="grid grid-cols-[1fr_90px_120px] gap-4">
              <Field id="clinic-city" label="City" required>
                <Input
                  id="clinic-city"
                  value={form.city}
                  onChange={(e) => set("city", e.target.value)}
                  placeholder="Springfield"
                  required
                  maxLength={100}
                />
              </Field>
              <Field id="clinic-state" label="State" required>
                <Input
                  id="clinic-state"
                  value={form.state}
                  onChange={(e) => set("state", e.target.value)}
                  placeholder="IL"
                  required
                  maxLength={50}
                />
              </Field>
              <Field id="clinic-zip" label="ZIP" required>
                <Input
                  id="clinic-zip"
                  value={form.zip}
                  onChange={(e) => set("zip", e.target.value)}
                  placeholder="62704"
                  required
                  maxLength={10}
                />
              </Field>
            </div>

            <Field id="clinic-phone" label="Phone">
              <Input
                id="clinic-phone"
                value={form.phone}
                onChange={(e) => set("phone", e.target.value)}
                placeholder="555-1000"
                maxLength={20}
                type="tel"
              />
            </Field>

            {clinic && (
              <div className="flex items-center justify-between rounded-lg border border-[var(--color-border)] px-3 py-2.5">
                <div>
                  <p className="text-[13px] font-medium text-[var(--color-foreground)]">Active</p>
                  <p className="text-[12px] text-[var(--color-muted-foreground)]">
                    Inactive clinics are hidden from selection lists.
                  </p>
                </div>
                <Switch
                  checked={form.isActive}
                  onCheckedChange={(v) => set("isActive", v)}
                  aria-label="Clinic active"
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
            <Button type="submit" disabled={isPending || !valid}>
              {isPending ? "Saving…" : clinic ? "Save changes" : "Add clinic"}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}

// ───────────────────────────────────────────────────────────────────────
//  Delete confirmation
// ───────────────────────────────────────────────────────────────────────

function DeleteClinicDialog({ state, onClose }: { state: EditorState; onClose: () => void }) {
  const isOpen = state.mode === "delete";
  const clinic = state.mode === "delete" ? state.clinic : undefined;
  const queryClient = useQueryClient();

  const deleteMutation = useMutation({
    mutationFn: (id: string) => deleteClinic(id),
    onSuccess: () => {
      toast.success("Clinic deleted");
      queryClient.invalidateQueries({ queryKey: ["administration", "clinics"] });
      onClose();
    },
    onError: (err) => toast.error("Delete failed", { description: describe(err) }),
  });

  return (
    <Dialog open={isOpen} onOpenChange={(o) => (!o ? onClose() : undefined)}>
      <DialogContent>
        <DialogHeader>
          <DialogTitle className="text-[var(--color-destructive)]">Delete clinic</DialogTitle>
          <DialogDescription>
            This removes{" "}
            <span className="font-medium text-[var(--color-foreground)]">{clinic?.name}</span>{" "}
            <span className="opacity-70">
              (created {clinic && formatDate(clinic.createdAtUtc)})
            </span>
            . Records referencing this clinic may need to be reassigned.
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
            onClick={() => clinic && deleteMutation.mutate(clinic.id)}
            disabled={deleteMutation.isPending || !clinic}
          >
            {deleteMutation.isPending ? "Deleting…" : "Delete clinic"}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
