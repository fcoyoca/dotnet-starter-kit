import { useEffect, useMemo, useState } from "react";
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { Database, Pencil, Plus, Trash2 } from "lucide-react";
import type { LookupItemDto, CreateLookupInput, UpdateLookupInput } from "@/api/administration";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Switch } from "@/components/ui/switch";
import { ConfirmDialog } from "@/components/ui/confirm-dialog";
import { EntityPageHeader, ErrorBand, LoadingRow } from "@/components/list";
import { EmptyState } from "@/components/empty-state";
import { ApiRequestError } from "@/lib/api-client";
import { cn } from "@/lib/cn";
import {
  Dialog,
  DialogBody,
  DialogContent,
  DialogFooter,
  DialogHeader,
  DialogTitle,
  DialogDescription,
} from "@/components/ui/dialog";

type ApiClient = {
  list: (isActive?: boolean) => Promise<LookupItemDto[]>;
  create: (input: CreateLookupInput) => Promise<number>;
  update: (id: number, input: Omit<UpdateLookupInput, "id">) => Promise<void>;
  remove: (id: number) => Promise<void>;
};

type LookupListPageProps = {
  queryKey: string;
  label: string;
  labelPlural: string;
  api: ApiClient;
  withSnomedCode?: boolean;
  canCreate?: boolean;
  canUpdate?: boolean;
  canDelete?: boolean;
};

export function LookupListPage({
  queryKey,
  label,
  labelPlural,
  api,
  withSnomedCode = false,
  canCreate = false,
  canUpdate = false,
  canDelete = false,
}: LookupListPageProps) {
  const queryClient = useQueryClient();

  const [search, setSearch] = useState("");
  const [debounced, setDebounced] = useState("");
  const [createOpen, setCreateOpen] = useState(false);
  const [editItem, setEditItem] = useState<LookupItemDto | null>(null);
  const [deleteItem, setDeleteItem] = useState<LookupItemDto | null>(null);

  useEffect(() => {
    const t = setTimeout(() => setDebounced(search.trim().toLowerCase()), 200);
    return () => clearTimeout(t);
  }, [search]);

  const query = useQuery({ queryKey: [queryKey], queryFn: () => api.list() });

  const items = useMemo(() => {
    const all = query.data ?? [];
    const sorted = [...all].sort((a, b) => a.name.localeCompare(b.name));
    if (!debounced) return sorted;
    return sorted.filter((item) => item.name.toLowerCase().includes(debounced));
  }, [query.data, debounced]);

  const createMutation = useMutation({
    mutationFn: (input: CreateLookupInput) => api.create(input),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: [queryKey] });
      setCreateOpen(false);
    },
  });

  const updateMutation = useMutation({
    mutationFn: ({ id, ...rest }: UpdateLookupInput) => api.update(id, rest),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: [queryKey] });
      setEditItem(null);
    },
  });

  const deleteMutation = useMutation({
    mutationFn: (id: number) => api.remove(id),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: [queryKey] });
      setDeleteItem(null);
    },
  });

  const searchActive = debounced.length > 0;

  return (
    <div className="space-y-4 sm:space-y-6">
      <EntityPageHeader
        icon={Database}
        title={labelPlural}
        total={query.data ? items.length : null}
        unit={label.toLowerCase()}
        description={`Manage ${labelPlural.toLowerCase()} used across the platform.`}
      >
        {canCreate && (
          <Button
            onClick={() => setCreateOpen(true)}
            className="h-9 flex-1 gap-1.5 rounded-lg px-4 text-[13px] font-semibold sm:flex-none"
          >
            <Plus className="size-4" />
            New {label.toLowerCase()}
          </Button>
        )}
      </EntityPageHeader>

      <div className="relative w-full max-w-sm">
        <span className="pointer-events-none absolute left-3 top-1/2 -translate-y-1/2 text-[var(--color-muted-foreground)]">
          <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" aria-hidden><circle cx="11" cy="11" r="8"/><path d="m21 21-4.35-4.35"/></svg>
        </span>
        <input
          type="search"
          value={search}
          onChange={(e) => setSearch(e.target.value)}
          placeholder={`Search ${labelPlural.toLowerCase()}…`}
          aria-label={`Search ${labelPlural.toLowerCase()}`}
          className="h-9 w-full rounded-md border border-[var(--color-input)] bg-transparent pl-9 pr-3 text-[13px] outline-none transition-colors placeholder:text-[oklch(from_var(--color-muted-foreground)_l_c_h_/_0.7)] focus-visible:border-[var(--color-ring)] focus-visible:ring-[3px] focus-visible:ring-[oklch(from_var(--color-ring)_l_c_h_/_0.5)]"
        />
      </div>

      {query.isError && (
        <ErrorBand
          message={
            query.error instanceof ApiRequestError
              ? query.error.problem?.detail ?? query.error.message
              : `Failed to load ${labelPlural.toLowerCase()}.`
          }
        />
      )}

      {query.isLoading && <LoadingRow label={`Loading ${labelPlural.toLowerCase()}`} />}

      {!query.isLoading && items.length === 0 && !query.isError && (
        searchActive ? (
          <div className="py-16 text-center">
            <p className="font-display text-2xl">No results found.</p>
            <p className="mt-1 text-sm text-[var(--color-muted-foreground)]">
              Nothing matches &ldquo;{debounced}&rdquo;. Try a different term.
            </p>
            <Button
              variant="outline"
              className="mt-4 h-9 rounded-lg px-4 text-[13px]"
              onClick={() => setSearch("")}
            >
              Clear search
            </Button>
          </div>
        ) : (
          <EmptyState
            icon={Database}
            kicker="// no entries"
            title={`No ${labelPlural.toLowerCase()} yet.`}
            description={`Add the first ${label.toLowerCase()} to get started.`}
            action={
              canCreate ? (
                <Button onClick={() => setCreateOpen(true)} className="h-9 rounded-lg px-4 text-[13px]">
                  <Plus className="mr-1.5 h-4 w-4" /> New {label.toLowerCase()}
                </Button>
              ) : undefined
            }
          />
        )
      )}

      {items.length > 0 && (
        <div>
          <p className="mb-3 text-[12px] font-medium text-[var(--color-muted-foreground)]">
            {items.length} {items.length !== 1 ? labelPlural.toLowerCase() : label.toLowerCase()}
          </p>

          <div className="overflow-hidden rounded-xl border border-[var(--color-border)] bg-[var(--color-card)] shadow-xs">
            <div className="grid grid-cols-[1fr_80px_80px] items-center gap-3 border-b border-[var(--color-border)] bg-[var(--color-muted)]/40 px-4 py-2.5">
              <span className="text-[11.5px] font-semibold uppercase tracking-wider text-[var(--color-muted-foreground)]">
                Name{withSnomedCode ? " / SNOMED" : ""}
              </span>
              <span className="text-center text-[11.5px] font-semibold uppercase tracking-wider text-[var(--color-muted-foreground)]">
                Status
              </span>
              <span />
            </div>

            <ol className="divide-y divide-[var(--color-border)]">
              {items.map((item) => (
                <LookupRow
                  key={item.id}
                  item={item}
                  withSnomedCode={withSnomedCode}
                  canUpdate={canUpdate}
                  canDelete={canDelete}
                  onEdit={() => setEditItem(item)}
                  onDelete={() => setDeleteItem(item)}
                />
              ))}
            </ol>
          </div>
        </div>
      )}

      {/* Create dialog */}
      <LookupFormDialog
        open={createOpen}
        onOpenChange={setCreateOpen}
        title={`New ${label}`}
        withSnomedCode={withSnomedCode}
        pending={createMutation.isPending}
        error={createMutation.error instanceof ApiRequestError ? createMutation.error.problem?.detail : undefined}
        onSave={(values) => createMutation.mutate({ name: values.name, snomedCode: values.snomedCode ?? null })}
      />

      {/* Edit dialog */}
      {editItem && (
        <LookupFormDialog
          open={editItem !== null}
          onOpenChange={(open) => { if (!open) setEditItem(null); }}
          title={`Edit ${label}`}
          withSnomedCode={withSnomedCode}
          initialName={editItem.name}
          initialIsActive={editItem.isActive}
          initialSnomedCode={editItem.snomedCode ?? ""}
          showIsActive
          pending={updateMutation.isPending}
          error={updateMutation.error instanceof ApiRequestError ? updateMutation.error.problem?.detail : undefined}
          onSave={(values) =>
            updateMutation.mutate({
              id: editItem.id,
              name: values.name,
              isActive: values.isActive,
              snomedCode: values.snomedCode ?? null,
            })
          }
        />
      )}

      {/* Delete confirm */}
      <ConfirmDialog
        open={deleteItem !== null}
        onOpenChange={(open) => { if (!open) setDeleteItem(null); }}
        title={`Delete ${label}`}
        description={
          <>
            Are you sure you want to delete <strong>{deleteItem?.name}</strong>? This action cannot be undone.
          </>
        }
        confirmLabel="Delete"
        destructive
        pending={deleteMutation.isPending}
        onConfirm={() => { if (deleteItem) deleteMutation.mutate(deleteItem.id); }}
      />
    </div>
  );
}

// ─── Row ──────────────────────────────────────────────────────────────────────

function LookupRow({
  item,
  withSnomedCode,
  canUpdate,
  canDelete,
  onEdit,
  onDelete,
}: {
  item: LookupItemDto;
  withSnomedCode: boolean;
  canUpdate: boolean;
  canDelete: boolean;
  onEdit: () => void;
  onDelete: () => void;
}) {
  return (
    <li className="list-none">
      <div
        className={cn(
          "grid grid-cols-[1fr_80px_80px] items-center gap-3 px-4 py-3",
          !item.isActive && "opacity-60",
        )}
      >
        <div className="min-w-0">
          <span className="block truncate text-[14px] font-medium text-[var(--color-foreground)]">
            {item.name}
          </span>
          {withSnomedCode && item.snomedCode && (
            <span className="block truncate font-mono text-[11px] text-[var(--color-muted-foreground)]">
              {item.snomedCode}
            </span>
          )}
        </div>

        <div className="flex justify-center">
          <span
            className={cn(
              "inline-flex items-center rounded-full px-1.5 py-0.5 text-[10.5px] font-medium",
              item.isActive
                ? "bg-[oklch(from_var(--color-success)_l_c_h_/_0.12)] text-[var(--color-success)]"
                : "bg-[var(--color-muted)] text-[var(--color-muted-foreground)]",
            )}
          >
            {item.isActive ? "Active" : "Inactive"}
          </span>
        </div>

        <div className="flex items-center justify-end gap-1">
          {canUpdate && (
            <button
              type="button"
              onClick={onEdit}
              aria-label={`Edit ${item.name}`}
              className="grid h-7 w-7 place-items-center rounded-md text-[var(--color-muted-foreground)] transition-colors hover:bg-[var(--color-accent)] hover:text-[var(--color-foreground)] focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-[var(--color-ring)]"
            >
              <Pencil className="size-3.5" />
            </button>
          )}
          {canDelete && (
            <button
              type="button"
              onClick={onDelete}
              aria-label={`Delete ${item.name}`}
              className="grid h-7 w-7 place-items-center rounded-md text-[var(--color-muted-foreground)] transition-colors hover:bg-[oklch(from_var(--color-destructive)_l_c_h_/_0.10)] hover:text-[var(--color-destructive)] focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-[var(--color-ring)]"
            >
              <Trash2 className="size-3.5" />
            </button>
          )}
        </div>
      </div>
    </li>
  );
}

// ─── Form dialog ──────────────────────────────────────────────────────────────

type FormValues = { name: string; isActive: boolean; snomedCode: string };

function LookupFormDialog({
  open,
  onOpenChange,
  title,
  withSnomedCode,
  initialName = "",
  initialIsActive = true,
  initialSnomedCode = "",
  showIsActive = false,
  pending,
  error,
  onSave,
}: {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  title: string;
  withSnomedCode: boolean;
  initialName?: string;
  initialIsActive?: boolean;
  initialSnomedCode?: string;
  showIsActive?: boolean;
  pending: boolean;
  error?: string;
  onSave: (values: FormValues) => void;
}) {
  const [name, setName] = useState(initialName);
  const [isActive, setIsActive] = useState(initialIsActive);
  const [snomedCode, setSnomedCode] = useState(initialSnomedCode);

  useEffect(() => {
    if (open) {
      setName(initialName);
      setIsActive(initialIsActive);
      setSnomedCode(initialSnomedCode);
    }
  }, [open, initialName, initialIsActive, initialSnomedCode]);

  function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    if (!name.trim()) return;
    onSave({ name: name.trim(), isActive, snomedCode: snomedCode.trim() });
  }

  return (
    <Dialog open={open} onOpenChange={(o) => { if (!pending) onOpenChange(o); }}>
      <DialogContent size="sm">
        <form onSubmit={handleSubmit}>
          <DialogHeader>
            <DialogTitle>{title}</DialogTitle>
            {error && (
              <DialogDescription className="text-[var(--color-destructive)]">
                {error}
              </DialogDescription>
            )}
          </DialogHeader>

          <DialogBody className="space-y-4">
            <div className="space-y-1.5">
              <Label htmlFor="lookup-name">Name</Label>
              <Input
                id="lookup-name"
                value={name}
                onChange={(e) => setName(e.target.value)}
                placeholder="Enter name…"
                required
                autoFocus
                disabled={pending}
              />
            </div>

            {withSnomedCode && (
              <div className="space-y-1.5">
                <Label htmlFor="lookup-snomed">SNOMED Code</Label>
                <Input
                  id="lookup-snomed"
                  value={snomedCode}
                  onChange={(e) => setSnomedCode(e.target.value)}
                  placeholder="Optional SNOMED CT code…"
                  disabled={pending}
                />
              </div>
            )}

            {showIsActive && (
              <div className="flex items-center justify-between rounded-lg border border-[var(--color-border)] px-3 py-2.5">
                <Label htmlFor="lookup-active" className="cursor-pointer select-none">
                  Active
                </Label>
                <Switch
                  id="lookup-active"
                  checked={isActive}
                  onCheckedChange={setIsActive}
                  disabled={pending}
                />
              </div>
            )}
          </DialogBody>

          <DialogFooter>
            <Button type="button" variant="outline" onClick={() => onOpenChange(false)} disabled={pending}>
              Cancel
            </Button>
            <Button type="submit" disabled={pending || !name.trim()}>
              {pending ? "Saving…" : "Save"}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}
