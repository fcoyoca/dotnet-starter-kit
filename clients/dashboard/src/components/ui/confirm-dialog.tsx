import * as React from "react";
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
import { cn } from "@/lib/cn";

/**
 * ConfirmDialog — the shared "are you sure?" gate in front of a destructive
 * action. Every delete/remove/archive control routes through this instead of
 * firing its mutation straight from the click handler.
 *
 * Drive it from the pending subject rather than a boolean, so the copy can name
 * what is about to be destroyed:
 *
 *   const [pendingDelete, setPendingDelete] = useState<Note | null>(null);
 *   ...
 *   <button onClick={() => setPendingDelete(note)} />
 *   <ConfirmDialog
 *     open={pendingDelete !== null}
 *     title="Delete this note?"
 *     description={<>«{pendingDelete?.name}» will be removed.</>}
 *     confirmLabel="Delete note"
 *     pending={deleteMutation.isPending}
 *     onCancel={() => setPendingDelete(null)}
 *     onConfirm={() => pendingDelete && deleteMutation.mutate(pendingDelete.id)}
 *   />
 *
 * Safe to render from inside an already-open Dialog: Radix portals each layer
 * to <body> in mount order, and the raised z-index keeps this one on top.
 */

export type ConfirmDialogProps = {
  open: boolean;
  onCancel: () => void;
  onConfirm: () => void;
  /** The question, e.g. "Delete this note?" */
  title: string;
  /** What the action does and what it costs. Names the subject where possible. */
  description?: React.ReactNode;
  /** Small uppercase kicker above the title, e.g. "Delete note". */
  eyebrow?: string;
  confirmLabel?: string;
  /** Confirm-button label while the mutation is in flight. */
  pendingLabel?: string;
  cancelLabel?: string;
  /** Disables both buttons and swaps in `pendingLabel`. */
  pending?: boolean;
  /** `destructive` (default) paints the confirm button red. */
  variant?: "destructive" | "default";
  /** Optional extra body content — a preview of the subject, a warning, etc. */
  children?: React.ReactNode;
};

export function ConfirmDialog({
  open,
  onCancel,
  onConfirm,
  title,
  description,
  eyebrow,
  confirmLabel = "Delete",
  pendingLabel = "Deleting…",
  cancelLabel = "Cancel",
  pending = false,
  variant = "destructive",
  children,
}: ConfirmDialogProps) {
  return (
    <Dialog
      open={open}
      // Ignore dismissals while the mutation is in flight — closing the dialog
      // mid-request would strand the user with no feedback on the outcome.
      onOpenChange={(next) => (!next && !pending ? onCancel() : undefined)}
    >
      <DialogContent className="z-[60] sm:max-w-md" data-slot="confirm-dialog">
        <DialogHeader>
          {eyebrow && (
            <span
              className={cn(
                "text-[11px] font-semibold uppercase tracking-wider",
                variant === "destructive"
                  ? "text-[var(--color-destructive)]"
                  : "text-[var(--color-muted-foreground)]",
              )}
            >
              {eyebrow}
            </span>
          )}
          <DialogTitle>{title}</DialogTitle>
          {description && <DialogDescription>{description}</DialogDescription>}
        </DialogHeader>

        {children && <DialogBody>{children}</DialogBody>}

        <DialogFooter>
          <DialogClose asChild>
            <Button type="button" variant="outline" disabled={pending}>
              {cancelLabel}
            </Button>
          </DialogClose>
          <Button
            type="button"
            variant={variant}
            onClick={onConfirm}
            disabled={pending}
            data-slot="confirm-dialog-confirm"
          >
            {pending ? pendingLabel : confirmLabel}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
