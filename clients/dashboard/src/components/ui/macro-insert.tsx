import { useEffect, useMemo, useRef, useState, type FormEvent } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { Plus, Sparkles } from "lucide-react";
import { toast } from "sonner";
import { createMacro, listMacros, type MacroDto } from "@/api/administration";
import { searchUsers } from "@/api/identity";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Textarea } from "@/components/ui/textarea";
import {
  Dialog,
  DialogBody,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import { Combobox, Field, type ComboboxOption } from "@/components/list";
import { describe } from "@/lib/list-helpers";
import { cn } from "@/lib/cn";

/**
 * MacroInsert — the Macros dialog for a report field, mirroring BackChart.
 *
 * The macro is NOT applied straight to the report. The dialog holds a working
 * copy of the field's text beside the macro list: picking a macro splices it
 * into that staging field (at the caret, when it's focused), where it can be
 * composed with further macros and edited by hand. Only "Complete" writes the
 * result back, via `onCommit`; cancelling discards the whole staged edit. That
 * makes an insert previewable and reversible, which a direct splice into the
 * live field never was.
 *
 * Macros come from the Administration catalog in two buckets — those assigned
 * to a specific report field, and the "All (General)" (unassigned) bucket. We
 * surface BOTH, field-specific first, so the list is populated even for a field
 * with no macros of its own.
 */
export function MacroInsert({
  reportFieldId,
  fieldName,
  value,
  onCommit,
  disabled,
  className,
}: {
  reportFieldId: number;
  /** Field display name — shown in the dialog for context. */
  fieldName?: string;
  /** The field's current text; seeds the staging copy each time the dialog opens. */
  value: string;
  /** The staged text, committed back to the field on "Complete". */
  onCommit: (text: string) => void;
  disabled?: boolean;
  className?: string;
}) {
  const queryClient = useQueryClient();
  const [open, setOpen] = useState(false);
  const [mode, setMode] = useState<"pick" | "create">("pick");
  const [draft, setDraft] = useState("");
  const stagingRef = useRef<HTMLTextAreaElement | null>(null);

  // Re-seed from the field on every open, so a discarded edit never lingers and
  // a draft never shadows text typed into the field since the last visit.
  useEffect(() => {
    if (!open) return;
    setDraft(value);
    setMode("pick");
    // `value` is deliberately not a dep: re-seeding mid-edit would wipe the
    // staged text the moment the field's own state changed underneath.
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [open]);

  // Only fetch once the dialog is opened — avoids N queries on a long report.
  // Match by field NAME (not id) so macros are shared across the per-type copies
  // of a field (e.g. "Diagnostic Imaging" exists under Initial Eval / Progress /
  // Discharge as distinct ids). Falls back to the id when no name is supplied.
  const fieldMacrosQuery = useQuery({
    queryKey: ["administration.macros", "field", fieldName ?? reportFieldId],
    queryFn: () =>
      fieldName
        ? listMacros({ reportFieldName: fieldName, isActive: true, pageSize: 100 })
        : listMacros({ reportFieldId, isActive: true, pageSize: 100 }),
    enabled: open,
    staleTime: 5 * 60 * 1000,
  });

  const generalMacrosQuery = useQuery({
    queryKey: ["administration.macros", "general"],
    queryFn: () => listMacros({ general: true, isActive: true, pageSize: 100 }),
    enabled: open,
    staleTime: 5 * 60 * 1000,
  });

  const isLoading = fieldMacrosQuery.isLoading || generalMacrosQuery.isLoading;

  // Field-specific macros first, then general — deduped, only those with text.
  const macros = useMemo(() => {
    const withText = (m: MacroDto) => (m.text ?? "").trim().length > 0;
    const field = (fieldMacrosQuery.data?.items ?? []).filter(withText);
    const general = (generalMacrosQuery.data?.items ?? []).filter(withText);
    const seen = new Set(field.map((m) => m.id));
    return [...field, ...general.filter((m) => !seen.has(m.id))];
  }, [fieldMacrosQuery.data, generalMacrosQuery.data]);

  /** Splice at the caret when the staging field is focused; otherwise append. */
  const stageMacro = (text: string) => {
    const ta = stagingRef.current;
    setDraft((prev) => {
      if (ta && document.activeElement === ta) {
        const start = ta.selectionStart ?? prev.length;
        const end = ta.selectionEnd ?? prev.length;
        return prev.slice(0, start) + text + prev.slice(end);
      }
      const sep = prev && !prev.endsWith("\n") ? "\n" : "";
      return prev + sep + text;
    });
  };

  const onComplete = () => {
    onCommit(draft);
    setOpen(false);
  };

  return (
    <>
      <button
        type="button"
        title="Insert macro"
        disabled={disabled}
        onClick={() => setOpen(true)}
        className={cn(
          "inline-flex h-7 items-center gap-1 rounded-md border border-[var(--color-border)] px-2",
          "text-[11px] font-semibold uppercase tracking-wider text-[var(--color-muted-foreground)]",
          "transition-colors hover:bg-[var(--color-accent)] hover:text-[var(--color-foreground)]",
          "disabled:pointer-events-none disabled:opacity-40",
          className,
        )}
      >
        <Sparkles className="size-3.5" />
        Macro
      </button>

      <Dialog open={open} onOpenChange={setOpen}>
        <DialogContent className="!max-w-3xl">
          {mode === "pick" ? (
            <>
              <DialogHeader>
                <DialogTitle>Macros</DialogTitle>
                <DialogDescription>
                  {fieldName ? `“${fieldName}” — p` : "P"}ick macros into the working copy, edit it
                  as needed, then Complete to write it back to the field.
                </DialogDescription>
              </DialogHeader>

              <DialogBody>
                <div className="grid gap-4 sm:grid-cols-2">
                  <Field id="macro-staging" label={fieldName ?? "Field text"}>
                    <Textarea
                      id="macro-staging"
                      data-testid="macro-staging-text"
                      ref={stagingRef}
                      value={draft}
                      onChange={(e) => setDraft(e.target.value)}
                      rows={14}
                      maxLength={16000}
                      placeholder="Pick a macro, or type here…"
                    />
                  </Field>

                  <div className="min-w-0">
                    <p className="mb-1.5 text-[11.5px] font-semibold uppercase tracking-wider text-[var(--color-muted-foreground)]">
                      Macros
                    </p>
                    <div className="h-[19rem] overflow-y-auto rounded-md border border-[var(--color-border)]">
                      {isLoading ? (
                        <p className="px-3 py-3 text-[12px] text-[var(--color-muted-foreground)]">
                          Loading…
                        </p>
                      ) : macros.length === 0 ? (
                        <p className="px-3 py-3 text-[12px] text-[var(--color-muted-foreground)]">
                          No macros yet — create one below.
                        </p>
                      ) : (
                        <ul className="divide-y divide-[var(--color-border)]">
                          {macros.map((m) => (
                            <li key={m.id}>
                              <button
                                type="button"
                                onClick={() => stageMacro(m.text ?? "")}
                                className="flex w-full flex-col gap-0.5 px-3 py-2 text-left hover:bg-[var(--color-accent)]"
                              >
                                <span className="flex items-center justify-between gap-2">
                                  <span className="text-[13px] font-medium">{m.name}</span>
                                  {m.reportFieldId == null && (
                                    <span className="shrink-0 rounded-full bg-[var(--color-muted)] px-1.5 py-0.5 text-[10px] font-semibold uppercase tracking-wider text-[var(--color-muted-foreground)]">
                                      General
                                    </span>
                                  )}
                                </span>
                                <span className="line-clamp-2 text-[11.5px] text-[var(--color-muted-foreground)]">
                                  {m.text}
                                </span>
                              </button>
                            </li>
                          ))}
                        </ul>
                      )}
                    </div>
                    <Button
                      type="button"
                      size="sm"
                      variant="outline"
                      className="mt-2 w-full"
                      onClick={() => setMode("create")}
                    >
                      <Plus className="size-4" />
                      Create New Macro
                    </Button>
                  </div>
                </div>
              </DialogBody>

              <DialogFooter>
                <Button type="button" variant="outline" onClick={() => setOpen(false)}>
                  Cancel
                </Button>
                <Button type="button" onClick={onComplete}>
                  Complete
                </Button>
              </DialogFooter>
            </>
          ) : (
            <CreateMacroForm
              reportFieldId={reportFieldId}
              fieldName={fieldName}
              onCancel={() => setMode("pick")}
              onCreated={() => {
                // Back to the picker with the new macro in the list — saving a
                // macro never inserts it, as in BackChart.
                void queryClient.invalidateQueries({ queryKey: ["administration.macros"] });
                setMode("pick");
              }}
            />
          )}
        </DialogContent>
      </Dialog>
    </>
  );
}

function CreateMacroForm({
  reportFieldId,
  fieldName,
  onCancel,
  onCreated,
}: {
  reportFieldId: number;
  fieldName?: string;
  onCancel: () => void;
  onCreated: () => void;
}) {
  const [name, setName] = useState("");
  const [text, setText] = useState("");
  const [allFields, setAllFields] = useState(false);
  // null = "All users" (shared with everyone); otherwise the chosen user's id.
  const [useableByUserId, setUseableByUserId] = useState<string | null>(null);

  const usersQuery = useQuery({
    queryKey: ["identity.users", "macro-owner-options"],
    queryFn: () => searchUsers({ isActive: true, pageSize: 100 }),
    staleTime: 5 * 60 * 1000,
  });

  const userOptions: ComboboxOption[] = useMemo(
    () =>
      (usersQuery.data?.items ?? [])
        .filter((u) => !!u.id)
        .map((u) => ({
          value: u.id as string,
          label:
            [u.firstName, u.lastName].filter(Boolean).join(" ").trim() ||
            u.userName ||
            u.email ||
            (u.id as string),
        })),
    [usersQuery.data],
  );

  const createMutation = useMutation({
    mutationFn: createMacro,
    onSuccess: () => {
      toast.success("Macro created.");
      onCreated();
    },
    onError: (err) => toast.error("Failed to create macro.", { description: describe(err) }),
  });

  const onSubmit = (e: FormEvent<HTMLFormElement>) => {
    e.preventDefault();
    if (!name.trim() || !text.trim()) return;
    createMutation.mutate({
      name: name.trim(),
      text: text.trim(),
      // Unchecked "all fields" → scope to this field; checked → general (null).
      reportFieldId: allFields ? null : reportFieldId,
      // "All users" (null) → shared with everyone; otherwise owned by the chosen user.
      useableByUserId,
    });
  };

  return (
    <form onSubmit={onSubmit}>
      <DialogHeader>
        <DialogTitle>Create New Macro</DialogTitle>
        <DialogDescription>
          {allFields
            ? "Available to all report fields."
            : `Scoped to ${fieldName ? `the “${fieldName}” field` : "this field"}.`}
        </DialogDescription>
      </DialogHeader>

      <DialogBody className="space-y-4">
        <Field id="macro-name" label="Macro Name" required>
          <Input
            id="macro-name"
            value={name}
            onChange={(e) => setName(e.target.value)}
            placeholder="Normal Exam"
            autoFocus
            maxLength={200}
          />
        </Field>

        <Field id="macro-text" label="Macro Text" required>
          <Textarea
            id="macro-text"
            value={text}
            onChange={(e) => setText(e.target.value)}
            rows={6}
            maxLength={8000}
            placeholder="Patient is well-appearing and in no acute distress…"
          />
        </Field>

        <label className="flex items-center gap-2 text-[13px] cursor-pointer">
          <input
            type="checkbox"
            checked={allFields}
            onChange={(e) => setAllFields(e.target.checked)}
            className="rounded border-[var(--color-border)]"
          />
          <span>Make this macro available to all fields</span>
        </label>

        <Field
          id="macro-useable-by"
          label="Useable by"
          hint="Choose “All users” to share with everyone, or pick a user to keep it private to them."
        >
          <Combobox
            id="macro-useable-by"
            label="Useable by"
            value={useableByUserId}
            onChange={setUseableByUserId}
            options={userOptions}
            emptyOptionLabel="All users"
            placeholder="All users"
            searchable
          />
        </Field>
      </DialogBody>

      <DialogFooter>
        <Button type="button" variant="outline" onClick={onCancel} disabled={createMutation.isPending}>
          Cancel
        </Button>
        <Button type="submit" disabled={createMutation.isPending || !name.trim() || !text.trim()}>
          {createMutation.isPending ? "Saving…" : "Save Macro"}
        </Button>
      </DialogFooter>
    </form>
  );
}
