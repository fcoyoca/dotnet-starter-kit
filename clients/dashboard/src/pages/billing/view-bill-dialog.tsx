import { Printer } from "lucide-react";
import { toast } from "sonner";
import { Button } from "@/components/ui/button";
import {
  Dialog,
  DialogBody,
  DialogClose,
  DialogContent,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import {
  BillSummaryView,
  printBillSummary,
  type BillSummaryData,
} from "@/pages/billing/bill-summary";

type Props = {
  open: boolean;
  onClose(): void;
  data: BillSummaryData;
  /** Print sheet + dialog title. */
  title?: string;
};

/**
 * Read-only bill review, reused by the SuperBill editor and the claim-detail page.
 * The Print button opens a self-contained print window (Save-as-PDF is the browser's
 * built-in print destination), matching legacy BackChart's "View Bill" → print flow.
 */
export function ViewBillDialog({ open, onClose, data, title = "View Bill" }: Props) {
  return (
    <Dialog open={open} onOpenChange={(o) => !o && onClose()}>
      <DialogContent className="!max-w-3xl">
        <DialogHeader>
          <DialogTitle>{title}</DialogTitle>
        </DialogHeader>
        <DialogBody>
          <BillSummaryView data={data} />
        </DialogBody>
        <DialogFooter>
          <Button
            type="button"
            onClick={() => {
              if (!printBillSummary(data, title)) {
                toast.error("Couldn't open the print window — allow pop-ups for this site.");
              }
            }}
          >
            <Printer className="size-4" />
            Print / PDF
          </Button>
          <DialogClose asChild>
            <Button type="button" variant="outline">
              Close
            </Button>
          </DialogClose>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
