import { useEffect, useMemo, useState, type FormEvent } from "react";
import {
  keepPreviousData,
  useMutation,
  useQuery,
  useQueryClient,
} from "@tanstack/react-query";
import { ChevronRight, Pencil, Plus, Search, Stethoscope, Trash2 } from "lucide-react";
import { toast } from "sonner";
import {
  createProvider,
  deleteProvider,
  listProviders,
  updateProvider,
  useClinicOptions,
  type ProviderDto,
  type CreateProviderInput,
  type UpdateProviderInput,
} from "@/api/administration";
import { searchUsers } from "@/api/identity";
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
  type ComboboxOption,
} from "@/components/list";
import { describe } from "@/lib/list-helpers";

const PAGE_SIZE = 20;

type EditorState =
  | { mode: "closed" }
  | { mode: "create" }
  | { mode: "edit"; provider: ProviderDto }
  | { mode: "delete"; provider: ProviderDto };

function fullName(p: ProviderDto): string {
  return [p.prefix, p.firstName, p.lastName, p.suffix].filter(Boolean).join(" ");
}

/**
 * Tenant users as combobox options for the optional provider→user link. Loads the
 * tenant's users (no active-only filter, so a provider can be linked to any account)
 * and exposes load/error state so the picker never shows a silent empty box.
 *
 * PageSize is capped at 100 — the Identity SearchUsers endpoint rejects anything
 * larger (PagedQueryValidator: PageSize InclusiveBetween(1, 100)) with a 400, which
 * is what made an over-large request come back empty.
 */
function useUserOptions(): { options: ComboboxOption[]; isLoading: boolean; isError: boolean } {
  const { data, isLoading, isError } = useQuery({
    queryKey: ["administration", "providerUserOptions"],
    queryFn: () => searchUsers({ pageNumber: 1, pageSize: 100, sort: "userName asc" }),
    staleTime: 5 * 60 * 1000,
  });
  const options = (data?.items ?? [])
    .filter((u): u is typeof u & { id: string } => Boolean(u.id))
    .map((u) => ({
      value: u.id,
      label: [u.firstName, u.lastName].filter(Boolean).join(" ") || u.userName || u.email || u.id,
      hint: u.email ?? undefined,
    }));
  return { options, isLoading, isError };
}

export function ProvidersPage() {
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
    queryKey: ["administration", "providers", { search: debouncedSearch, pageNumber, pageSize: PAGE_SIZE }],
    queryFn: () =>
      listProviders({
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
        icon={Stethoscope}
        title="Providers"
        total={data?.totalCount ?? null}
        unit="provider"
        description="Manage the clinical providers in your organization — their specialty, NPI, primary clinic, and optional login account."
      >
        <Button
          onClick={() => setEditor({ mode: "create" })}
          className="h-9 flex-1 gap-1.5 rounded-lg px-4 text-[13px] font-semibold sm:flex-none"
        >
          <Plus className="size-4" />
          New provider
        </Button>
      </EntityPageHeader>

      <EntitySearch value={search} onChange={setSearch} placeholder="Search by name, NPI, or specialty…" />

      {query.isLoading && items.length === 0 ? (
        <EntityListLoading desktopColumns="grid-cols-[1fr_140px_160px_90px_24px]" />
      ) : items.length === 0 ? (
        <EntityEmpty
          icon={searchActive ? Search : Stethoscope}
          title={searchActive ? "No providers found" : "No providers yet"}
          body={
            searchActive
              ? `Nothing matches "${debouncedSearch}". Try a different term or clear the search.`
              : "Add your first provider to start scheduling and billing under their identity."
          }
          action={
            searchActive ? (
              <Button variant="outline" onClick={() => setSearch("")} className="h-9 rounded-lg px-4 text-[13px]">
                Clear search
              </Button>
            ) : (
              <Button onClick={() => setEditor({ mode: "create" })} className="h-9 rounded-lg px-4 text-[13px]">
                <Plus className="mr-1.5 size-4" />
                Add provider
              </Button>
            )
          }
        />
      ) : (
        <div>
          <div className="mb-3 flex items-center justify-between">
            <p className="text-[12px] font-medium text-[var(--color-muted-foreground)]">
              {data?.totalCount ?? 0} provider{(data?.totalCount ?? 0) !== 1 ? "s" : ""} found
            </p>
          </div>

          <div className="space-y-2 md:hidden">
            {items.map((p) => (
              <MobileCard key={p.id} provider={p} onEdit={() => setEditor({ mode: "edit", provider: p })} />
            ))}
          </div>

          <EntityListCard className="hidden md:block">
            <EntityListHeader className="grid-cols-[1fr_140px_160px_90px_24px]">
              <span>Provider</span>
              <span>NPI</span>
              <span>Primary clinic</span>
              <span>Status</span>
              <span />
            </EntityListHeader>
            {items.map((p, i) => (
              <DesktopRow
                key={p.id}
                provider={p}
                isLast={i === items.length - 1}
                onEdit={() => setEditor({ mode: "edit", provider: p })}
                onDelete={() => setEditor({ mode: "delete", provider: p })}
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

      <ProviderEditorDialog state={editor} onClose={() => setEditor({ mode: "closed" })} />
      <DeleteProviderDialog state={editor} onClose={() => setEditor({ mode: "closed" })} />
    </div>
  );
}

function MobileCard({ provider, onEdit }: { provider: ProviderDto; onEdit: () => void }) {
  return (
    <EntityMobileCard
      href="#"
      onClick={(e) => {
        e.preventDefault();
        onEdit();
      }}
      aria-label={`Edit provider ${fullName(provider)}`}
    >
      <div className="flex items-center justify-between">
        <div className="flex min-w-0 items-center gap-3">
          <EntityInitialsAvatar name={`${provider.firstName} ${provider.lastName}`} size={40} />
          <div className="min-w-0">
            <p className="truncate text-[14px] font-medium text-[var(--color-foreground)]">{fullName(provider)}</p>
            {provider.specialty && (
              <p className="mt-0.5 truncate text-[12px] text-[var(--color-muted-foreground)]">{provider.specialty}</p>
            )}
          </div>
        </div>
        <EntityStatusBadge tone={provider.isActive ? "success" : "default"}>
          {provider.isActive ? "Active" : "Inactive"}
        </EntityStatusBadge>
      </div>
    </EntityMobileCard>
  );
}

function DesktopRow({
  provider,
  isLast,
  onEdit,
  onDelete,
}: {
  provider: ProviderDto;
  isLast: boolean;
  onEdit: () => void;
  onDelete: () => void;
}) {
  return (
    <EntityListRow className="grid-cols-[1fr_140px_160px_90px_24px]" isLast={isLast}>
      <div className="flex min-w-0 items-center gap-3">
        <EntityInitialsAvatar name={`${provider.firstName} ${provider.lastName}`} size={36} />
        <div className="min-w-0">
          <div className="truncate text-[14px] font-medium text-[var(--color-foreground)] transition-colors group-hover:text-[var(--color-primary)]">
            {fullName(provider)}
          </div>
          {provider.specialty && (
            <div className="mt-0.5 truncate text-[12px] text-[var(--color-muted-foreground)]">{provider.specialty}</div>
          )}
        </div>
      </div>
      <code className="truncate font-mono text-[12px] text-[var(--color-muted-foreground)]">
        {provider.npi ?? "—"}
      </code>
      <div className="min-w-0 truncate text-[12px] text-[var(--color-muted-foreground)]">
        {provider.primaryClinicName ?? "—"}
      </div>
      <div className="flex items-center">
        <EntityStatusBadge tone={provider.isActive ? "success" : "default"}>
          {provider.isActive ? "Active" : "Inactive"}
        </EntityStatusBadge>
      </div>
      <div className="flex items-center justify-end gap-1">
        <button
          type="button"
          aria-label={`Edit ${fullName(provider)}`}
          onClick={onEdit}
          className="grid size-7 cursor-pointer place-items-center rounded-md text-[var(--color-muted-foreground)] opacity-0 transition-all hover:bg-[var(--color-muted)] hover:text-[var(--color-foreground)] group-hover:opacity-100"
        >
          <Pencil className="size-3.5" />
        </button>
        <button
          type="button"
          aria-label={`Delete ${fullName(provider)}`}
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

function ProviderEditorDialog({ state, onClose }: { state: EditorState; onClose: () => void }) {
  const isOpen = state.mode === "create" || state.mode === "edit";
  const provider = state.mode === "edit" ? state.provider : undefined;
  const queryClient = useQueryClient();
  const clinicOptions = useClinicOptions() ?? [];
  const { options: userOptions, isLoading: usersLoading, isError: usersError } = useUserOptions();

  const initial = useMemo(
    () => ({
      firstName: provider?.firstName ?? "",
      lastName: provider?.lastName ?? "",
      prefix: provider?.prefix ?? "",
      suffix: provider?.suffix ?? "",
      specialty: provider?.specialty ?? "",
      npi: provider?.npi ?? "",
      kareoExternalId: provider?.kareoExternalId ?? "",
      primaryClinicId: provider?.primaryClinicId ?? null,
      userId: provider?.userId ?? null,
      isActive: provider?.isActive ?? true,
    }),
    [provider],
  );

  const [form, setForm] = useState(initial);
  useEffect(() => {
    if (isOpen) setForm(initial);
  }, [isOpen, initial]);

  const set = <K extends keyof typeof form>(key: K, value: (typeof form)[K]) =>
    setForm((f) => ({ ...f, [key]: value }));

  const invalidate = () => queryClient.invalidateQueries({ queryKey: ["administration", "providers"] });

  const createMutation = useMutation({
    mutationFn: (input: CreateProviderInput) => createProvider(input),
    onSuccess: () => {
      toast.success("Provider created");
      invalidate();
      onClose();
    },
    onError: (err) => toast.error("Create failed", { description: describe(err) }),
  });

  const updateMutation = useMutation({
    mutationFn: (input: UpdateProviderInput) => updateProvider(input),
    onSuccess: () => {
      toast.success("Provider updated");
      invalidate();
      onClose();
    },
    onError: (err) => toast.error("Update failed", { description: describe(err) }),
  });

  const isPending = createMutation.isPending || updateMutation.isPending;
  const npiValid = !form.npi.trim() || /^[0-9]{10}$/.test(form.npi.trim());
  const valid = form.firstName.trim() && form.lastName.trim() && npiValid;

  const onSubmit = (e: FormEvent<HTMLFormElement>) => {
    e.preventDefault();
    if (!valid) return;
    const payload = {
      firstName: form.firstName.trim(),
      lastName: form.lastName.trim(),
      prefix: form.prefix.trim() || null,
      suffix: form.suffix.trim() || null,
      specialty: form.specialty.trim() || null,
      npi: form.npi.trim() || null,
      kareoExternalId: form.kareoExternalId.trim() || null,
      primaryClinicId: form.primaryClinicId,
      userId: form.userId,
    };
    if (state.mode === "edit" && provider) {
      updateMutation.mutate({ providerId: provider.id, isActive: form.isActive, ...payload });
    } else {
      createMutation.mutate(payload);
    }
  };

  return (
    <Dialog open={isOpen} onOpenChange={(o) => (!o ? onClose() : undefined)}>
      <DialogContent className="!max-w-lg">
        <form onSubmit={onSubmit}>
          <DialogHeader>
            <DialogTitle>{provider ? "Edit provider" : "Add a provider"}</DialogTitle>
            <DialogDescription>
              {provider
                ? `Update details for ${fullName(provider)}.`
                : "Add a clinical provider to your organization."}
            </DialogDescription>
          </DialogHeader>

          <DialogBody className="space-y-5">
            <div className="grid grid-cols-[80px_1fr_1fr_80px] gap-3">
              <Field id="prov-prefix" label="Prefix">
                <Input id="prov-prefix" value={form.prefix} onChange={(e) => set("prefix", e.target.value)} placeholder="Dr." maxLength={20} />
              </Field>
              <Field id="prov-first" label="First name" required>
                <Input id="prov-first" value={form.firstName} onChange={(e) => set("firstName", e.target.value)} placeholder="Gregory" autoFocus required maxLength={100} />
              </Field>
              <Field id="prov-last" label="Last name" required>
                <Input id="prov-last" value={form.lastName} onChange={(e) => set("lastName", e.target.value)} placeholder="House" required maxLength={100} />
              </Field>
              <Field id="prov-suffix" label="Suffix">
                <Input id="prov-suffix" value={form.suffix} onChange={(e) => set("suffix", e.target.value)} placeholder="MD" maxLength={20} />
              </Field>
            </div>

            <div className="grid grid-cols-2 gap-4">
              <Field id="prov-specialty" label="Specialty">
                <Input id="prov-specialty" value={form.specialty} onChange={(e) => set("specialty", e.target.value)} placeholder="Cardiology" maxLength={150} />
              </Field>
              <Field id="prov-npi" label="NPI" hint={form.npi.trim() && !npiValid ? "NPI must be exactly 10 digits." : "10 digits"}>
                <Input id="prov-npi" value={form.npi} onChange={(e) => set("npi", e.target.value)} placeholder="1234567890" maxLength={10} inputMode="numeric" aria-invalid={form.npi.trim() !== "" && !npiValid} />
              </Field>
            </div>

            <Field id="prov-clinic" label="Primary clinic">
              <Combobox
                id="prov-clinic"
                label="Primary clinic"
                variant="field"
                searchable
                clearable
                emptyOptionLabel="No primary clinic"
                placeholder="Select a clinic…"
                value={form.primaryClinicId}
                onChange={(v) => set("primaryClinicId", v)}
                options={clinicOptions}
              />
            </Field>

            <Field
              id="prov-user"
              label="Linked user account"
              hint={
                usersError
                  ? "Couldn't load users — you may not have permission to view them."
                  : usersLoading
                    ? "Loading users…"
                    : userOptions.length === 0
                      ? "No user accounts found in this organization yet."
                      : "Optional — link this provider to a login account."
              }
            >
              <Combobox
                id="prov-user"
                label="Linked user account"
                variant="field"
                searchable
                clearable
                emptyOptionLabel="No linked account"
                placeholder={usersLoading ? "Loading users…" : "Select a user…"}
                value={form.userId}
                onChange={(v) => set("userId", v)}
                options={userOptions}
                disabled={usersLoading}
              />
            </Field>

            <Field id="prov-kareo" label="Kareo / Tebra ID" hint="Optional external billing provider id.">
              <Input id="prov-kareo" value={form.kareoExternalId} onChange={(e) => set("kareoExternalId", e.target.value)} placeholder="e.g. 12345" maxLength={64} />
            </Field>

            {provider && (
              <div className="flex items-center justify-between rounded-lg border border-[var(--color-border)] px-3 py-2.5">
                <div>
                  <p className="text-[13px] font-medium text-[var(--color-foreground)]">Active</p>
                  <p className="text-[12px] text-[var(--color-muted-foreground)]">
                    Inactive providers are hidden from scheduling and selection lists.
                  </p>
                </div>
                <Switch checked={form.isActive} onCheckedChange={(v) => set("isActive", v)} aria-label="Provider active" />
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
              {isPending ? "Saving…" : provider ? "Save changes" : "Add provider"}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}

function DeleteProviderDialog({ state, onClose }: { state: EditorState; onClose: () => void }) {
  const isOpen = state.mode === "delete";
  const provider = state.mode === "delete" ? state.provider : undefined;
  const queryClient = useQueryClient();

  const deleteMutation = useMutation({
    mutationFn: (id: string) => deleteProvider(id),
    onSuccess: () => {
      toast.success("Provider deleted");
      queryClient.invalidateQueries({ queryKey: ["administration", "providers"] });
      onClose();
    },
    onError: (err) => toast.error("Delete failed", { description: describe(err) }),
  });

  return (
    <Dialog open={isOpen} onOpenChange={(o) => (!o ? onClose() : undefined)}>
      <DialogContent>
        <DialogHeader>
          <DialogTitle className="text-[var(--color-destructive)]">Delete provider</DialogTitle>
          <DialogDescription>
            This removes{" "}
            <span className="font-medium text-[var(--color-foreground)]">{provider && fullName(provider)}</span>. Any
            linked user account is left untouched.
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
            onClick={() => provider && deleteMutation.mutate(provider.id)}
            disabled={deleteMutation.isPending || !provider}
          >
            {deleteMutation.isPending ? "Deleting…" : "Delete provider"}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
