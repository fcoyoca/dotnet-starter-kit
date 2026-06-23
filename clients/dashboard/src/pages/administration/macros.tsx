import { useEffect, useMemo, useState, type FormEvent, type ReactNode } from "react";
import {
  keepPreviousData,
  useMutation,
  useQuery,
  useQueryClient,
} from "@tanstack/react-query";
import { ChevronRight, Layers, Pencil, Plus, ScrollText, Search, Trash2, User } from "lucide-react";
import { toast } from "sonner";
import {
  createMacro,
  createReportField,
  createReportType,
  deleteMacro,
  deleteReportField,
  deleteReportType,
  listMacros,
  listReportFields,
  listReportTypes,
  updateMacro,
  updateReportField,
  updateReportType,
  type MacroDto,
  type CreateMacroInput,
  type ReportFieldDto,
  type ReportTypeDto,
  type UpdateMacroInput,
} from "@/api/administration";
import { searchUsers, type UserDto } from "@/api/identity";
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
import { cn } from "@/lib/cn";

const PAGE_SIZE = 20;

const textareaClass = cn(
  "flex w-full rounded-lg border border-[var(--color-input)] bg-transparent px-3 py-2 text-sm shadow-xs",
  "placeholder:text-[oklch(from_var(--color-muted-foreground)_l_c_h_/_0.6)]",
  "focus-visible:border-[var(--color-ring)] focus-visible:outline-none focus-visible:ring-[3px] focus-visible:ring-[oklch(from_var(--color-ring)_l_c_h_/_0.5)]",
);

const selectClass = cn(
  "h-9 rounded-lg border border-[var(--color-input)] bg-transparent px-2 text-[13px] shadow-xs",
  "focus-visible:border-[var(--color-ring)] focus-visible:outline-none focus-visible:ring-[3px] focus-visible:ring-[oklch(from_var(--color-ring)_l_c_h_/_0.5)]",
);

/** The "All (General)" pseudo-field: macros not tied to a specific report field. */
const GENERAL = { id: null as number | null, name: "All (General)" };

function userLabel(u: UserDto): string {
  const full = `${u.firstName ?? ""} ${u.lastName ?? ""}`.trim();
  return full || u.userName || u.email || (u.id ?? "Unknown");
}

type SelectedField = { id: number | null; name: string };

type MacroEditorState =
  | { mode: "closed" }
  | { mode: "create" }
  | { mode: "edit"; macro: MacroDto }
  | { mode: "delete"; macro: MacroDto };

type TypeEditorState =
  | { mode: "closed" }
  | { mode: "create" }
  | { mode: "edit"; type: ReportTypeDto }
  | { mode: "delete"; type: ReportTypeDto };

type FieldEditorState =
  | { mode: "closed" }
  | { mode: "create" }
  | { mode: "edit"; field: ReportFieldDto }
  | { mode: "delete"; field: ReportFieldDto };

export function MacrosPage() {
  const [reportTypeId, setReportTypeId] = useState<number | null>(null);
  const [field, setField] = useState<SelectedField>(GENERAL);
  const [showInactiveFields, setShowInactiveFields] = useState(false);

  const [search, setSearch] = useState("");
  const [debouncedSearch, setDebouncedSearch] = useState("");
  const [showInactive, setShowInactive] = useState(false);
  const [ownerFilter, setOwnerFilter] = useState<string>("");
  const [pageNumber, setPageNumber] = useState(1);

  const [macroEditor, setMacroEditor] = useState<MacroEditorState>({ mode: "closed" });
  const [typeEditor, setTypeEditor] = useState<TypeEditorState>({ mode: "closed" });
  const [fieldEditor, setFieldEditor] = useState<FieldEditorState>({ mode: "closed" });

  useEffect(() => {
    const t = setTimeout(() => {
      setDebouncedSearch(search.trim());
      setPageNumber(1);
    }, 250);
    return () => clearTimeout(t);
  }, [search]);

  const typesQuery = useQuery({
    queryKey: ["administration", "report-types", "manage"],
    queryFn: () => listReportTypes(),
    staleTime: 60 * 1000,
  });

  // Default the selected type to the first one once loaded.
  const types = typesQuery.data;
  useEffect(() => {
    if (reportTypeId === null && types && types.length > 0) {
      setReportTypeId(types[0].id);
    }
  }, [types, reportTypeId]);

  const fieldsQuery = useQuery({
    queryKey: ["administration", "report-fields", reportTypeId, showInactiveFields],
    queryFn: () => listReportFields(reportTypeId as number, showInactiveFields),
    enabled: reportTypeId !== null,
    staleTime: 60 * 1000,
  });

  const usersQuery = useQuery({
    queryKey: ["identity", "users", "active-all"],
    queryFn: () => searchUsers({ isActive: true, pageSize: 200 }),
    staleTime: 5 * 60 * 1000,
  });
  const users = usersQuery.data?.items ?? [];
  const userName = (id?: string | null) =>
    (id && users.find((u) => u.id === id) && userLabel(users.find((u) => u.id === id) as UserDto)) || null;

  const macrosQuery = useQuery({
    queryKey: [
      "administration",
      "macros",
      { fieldId: field.id, search: debouncedSearch, showInactive, ownerFilter, pageNumber },
    ],
    queryFn: () =>
      listMacros({
        search: debouncedSearch || undefined,
        reportFieldId: field.id ?? undefined,
        general: field.id === null,
        useableByUserId: ownerFilter || undefined,
        isActive: showInactive ? undefined : true,
        pageNumber,
        pageSize: PAGE_SIZE,
      }),
    placeholderData: keepPreviousData,
  });

  const onSelectType = (id: number) => {
    setReportTypeId(id);
    setField(GENERAL);
    setPageNumber(1);
  };

  const onSelectField = (f: SelectedField) => {
    setField(f);
    setPageNumber(1);
  };

  const data = macrosQuery.data;
  const items = data?.items ?? [];
  const searchActive = debouncedSearch.length > 0;
  const fields = fieldsQuery.data ?? [];

  return (
    <div className="space-y-4 sm:space-y-6">
      <EntityPageHeader
        icon={ScrollText}
        title="Macros"
        description="Reusable text snippets, organized by report type and field. Manage your report types and fields here too."
      />

      {/* Report type → field master selectors (with management) */}
      <div className="grid gap-4 lg:grid-cols-2">
        <SelectorCard
          title="Report type"
          hint="Choose a report type, or manage the list."
          onAdd={() => setTypeEditor({ mode: "create" })}
        >
          {typesQuery.isLoading ? (
            <SelectorSkeleton />
          ) : (
            (typesQuery.data ?? []).map((t) => (
              <SelectorRow
                key={t.id}
                label={t.name}
                sub={t.isActive ? undefined : "Inactive"}
                selected={t.id === reportTypeId}
                onClick={() => onSelectType(t.id)}
                onEdit={() => setTypeEditor({ mode: "edit", type: t })}
                onDelete={() => setTypeEditor({ mode: "delete", type: t })}
              />
            ))
          )}
        </SelectorCard>

        <SelectorCard
          title="Fields"
          hint="Click a field to view its macros, or manage the fields."
          onAdd={reportTypeId !== null ? () => setFieldEditor({ mode: "create" }) : undefined}
          headerExtra={
            <label
              htmlFor="show-inactive-fields"
              className="flex cursor-pointer items-center gap-1.5 text-[11px] text-[var(--color-muted-foreground)]"
            >
              <Switch
                id="show-inactive-fields"
                checked={showInactiveFields}
                onCheckedChange={setShowInactiveFields}
                aria-label="Show inactive fields"
              />
              Inactive
            </label>
          }
        >
          <SelectorRow
            label={GENERAL.name}
            icon={Layers}
            selected={field.id === null}
            onClick={() => onSelectField(GENERAL)}
          />
          {fieldsQuery.isLoading ? (
            <SelectorSkeleton />
          ) : (
            fields.map((f) => (
              <SelectorRow
                key={f.id}
                label={f.name}
                sub={
                  [f.category && f.category !== f.name ? f.category : null, f.isActive ? null : "Inactive"]
                    .filter(Boolean)
                    .join(" · ") || undefined
                }
                selected={field.id === f.id}
                onClick={() => onSelectField({ id: f.id, name: f.name })}
                onEdit={() => setFieldEditor({ mode: "edit", field: f })}
                onDelete={() => setFieldEditor({ mode: "delete", field: f })}
              />
            ))
          )}
        </SelectorCard>
      </div>

      {/* Macros for the selected field */}
      <div className="rounded-xl border border-[var(--color-border)] p-4 sm:p-5">
        <div className="mb-4 flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
          <div>
            <h2 className="text-[14px] font-semibold text-[var(--color-foreground)]">
              Macros for {field.name}
            </h2>
            <p className="text-[12px] text-[var(--color-muted-foreground)]">
              {data?.totalCount ?? 0} macro{(data?.totalCount ?? 0) !== 1 ? "s" : ""}
            </p>
          </div>
          <div className="flex flex-wrap items-center gap-3">
            <select
              value={ownerFilter}
              onChange={(e) => {
                setOwnerFilter(e.target.value);
                setPageNumber(1);
              }}
              className={selectClass}
              aria-label="Filter by owner"
            >
              <option value="">All owners</option>
              {users.map((u) => (
                <option key={u.id} value={u.id}>
                  {userLabel(u)}
                </option>
              ))}
            </select>
            <label
              htmlFor="show-inactive"
              className="flex cursor-pointer items-center gap-2 text-[12px] text-[var(--color-muted-foreground)]"
            >
              <Switch
                id="show-inactive"
                checked={showInactive}
                onCheckedChange={(v) => {
                  setShowInactive(v);
                  setPageNumber(1);
                }}
                aria-label="Show inactive macros"
              />
              Show inactive
            </label>
            <Button
              onClick={() => setMacroEditor({ mode: "create" })}
              className="h-9 gap-1.5 rounded-lg px-4 text-[13px] font-semibold"
            >
              <Plus className="size-4" />
              New macro
            </Button>
          </div>
        </div>

        <EntitySearch value={search} onChange={setSearch} placeholder="Search by name…" />

        <div className="mt-4">
          {macrosQuery.isLoading && items.length === 0 ? (
            <EntityListLoading desktopColumns="grid-cols-[1fr_140px_90px_24px]" />
          ) : items.length === 0 ? (
            <EntityEmpty
              icon={searchActive ? Search : ScrollText}
              title={searchActive ? "No macros found" : "No macros here yet"}
              body={
                searchActive
                  ? `Nothing matches "${debouncedSearch}".`
                  : `Add a macro for ${field.name} to reuse boilerplate text.`
              }
              action={
                searchActive ? (
                  <Button variant="outline" onClick={() => setSearch("")} className="h-9 rounded-lg px-4 text-[13px]">
                    Clear search
                  </Button>
                ) : (
                  <Button onClick={() => setMacroEditor({ mode: "create" })} className="h-9 rounded-lg px-4 text-[13px]">
                    <Plus className="mr-1.5 size-4" />
                    Add macro
                  </Button>
                )
              }
            />
          ) : (
            <>
              <div className="space-y-2 md:hidden">
                {items.map((m) => (
                  <MobileCard
                    key={m.id}
                    macro={m}
                    owner={userName(m.useableByUserId)}
                    onEdit={() => setMacroEditor({ mode: "edit", macro: m })}
                  />
                ))}
              </div>

              <EntityListCard className="hidden md:block">
                <EntityListHeader className="grid-cols-[1fr_140px_90px_24px]">
                  <span>Macro</span>
                  <span>Useable by</span>
                  <span>Status</span>
                  <span />
                </EntityListHeader>
                {items.map((m, i) => (
                  <DesktopRow
                    key={m.id}
                    macro={m}
                    owner={userName(m.useableByUserId)}
                    isLast={i === items.length - 1}
                    onEdit={() => setMacroEditor({ mode: "edit", macro: m })}
                    onDelete={() => setMacroEditor({ mode: "delete", macro: m })}
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
            </>
          )}

          {macrosQuery.isError && (
            <div
              role="alert"
              className="mt-3 rounded-lg border border-[oklch(from_var(--color-destructive)_l_c_h_/_0.30)] bg-[oklch(from_var(--color-destructive)_l_c_h_/_0.06)] px-3 py-2 text-sm text-[var(--color-destructive)]"
            >
              {describe(macrosQuery.error)}
            </div>
          )}
        </div>
      </div>

      <MacroEditorDialog
        state={macroEditor}
        field={field}
        users={users}
        onClose={() => setMacroEditor({ mode: "closed" })}
      />
      <DeleteMacroDialog state={macroEditor} onClose={() => setMacroEditor({ mode: "closed" })} />
      <ReportTypeEditorDialog state={typeEditor} onClose={() => setTypeEditor({ mode: "closed" })} />
      <ReportFieldEditorDialog
        state={fieldEditor}
        reportTypeId={reportTypeId}
        onClose={() => setFieldEditor({ mode: "closed" })}
      />
    </div>
  );
}

function SelectorCard({
  title,
  hint,
  onAdd,
  headerExtra,
  children,
}: {
  title: string;
  hint: string;
  onAdd?: () => void;
  headerExtra?: ReactNode;
  children: ReactNode;
}) {
  return (
    <div className="rounded-xl border border-[var(--color-border)] p-3 sm:p-4">
      <div className="mb-2 flex items-start justify-between gap-2">
        <div>
          <h2 className="text-[14px] font-semibold text-[var(--color-foreground)]">{title}</h2>
          <p className="text-[12px] text-[var(--color-muted-foreground)]">{hint}</p>
        </div>
        <div className="flex items-center gap-2">
          {headerExtra}
          {onAdd ? (
            <button
              type="button"
              onClick={onAdd}
              aria-label={`Add ${title}`}
              className="grid size-7 cursor-pointer place-items-center rounded-md border border-[var(--color-border)] text-[var(--color-muted-foreground)] transition-colors hover:bg-[var(--color-muted)] hover:text-[var(--color-foreground)]"
            >
              <Plus className="size-4" />
            </button>
          ) : null}
        </div>
      </div>
      <div className="max-h-[260px] space-y-1 overflow-y-auto pr-1">{children}</div>
    </div>
  );
}

function SelectorRow({
  label,
  sub,
  icon: Icon,
  selected,
  onClick,
  onEdit,
  onDelete,
}: {
  label: string;
  sub?: string;
  icon?: typeof Layers;
  selected: boolean;
  onClick: () => void;
  onEdit?: () => void;
  onDelete?: () => void;
}) {
  return (
    <div
      className={cn(
        "group/row flex items-center gap-1 rounded-lg pr-1 transition-colors",
        selected ? "bg-[var(--color-primary)]" : "hover:bg-[var(--color-muted)]",
      )}
    >
      <button
        type="button"
        onClick={onClick}
        aria-pressed={selected}
        className={cn(
          "flex min-w-0 flex-1 cursor-pointer items-center gap-2 px-3 py-2 text-left text-[13px]",
          selected ? "text-[var(--color-primary-foreground)]" : "text-[var(--color-foreground)]",
        )}
      >
        {Icon ? <Icon className="size-3.5 shrink-0 opacity-80" /> : null}
        <span className="min-w-0 flex-1">
          <span className="block truncate font-medium">{label}</span>
          {sub ? (
            <span
              className={cn(
                "block truncate text-[11px]",
                selected ? "opacity-80" : "text-[var(--color-muted-foreground)]",
              )}
            >
              {sub}
            </span>
          ) : null}
        </span>
      </button>
      {(onEdit || onDelete) && (
        <div className="flex shrink-0 items-center gap-0.5 opacity-0 transition-opacity group-hover/row:opacity-100">
          {onEdit ? (
            <button
              type="button"
              onClick={onEdit}
              aria-label={`Edit ${label}`}
              className={cn(
                "grid size-6 cursor-pointer place-items-center rounded-md",
                selected
                  ? "text-[var(--color-primary-foreground)] hover:bg-[oklch(from_var(--color-primary-foreground)_l_c_h_/_0.2)]"
                  : "text-[var(--color-muted-foreground)] hover:bg-[var(--color-background)] hover:text-[var(--color-foreground)]",
              )}
            >
              <Pencil className="size-3" />
            </button>
          ) : null}
          {onDelete ? (
            <button
              type="button"
              onClick={onDelete}
              aria-label={`Delete ${label}`}
              className={cn(
                "grid size-6 cursor-pointer place-items-center rounded-md",
                selected
                  ? "text-[var(--color-primary-foreground)] hover:bg-[oklch(from_var(--color-primary-foreground)_l_c_h_/_0.2)]"
                  : "text-[var(--color-muted-foreground)] hover:bg-[var(--color-background)] hover:text-[var(--color-destructive)]",
              )}
            >
              <Trash2 className="size-3" />
            </button>
          ) : null}
        </div>
      )}
    </div>
  );
}

function SelectorSkeleton() {
  return (
    <div className="space-y-1.5 py-1">
      {[0, 1, 2].map((i) => (
        <div key={i} className="h-8 animate-pulse rounded-lg bg-[var(--color-muted)]" />
      ))}
    </div>
  );
}

function MobileCard({ macro, owner, onEdit }: { macro: MacroDto; owner: string | null; onEdit: () => void }) {
  return (
    <EntityMobileCard
      href="#"
      onClick={(e) => {
        e.preventDefault();
        onEdit();
      }}
      aria-label={`Edit macro ${macro.name}`}
    >
      <div className="flex items-center justify-between">
        <div className="flex min-w-0 items-center gap-3">
          <EntityInitialsAvatar name={macro.name} size={40} />
          <div className="min-w-0">
            <p className="truncate text-[14px] font-medium text-[var(--color-foreground)]">{macro.name}</p>
            <p className="truncate text-[12px] text-[var(--color-muted-foreground)]">{owner ?? "All users"}</p>
          </div>
        </div>
        <EntityStatusBadge tone={macro.isActive ? "success" : "default"}>
          {macro.isActive ? "Active" : "Inactive"}
        </EntityStatusBadge>
      </div>
    </EntityMobileCard>
  );
}

function DesktopRow({
  macro,
  owner,
  isLast,
  onEdit,
  onDelete,
}: {
  macro: MacroDto;
  owner: string | null;
  isLast: boolean;
  onEdit: () => void;
  onDelete: () => void;
}) {
  return (
    <EntityListRow className="grid-cols-[1fr_140px_90px_24px]" isLast={isLast}>
      <div className="flex min-w-0 items-center gap-3">
        <EntityInitialsAvatar name={macro.name} size={36} />
        <div className="min-w-0">
          <div className="truncate text-[14px] font-medium text-[var(--color-foreground)] transition-colors group-hover:text-[var(--color-primary)]">
            {macro.name}
          </div>
          {macro.text ? (
            <div className="truncate text-[12px] text-[var(--color-muted-foreground)]">{macro.text}</div>
          ) : null}
        </div>
      </div>
      <div className="flex min-w-0 items-center gap-1.5 text-[12px] text-[var(--color-muted-foreground)]">
        {owner ? <User className="size-3.5 shrink-0" /> : null}
        <span className="truncate">{owner ?? "All users"}</span>
      </div>
      <div className="flex items-center">
        <EntityStatusBadge tone={macro.isActive ? "success" : "default"}>
          {macro.isActive ? "Active" : "Inactive"}
        </EntityStatusBadge>
      </div>
      <div className="flex items-center justify-end gap-1">
        <button
          type="button"
          aria-label={`Edit ${macro.name}`}
          onClick={onEdit}
          className="grid size-7 cursor-pointer place-items-center rounded-md text-[var(--color-muted-foreground)] opacity-0 transition-all hover:bg-[var(--color-muted)] hover:text-[var(--color-foreground)] group-hover:opacity-100"
        >
          <Pencil className="size-3.5" />
        </button>
        <button
          type="button"
          aria-label={`Delete ${macro.name}`}
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

function MacroEditorDialog({
  state,
  field,
  users,
  onClose,
}: {
  state: MacroEditorState;
  field: SelectedField;
  users: UserDto[];
  onClose: () => void;
}) {
  const isOpen = state.mode === "create" || state.mode === "edit";
  const macro = state.mode === "edit" ? state.macro : undefined;
  const queryClient = useQueryClient();

  const initial = useMemo(
    () => ({
      name: macro?.name ?? "",
      text: macro?.text ?? "",
      useableByUserId: macro?.useableByUserId ?? "",
      isActive: macro?.isActive ?? true,
    }),
    [macro],
  );

  const [form, setForm] = useState(initial);
  useEffect(() => {
    if (isOpen) setForm(initial);
  }, [isOpen, initial]);

  const invalidate = () => queryClient.invalidateQueries({ queryKey: ["administration", "macros"] });

  const createMutation = useMutation({
    mutationFn: (input: CreateMacroInput) => createMacro(input),
    onSuccess: () => {
      toast.success("Macro created");
      invalidate();
      onClose();
    },
    onError: (err) => toast.error("Create failed", { description: describe(err) }),
  });

  const updateMutation = useMutation({
    mutationFn: (input: UpdateMacroInput) => updateMacro(input),
    onSuccess: () => {
      toast.success("Macro updated");
      invalidate();
      onClose();
    },
    onError: (err) => toast.error("Update failed", { description: describe(err) }),
  });

  const isPending = createMutation.isPending || updateMutation.isPending;
  const trimmedName = form.name.trim();
  const targetFieldName = macro ? (macro.reportFieldName ?? "All (General)") : field.name;

  const onSubmit = (e: FormEvent<HTMLFormElement>) => {
    e.preventDefault();
    if (!trimmedName) return;
    const text = form.text.trim() || null;
    const useableByUserId = form.useableByUserId || null;
    if (state.mode === "edit" && macro) {
      updateMutation.mutate({
        macroId: macro.id,
        name: trimmedName,
        text,
        reportFieldId: macro.reportFieldId ?? null,
        useableByUserId,
        isActive: form.isActive,
      });
    } else {
      createMutation.mutate({ name: trimmedName, text, reportFieldId: field.id ?? null, useableByUserId });
    }
  };

  return (
    <Dialog open={isOpen} onOpenChange={(o) => (!o ? onClose() : undefined)}>
      <DialogContent className="!max-w-lg">
        <form onSubmit={onSubmit}>
          <DialogHeader>
            <DialogTitle>{macro ? "Edit macro" : "Add a macro"}</DialogTitle>
            <DialogDescription>
              {macro ? `Update details for ${macro.name}.` : `Add a reusable text snippet for ${targetFieldName}.`}
            </DialogDescription>
          </DialogHeader>

          <DialogBody className="space-y-5">
            <div className="flex items-center gap-2 rounded-lg border border-[var(--color-border)] bg-[var(--color-muted)] px-3 py-2 text-[12px]">
              <Layers className="size-3.5 text-[var(--color-muted-foreground)]" />
              <span className="text-[var(--color-muted-foreground)]">Field:</span>
              <span className="font-medium text-[var(--color-foreground)]">{targetFieldName}</span>
            </div>

            <Field id="macro-name" label="Name" required>
              <Input
                id="macro-name"
                value={form.name}
                onChange={(e) => setForm((f) => ({ ...f, name: e.target.value }))}
                placeholder="Normal Exam"
                autoFocus
                required
                maxLength={200}
              />
            </Field>

            <Field id="macro-text" label="Text" hint="The boilerplate inserted when this macro is applied.">
              <textarea
                id="macro-text"
                value={form.text}
                onChange={(e) => setForm((f) => ({ ...f, text: e.target.value }))}
                placeholder="Patient is well-appearing and in no acute distress…"
                rows={6}
                maxLength={8000}
                className={textareaClass}
              />
            </Field>

            <Field id="macro-useable-by" label="Useable by" hint="Owner-only — leave as “All users” to share with everyone.">
              <select
                id="macro-useable-by"
                value={form.useableByUserId}
                onChange={(e) => setForm((f) => ({ ...f, useableByUserId: e.target.value }))}
                className={cn(selectClass, "w-full")}
              >
                <option value="">All users</option>
                {users.map((u) => (
                  <option key={u.id} value={u.id}>
                    {userLabel(u)}
                  </option>
                ))}
              </select>
            </Field>

            {macro && (
              <div className="flex items-center justify-between rounded-lg border border-[var(--color-border)] px-3 py-2.5">
                <div>
                  <p className="text-[13px] font-medium text-[var(--color-foreground)]">Active</p>
                  <p className="text-[12px] text-[var(--color-muted-foreground)]">
                    Inactive macros are hidden from selection lists.
                  </p>
                </div>
                <Switch
                  checked={form.isActive}
                  onCheckedChange={(v) => setForm((f) => ({ ...f, isActive: v }))}
                  aria-label="Macro active"
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
              {isPending ? "Saving…" : macro ? "Save changes" : "Add macro"}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}

function DeleteMacroDialog({ state, onClose }: { state: MacroEditorState; onClose: () => void }) {
  const isOpen = state.mode === "delete";
  const macro = state.mode === "delete" ? state.macro : undefined;
  const queryClient = useQueryClient();

  const deleteMutation = useMutation({
    mutationFn: (id: string) => deleteMacro(id),
    onSuccess: () => {
      toast.success("Macro deleted");
      queryClient.invalidateQueries({ queryKey: ["administration", "macros"] });
      onClose();
    },
    onError: (err) => toast.error("Delete failed", { description: describe(err) }),
  });

  return (
    <Dialog open={isOpen} onOpenChange={(o) => (!o ? onClose() : undefined)}>
      <DialogContent>
        <DialogHeader>
          <DialogTitle className="text-[var(--color-destructive)]">Delete macro</DialogTitle>
          <DialogDescription>
            This removes{" "}
            <span className="font-medium text-[var(--color-foreground)]">{macro?.name}</span>{" "}
            <span className="opacity-70">(created {macro && formatDate(macro.createdAtUtc)})</span>.
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
            onClick={() => macro && deleteMutation.mutate(macro.id)}
            disabled={deleteMutation.isPending || !macro}
          >
            {deleteMutation.isPending ? "Deleting…" : "Delete macro"}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}

function ReportTypeEditorDialog({ state, onClose }: { state: TypeEditorState; onClose: () => void }) {
  const isEdit = state.mode === "edit";
  const isOpen = state.mode === "create" || isEdit;
  const type = state.mode === "edit" ? state.type : undefined;
  const queryClient = useQueryClient();
  const invalidate = () => queryClient.invalidateQueries({ queryKey: ["administration", "report-types"] });

  const initial = useMemo(
    () => ({ name: type?.name ?? "", displayOrder: type?.displayOrder ?? 0, isActive: type?.isActive ?? true }),
    [type],
  );
  const [form, setForm] = useState(initial);
  useEffect(() => {
    if (isOpen) setForm(initial);
  }, [isOpen, initial]);

  const save = useMutation({
    mutationFn: async () => {
      const name = form.name.trim();
      if (isEdit && type) {
        await updateReportType({ id: type.id, name, displayOrder: form.displayOrder, isActive: form.isActive });
      } else {
        await createReportType({ name, displayOrder: form.displayOrder });
      }
    },
    onSuccess: () => {
      toast.success(isEdit ? "Report type updated" : "Report type created");
      invalidate();
      onClose();
    },
    onError: (err) => toast.error("Save failed", { description: describe(err) }),
  });

  const del = useMutation({
    mutationFn: () => deleteReportType((state as { type: ReportTypeDto }).type.id),
    onSuccess: () => {
      toast.success("Report type deleted");
      invalidate();
      onClose();
    },
    onError: (err) => toast.error("Delete failed", { description: describe(err) }),
  });

  if (state.mode === "delete") {
    return (
      <ConfirmDelete
        open
        title="Delete report type"
        name={state.type.name}
        note="Its fields become unavailable. Macros under those fields are kept."
        pending={del.isPending}
        onConfirm={() => del.mutate()}
        onClose={onClose}
      />
    );
  }

  const trimmed = form.name.trim();
  return (
    <Dialog open={isOpen} onOpenChange={(o) => (!o ? onClose() : undefined)}>
      <DialogContent className="!max-w-md">
        <form
          onSubmit={(e) => {
            e.preventDefault();
            if (trimmed) save.mutate();
          }}
        >
          <DialogHeader>
            <DialogTitle>{isEdit ? "Edit report type" : "Add report type"}</DialogTitle>
          </DialogHeader>
          <DialogBody className="space-y-5">
            <Field id="rt-name" label="Name" required>
              <Input
                id="rt-name"
                value={form.name}
                onChange={(e) => setForm((f) => ({ ...f, name: e.target.value }))}
                placeholder="Initial Evaluation"
                autoFocus
                required
                maxLength={128}
              />
            </Field>
            <Field id="rt-order" label="Display order" hint="Lower numbers sort first.">
              <Input
                id="rt-order"
                type="number"
                min={0}
                value={form.displayOrder}
                onChange={(e) => setForm((f) => ({ ...f, displayOrder: Number(e.target.value) || 0 }))}
              />
            </Field>
            {isEdit && (
              <div className="flex items-center justify-between rounded-lg border border-[var(--color-border)] px-3 py-2.5">
                <p className="text-[13px] font-medium text-[var(--color-foreground)]">Active</p>
                <Switch
                  checked={form.isActive}
                  onCheckedChange={(v) => setForm((f) => ({ ...f, isActive: v }))}
                  aria-label="Report type active"
                />
              </div>
            )}
          </DialogBody>
          <DialogFooter>
            <DialogClose asChild>
              <Button type="button" variant="outline" disabled={save.isPending}>
                Cancel
              </Button>
            </DialogClose>
            <Button type="submit" disabled={save.isPending || !trimmed}>
              {save.isPending ? "Saving…" : isEdit ? "Save changes" : "Add type"}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}

function ReportFieldEditorDialog({
  state,
  reportTypeId,
  onClose,
}: {
  state: FieldEditorState;
  reportTypeId: number | null;
  onClose: () => void;
}) {
  const isEdit = state.mode === "edit";
  const isOpen = state.mode === "create" || isEdit;
  const field = state.mode === "edit" ? state.field : undefined;
  const queryClient = useQueryClient();
  const invalidate = () => queryClient.invalidateQueries({ queryKey: ["administration", "report-fields"] });

  const initial = useMemo(
    () => ({
      name: field?.name ?? "",
      category: field?.category ?? "",
      displayOrder: field?.displayOrder ?? 0,
      isActive: field?.isActive ?? true,
    }),
    [field],
  );
  const [form, setForm] = useState(initial);
  useEffect(() => {
    if (isOpen) setForm(initial);
  }, [isOpen, initial]);

  const save = useMutation({
    mutationFn: async () => {
      const name = form.name.trim();
      const category = form.category.trim() || null;
      if (isEdit && field) {
        await updateReportField({ id: field.id, name, category, displayOrder: form.displayOrder, isActive: form.isActive });
      } else if (reportTypeId !== null) {
        await createReportField({ reportTypeId, name, category, displayOrder: form.displayOrder });
      }
    },
    onSuccess: () => {
      toast.success(isEdit ? "Field updated" : "Field created");
      invalidate();
      onClose();
    },
    onError: (err) => toast.error("Save failed", { description: describe(err) }),
  });

  const del = useMutation({
    mutationFn: () => deleteReportField((state as { field: ReportFieldDto }).field.id),
    onSuccess: () => {
      toast.success("Field deleted");
      invalidate();
      onClose();
    },
    onError: (err) => toast.error("Delete failed", { description: describe(err) }),
  });

  if (state.mode === "delete") {
    return (
      <ConfirmDelete
        open
        title="Delete report field"
        name={state.field.name}
        note="Macros under this field are kept but become unreachable from this list."
        pending={del.isPending}
        onConfirm={() => del.mutate()}
        onClose={onClose}
      />
    );
  }

  const trimmed = form.name.trim();
  return (
    <Dialog open={isOpen} onOpenChange={(o) => (!o ? onClose() : undefined)}>
      <DialogContent className="!max-w-md">
        <form
          onSubmit={(e) => {
            e.preventDefault();
            if (trimmed) save.mutate();
          }}
        >
          <DialogHeader>
            <DialogTitle>{isEdit ? "Edit field" : "Add field"}</DialogTitle>
          </DialogHeader>
          <DialogBody className="space-y-5">
            <Field id="rf-name" label="Name" required>
              <Input
                id="rf-name"
                value={form.name}
                onChange={(e) => setForm((f) => ({ ...f, name: e.target.value }))}
                placeholder="Chief Complaint"
                autoFocus
                required
                maxLength={128}
              />
            </Field>
            <Field id="rf-category" label="Category" hint="Optional grouping label.">
              <Input
                id="rf-category"
                value={form.category}
                onChange={(e) => setForm((f) => ({ ...f, category: e.target.value }))}
                placeholder="Subjective"
                maxLength={128}
              />
            </Field>
            <Field id="rf-order" label="Display order" hint="Lower numbers sort first.">
              <Input
                id="rf-order"
                type="number"
                min={0}
                value={form.displayOrder}
                onChange={(e) => setForm((f) => ({ ...f, displayOrder: Number(e.target.value) || 0 }))}
              />
            </Field>
            {isEdit && (
              <div className="flex items-center justify-between rounded-lg border border-[var(--color-border)] px-3 py-2.5">
                <p className="text-[13px] font-medium text-[var(--color-foreground)]">Active</p>
                <Switch
                  checked={form.isActive}
                  onCheckedChange={(v) => setForm((f) => ({ ...f, isActive: v }))}
                  aria-label="Field active"
                />
              </div>
            )}
          </DialogBody>
          <DialogFooter>
            <DialogClose asChild>
              <Button type="button" variant="outline" disabled={save.isPending}>
                Cancel
              </Button>
            </DialogClose>
            <Button type="submit" disabled={save.isPending || !trimmed}>
              {save.isPending ? "Saving…" : isEdit ? "Save changes" : "Add field"}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}

function ConfirmDelete({
  open,
  title,
  name,
  note,
  pending,
  onConfirm,
  onClose,
}: {
  open: boolean;
  title: string;
  name: string;
  note?: string;
  pending: boolean;
  onConfirm: () => void;
  onClose: () => void;
}) {
  return (
    <Dialog open={open} onOpenChange={(o) => (!o ? onClose() : undefined)}>
      <DialogContent>
        <DialogHeader>
          <DialogTitle className="text-[var(--color-destructive)]">{title}</DialogTitle>
          <DialogDescription>
            This removes <span className="font-medium text-[var(--color-foreground)]">{name}</span>.
            {note ? <span className="block opacity-70">{note}</span> : null}
          </DialogDescription>
        </DialogHeader>
        <DialogFooter>
          <DialogClose asChild>
            <Button type="button" variant="outline" disabled={pending}>
              Cancel
            </Button>
          </DialogClose>
          <Button variant="destructive" onClick={onConfirm} disabled={pending}>
            {pending ? "Deleting…" : "Delete"}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
