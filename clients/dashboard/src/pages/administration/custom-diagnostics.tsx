import { useEffect, useMemo, useState, type FormEvent } from "react";
import {
  keepPreviousData,
  useMutation,
  useQuery,
  useQueryClient,
} from "@tanstack/react-query";
import { ChevronRight, ClipboardPlus, Pencil, Plus, Search, Trash2 } from "lucide-react";
import { toast } from "sonner";
import {
  createCustomDiagnostic,
  deleteCustomDiagnostic,
  listCustomDiagnostics,
  updateCustomDiagnostic,
  type CustomDiagnosticDto,
  type CreateCustomDiagnosticInput,
  type UpdateCustomDiagnosticInput,
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
import { cn } from "@/lib/cn";

const textareaClass = cn(
  "flex w-full rounded-lg border border-[var(--color-input)] bg-transparent px-3 py-2 text-sm shadow-xs",
  "placeholder:text-[oklch(from_var(--color-muted-foreground)_l_c_h_/_0.6)]",
  "focus-visible:border-[var(--color-ring)] focus-visible:outline-none focus-visible:ring-[3px] focus-visible:ring-[oklch(from_var(--color-ring)_l_c_h_/_0.5)]",
);

const PAGE_SIZE = 20;

type EditorState =
  | { mode: "closed" }
  | { mode: "create" }
  | { mode: "edit"; diagnostic: CustomDiagnosticDto }
  | { mode: "delete"; diagnostic: CustomDiagnosticDto };

export function CustomDiagnosticsPage() {
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
    queryKey: ["administration", "custom-diagnostics", { search: debouncedSearch, pageNumber, pageSize: PAGE_SIZE }],
    queryFn: () => listCustomDiagnostics({ search: debouncedSearch || undefined, pageNumber, pageSize: PAGE_SIZE }),
    placeholderData: keepPreviousData,
  });

  const data = query.data;
  const items = data?.items ?? [];
  const searchActive = debouncedSearch.length > 0;

  return (
    <div className="space-y-4 sm:space-y-6">
      <EntityPageHeader
        icon={ClipboardPlus}
        title="Custom Diagnostics"
        total={data?.totalCount ?? null}
        unit="diagnostic"
        description="Tenant-defined diagnostic codes that complement the standard ICD set."
      >
        <Button
          onClick={() => setEditor({ mode: "create" })}
          className="h-9 flex-1 gap-1.5 rounded-lg px-4 text-[13px] font-semibold sm:flex-none"
        >
          <Plus className="size-4" />
          New diagnostic
        </Button>
      </EntityPageHeader>

      <EntitySearch value={search} onChange={setSearch} placeholder="Search by code or description…" />

      {query.isLoading && items.length === 0 ? (
        <EntityListLoading desktopColumns="grid-cols-[140px_1fr_70px_90px_24px]" />
      ) : items.length === 0 ? (
        <EntityEmpty
          icon={searchActive ? Search : ClipboardPlus}
          title={searchActive ? "No custom diagnostics found" : "No custom diagnostics yet"}
          body={
            searchActive
              ? `Nothing matches "${debouncedSearch}". Try a different term or clear the search.`
              : "Add your first custom diagnostic code."
          }
          action={
            searchActive ? (
              <Button variant="outline" onClick={() => setSearch("")} className="h-9 rounded-lg px-4 text-[13px]">
                Clear search
              </Button>
            ) : (
              <Button onClick={() => setEditor({ mode: "create" })} className="h-9 rounded-lg px-4 text-[13px]">
                <Plus className="mr-1.5 size-4" />
                Add diagnostic
              </Button>
            )
          }
        />
      ) : (
        <div>
          <div className="mb-3 flex items-center justify-between">
            <p className="text-[12px] font-medium text-[var(--color-muted-foreground)]">
              {data?.totalCount ?? 0} diagnostic{(data?.totalCount ?? 0) !== 1 ? "s" : ""} found
            </p>
          </div>

          <div className="space-y-2 md:hidden">
            {items.map((d) => (
              <MobileCard key={d.id} diagnostic={d} onEdit={() => setEditor({ mode: "edit", diagnostic: d })} />
            ))}
          </div>

          <EntityListCard className="hidden md:block">
            <EntityListHeader className="grid-cols-[140px_1fr_70px_90px_24px]">
              <span>Code</span>
              <span>Description</span>
              <span>Chiro</span>
              <span>Status</span>
              <span />
            </EntityListHeader>
            {items.map((d, i) => (
              <DesktopRow
                key={d.id}
                diagnostic={d}
                isLast={i === items.length - 1}
                onEdit={() => setEditor({ mode: "edit", diagnostic: d })}
                onDelete={() => setEditor({ mode: "delete", diagnostic: d })}
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

      <CustomDiagnosticEditorDialog state={editor} onClose={() => setEditor({ mode: "closed" })} />
      <DeleteCustomDiagnosticDialog state={editor} onClose={() => setEditor({ mode: "closed" })} />
    </div>
  );
}

function MobileCard({ diagnostic, onEdit }: { diagnostic: CustomDiagnosticDto; onEdit: () => void }) {
  return (
    <EntityMobileCard
      href="#"
      onClick={(e) => {
        e.preventDefault();
        onEdit();
      }}
      aria-label={`Edit custom diagnostic ${diagnostic.code}`}
    >
      <div className="flex items-center justify-between">
        <div className="flex min-w-0 items-center gap-3">
          <EntityInitialsAvatar name={diagnostic.code} size={40} />
          <div className="min-w-0">
            <p className="truncate text-[14px] font-medium text-[var(--color-foreground)]">{diagnostic.code}</p>
            {diagnostic.description && (
              <p className="mt-0.5 truncate text-[12px] text-[var(--color-muted-foreground)]">{diagnostic.description}</p>
            )}
          </div>
        </div>
        <EntityStatusBadge tone={diagnostic.isActive ? "success" : "default"}>
          {diagnostic.isActive ? "Active" : "Inactive"}
        </EntityStatusBadge>
      </div>
    </EntityMobileCard>
  );
}

function DesktopRow({
  diagnostic,
  isLast,
  onEdit,
  onDelete,
}: {
  diagnostic: CustomDiagnosticDto;
  isLast: boolean;
  onEdit: () => void;
  onDelete: () => void;
}) {
  return (
    <EntityListRow className="grid-cols-[140px_1fr_70px_90px_24px]" isLast={isLast}>
      <div className="min-w-0 truncate text-[14px] font-medium text-[var(--color-foreground)] transition-colors group-hover:text-[var(--color-primary)]">
        {diagnostic.code}
      </div>
      <div className="min-w-0 truncate text-[12px] text-[var(--color-muted-foreground)]">
        {diagnostic.description ?? "—"}
      </div>
      <div className="flex items-center text-[12px] text-[var(--color-muted-foreground)]">
        {diagnostic.isChiropractic ? "Yes" : "No"}
      </div>
      <div className="flex items-center">
        <EntityStatusBadge tone={diagnostic.isActive ? "success" : "default"}>
          {diagnostic.isActive ? "Active" : "Inactive"}
        </EntityStatusBadge>
      </div>
      <div className="flex items-center justify-end gap-1">
        <button
          type="button"
          aria-label={`Edit ${diagnostic.code}`}
          onClick={onEdit}
          className="grid size-7 cursor-pointer place-items-center rounded-md text-[var(--color-muted-foreground)] opacity-0 transition-all hover:bg-[var(--color-muted)] hover:text-[var(--color-foreground)] group-hover:opacity-100"
        >
          <Pencil className="size-3.5" />
        </button>
        <button
          type="button"
          aria-label={`Delete ${diagnostic.code}`}
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

function CustomDiagnosticEditorDialog({ state, onClose }: { state: EditorState; onClose: () => void }) {
  const isOpen = state.mode === "create" || state.mode === "edit";
  const diagnostic = state.mode === "edit" ? state.diagnostic : undefined;
  const queryClient = useQueryClient();

  const initial = useMemo(
    () => ({
      code: diagnostic?.code ?? "",
      description: diagnostic?.description ?? "",
      longDescription: diagnostic?.longDescription ?? "",
      isChiropractic: diagnostic?.isChiropractic ?? false,
      isActive: diagnostic?.isActive ?? true,
    }),
    [diagnostic],
  );

  const [form, setForm] = useState(initial);
  useEffect(() => {
    if (isOpen) setForm(initial);
  }, [isOpen, initial]);

  const set = <K extends keyof typeof form>(key: K, value: (typeof form)[K]) =>
    setForm((f) => ({ ...f, [key]: value }));

  const invalidate = () => {
    void queryClient.invalidateQueries({ queryKey: ["administration", "custom-diagnostics"] });
    // Live lookup refresh (Part B): custom diagnostics feed the diagnostic
    // codes dialog + SuperBill (["custom-diagnostics","by-ids",…]) and the
    // incident dialog's dx search (["dx-search",…]).
    void queryClient.invalidateQueries({ queryKey: ["custom-diagnostics"] });
    void queryClient.invalidateQueries({ queryKey: ["dx-search"] });
  };

  const createMutation = useMutation({
    mutationFn: (input: CreateCustomDiagnosticInput) => createCustomDiagnostic(input),
    onSuccess: () => {
      toast.success("Custom diagnostic created");
      invalidate();
      onClose();
    },
    onError: (err) => toast.error("Create failed", { description: describe(err) }),
  });

  const updateMutation = useMutation({
    mutationFn: (input: UpdateCustomDiagnosticInput) => updateCustomDiagnostic(input),
    onSuccess: () => {
      toast.success("Custom diagnostic updated");
      invalidate();
      onClose();
    },
    onError: (err) => toast.error("Update failed", { description: describe(err) }),
  });

  const isPending = createMutation.isPending || updateMutation.isPending;
  const trimmedCode = form.code.trim();

  const onSubmit = (e: FormEvent<HTMLFormElement>) => {
    e.preventDefault();
    if (!trimmedCode) return;
    const payload = {
      code: trimmedCode,
      description: form.description.trim() || null,
      longDescription: form.longDescription.trim() || null,
      isChiropractic: form.isChiropractic,
    };
    if (state.mode === "edit" && diagnostic) {
      updateMutation.mutate({ diagnosticId: diagnostic.id, isActive: form.isActive, ...payload });
    } else {
      createMutation.mutate(payload);
    }
  };

  return (
    <Dialog open={isOpen} onOpenChange={(o) => (!o ? onClose() : undefined)}>
      <DialogContent className="!max-w-lg">
        <form onSubmit={onSubmit}>
          <DialogHeader>
            <DialogTitle>{diagnostic ? "Edit custom diagnostic" : "Add a custom diagnostic"}</DialogTitle>
            <DialogDescription>
              {diagnostic ? `Update details for ${diagnostic.code}.` : "Add a tenant-defined diagnostic code."}
            </DialogDescription>
          </DialogHeader>

          <DialogBody className="space-y-5">
            <Field id="cdx-code" label="Code" required>
              <Input
                id="cdx-code"
                value={form.code}
                onChange={(e) => set("code", e.target.value)}
                placeholder="M54.5"
                autoFocus
                required
                maxLength={50}
              />
            </Field>

            <Field id="cdx-description" label="Description">
              <Input
                id="cdx-description"
                value={form.description}
                onChange={(e) => set("description", e.target.value)}
                placeholder="Low back pain"
                maxLength={500}
              />
            </Field>

            <Field id="cdx-long" label="Long description">
              <textarea
                id="cdx-long"
                value={form.longDescription}
                onChange={(e) => set("longDescription", e.target.value)}
                placeholder="Chronic low back pain, unspecified laterality…"
                rows={4}
                maxLength={2000}
                className={textareaClass}
              />
            </Field>

            <div className="flex items-center justify-between rounded-lg border border-[var(--color-border)] px-3 py-2.5">
              <div>
                <p className="text-[13px] font-medium text-[var(--color-foreground)]">Chiropractic</p>
                <p className="text-[12px] text-[var(--color-muted-foreground)]">
                  Mark codes used in chiropractic charting.
                </p>
              </div>
              <Switch
                checked={form.isChiropractic}
                onCheckedChange={(v) => set("isChiropractic", v)}
                aria-label="Chiropractic diagnostic"
              />
            </div>

            {diagnostic && (
              <div className="flex items-center justify-between rounded-lg border border-[var(--color-border)] px-3 py-2.5">
                <div>
                  <p className="text-[13px] font-medium text-[var(--color-foreground)]">Active</p>
                  <p className="text-[12px] text-[var(--color-muted-foreground)]">
                    Inactive diagnostics are hidden from selection lists.
                  </p>
                </div>
                <Switch checked={form.isActive} onCheckedChange={(v) => set("isActive", v)} aria-label="Custom diagnostic active" />
              </div>
            )}
          </DialogBody>

          <DialogFooter>
            <DialogClose asChild>
              <Button type="button" variant="outline" disabled={isPending}>
                Cancel
              </Button>
            </DialogClose>
            <Button type="submit" disabled={isPending || !trimmedCode}>
              {isPending ? "Saving…" : diagnostic ? "Save changes" : "Add diagnostic"}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}

function DeleteCustomDiagnosticDialog({ state, onClose }: { state: EditorState; onClose: () => void }) {
  const isOpen = state.mode === "delete";
  const diagnostic = state.mode === "delete" ? state.diagnostic : undefined;
  const queryClient = useQueryClient();

  const deleteMutation = useMutation({
    mutationFn: (id: string) => deleteCustomDiagnostic(id),
    onSuccess: () => {
      toast.success("Custom diagnostic deleted");
      queryClient.invalidateQueries({ queryKey: ["administration", "custom-diagnostics"] });
      queryClient.invalidateQueries({ queryKey: ["custom-diagnostics"] });
      queryClient.invalidateQueries({ queryKey: ["dx-search"] });
      onClose();
    },
    onError: (err) => toast.error("Delete failed", { description: describe(err) }),
  });

  return (
    <Dialog open={isOpen} onOpenChange={(o) => (!o ? onClose() : undefined)}>
      <DialogContent>
        <DialogHeader>
          <DialogTitle className="text-[var(--color-destructive)]">Delete custom diagnostic</DialogTitle>
          <DialogDescription>
            This removes{" "}
            <span className="font-medium text-[var(--color-foreground)]">{diagnostic?.code}</span>. Records
            referencing it may need to be reassigned.
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
            onClick={() => diagnostic && deleteMutation.mutate(diagnostic.id)}
            disabled={deleteMutation.isPending || !diagnostic}
          >
            {deleteMutation.isPending ? "Deleting…" : "Delete diagnostic"}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
