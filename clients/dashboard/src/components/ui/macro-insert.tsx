import { useMemo, useState, type FormEvent } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { Plus, Sparkles } from "lucide-react";
import { toast } from "sonner";
import { createMacro, listMacros, type MacroDto } from "@/api/administration";
import { useAuth } from "@/auth/use-auth";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Textarea } from "@/components/ui/textarea";
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
import { Field } from "@/components/list";
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuLabel,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu";
import { describe } from "@/lib/list-helpers";
import { cn } from "@/lib/cn";

/**
 * MacroInsert — a small popover, scoped to a report field, that lists the
 * tenant's macros and inserts the chosen macro's text into the focused editor
 * field. Mirrors BackChart's per-field macro picker (incl. its "Create New
 * Macro" affordance). Macros come from the Administration catalog, which keeps
 * them in two buckets: those assigned to a specific report field, and the
 * "All (General)" bucket (unassigned). We surface BOTH — field-specific first,
 * then general — so the picker is populated even when a field has no macros.
 *
 * The insert is delivered through `onInsert(text)` so the parent decides how to
 * splice it (append, replace selection, etc.) — see the report editor (R14).
 */
export function MacroInsert({
  reportFieldId,
  fieldName,
  onInsert,
  disabled,
  className,
}: {
  reportFieldId: number;
  /** Field display name — shown in the create dialog for context. */
  fieldName?: string;
  onInsert: (text: string) => void;
  disabled?: boolean;
  className?: string;
}) {
  const { user } = useAuth();
  const queryClient = useQueryClient();
  const [open, setOpen] = useState(false);
  const [createOpen, setCreateOpen] = useState(false);

  // Only fetch once the popover is opened — avoids N queries on a long report.
  const fieldMacrosQuery = useQuery({
    queryKey: ["administration.macros", "field", reportFieldId],
    queryFn: () => listMacros({ reportFieldId, isActive: true, pageSize: 100 }),
    enabled: open,
    staleTime: 5 * 60 * 1000,
  });

  // The "All (General)" bucket (reportFieldId = null) — shared across fields.
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

  return (
    <>
      <DropdownMenu open={open} onOpenChange={(o) => !disabled && setOpen(o)}>
        <DropdownMenuTrigger asChild disabled={disabled}>
          <button
            type="button"
            title="Insert macro"
            aria-label="Insert macro"
            disabled={disabled}
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
        </DropdownMenuTrigger>

        <DropdownMenuContent align="end" className="max-h-[min(360px,55vh)] w-72 overflow-y-auto">
          <DropdownMenuLabel>Macros</DropdownMenuLabel>
          {isLoading ? (
            <p className="px-3 py-3 text-[12px] text-[var(--color-muted-foreground)]">Loading…</p>
          ) : macros.length === 0 ? (
            <p className="px-3 py-3 text-[12px] text-[var(--color-muted-foreground)]">
              No macros yet — create one below.
            </p>
          ) : (
            <ul role="none" className="py-1">
              {macros.map((m) => (
                <li key={m.id} role="none">
                  <button
                    type="button"
                    onClick={() => {
                      onInsert(m.text ?? "");
                      setOpen(false);
                    }}
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

          <DropdownMenuSeparator />
          <button
            type="button"
            onClick={() => {
              setOpen(false);
              setCreateOpen(true);
            }}
            className="flex w-full items-center gap-2 px-3 py-2 text-left text-[13px] font-medium text-[var(--color-primary)] hover:bg-[var(--color-accent)]"
          >
            <Plus className="size-4" />
            Create new macro
          </button>
        </DropdownMenuContent>
      </DropdownMenu>

      <CreateMacroDialog
        open={createOpen}
        onClose={() => setCreateOpen(false)}
        reportFieldId={reportFieldId}
        fieldName={fieldName}
        currentUserId={user?.id ?? null}
        onCreated={(text, insertNow) => {
          // Refresh both buckets so the new macro shows next time the popover opens.
          void queryClient.invalidateQueries({ queryKey: ["administration.macros"] });
          if (insertNow) onInsert(text);
          setCreateOpen(false);
        }}
      />
    </>
  );
}

function CreateMacroDialog({
  open,
  onClose,
  reportFieldId,
  fieldName,
  currentUserId,
  onCreated,
}: {
  open: boolean;
  onClose: () => void;
  reportFieldId: number;
  fieldName?: string;
  currentUserId: string | null;
  onCreated: (text: string, insertNow: boolean) => void;
}) {
  const [name, setName] = useState("");
  const [text, setText] = useState("");
  const [allFields, setAllFields] = useState(false);
  const [everyone, setEveryone] = useState(true);
  const [insertAfter, setInsertAfter] = useState(true);

  const reset = () => {
    setName("");
    setText("");
    setAllFields(false);
    setEveryone(true);
    setInsertAfter(true);
  };

  const createMutation = useMutation({
    mutationFn: createMacro,
    onSuccess: () => {
      toast.success("Macro created.");
      onCreated(text.trim(), insertAfter);
      reset();
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
      // Checked "everyone" → shared (null); unchecked → owned by current user.
      useableByUserId: everyone ? null : currentUserId,
    });
  };

  return (
    <Dialog
      open={open}
      onOpenChange={(o) => {
        if (!o) {
          reset();
          onClose();
        }
      }}
    >
      <DialogContent className="!max-w-lg">
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

            <label className="flex items-center gap-2 text-[13px] cursor-pointer">
              <input
                type="checkbox"
                checked={everyone}
                onChange={(e) => setEveryone(e.target.checked)}
                className="rounded border-[var(--color-border)]"
              />
              <span>Allow everyone to use this macro</span>
            </label>

            <label className="flex items-center gap-2 text-[13px] cursor-pointer">
              <input
                type="checkbox"
                checked={insertAfter}
                onChange={(e) => setInsertAfter(e.target.checked)}
                className="rounded border-[var(--color-border)]"
              />
              <span>Insert into the current field after saving</span>
            </label>
          </DialogBody>

          <DialogFooter>
            <DialogClose asChild>
              <Button type="button" variant="outline" disabled={createMutation.isPending}>
                Cancel
              </Button>
            </DialogClose>
            <Button type="submit" disabled={createMutation.isPending || !name.trim() || !text.trim()}>
              {createMutation.isPending ? "Saving…" : "Save Macro"}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}
