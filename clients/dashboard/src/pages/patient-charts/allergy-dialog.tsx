import { useEffect, useState, type FormEvent } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { toast } from "sonner";
import { createAllergy, updateAllergy, type PatientAllergy } from "@/api/allergies";
import { listAllergyReactions } from "@/api/administration";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Textarea } from "@/components/ui/textarea";
import {
  Dialog,
  DialogBody,
  DialogClose,
  DialogContent,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import { Combobox, Field, type ComboboxOption } from "@/components/list";
import { describe } from "@/lib/list-helpers";
import { DrugPicker, type DrugSelection } from "@/pages/patient-charts/drug-picker";

type Props = {
  patientId: string;
  open: boolean;
  onClose(): void;
  /** Edit mode when set. */
  allergy?: PatientAllergy | null;
  /** Whether the current user may flip Active/Inactive (Allergies.Delete, legacy ALLERGYLISTDELETE). */
  canToggleActive: boolean;
};

export function AllergyDialog({ patientId, open, onClose, allergy, canToggleActive }: Props) {
  const queryClient = useQueryClient();
  const isEdit = !!allergy;

  const [drug, setDrug] = useState<DrugSelection | null>(null);
  const [reaction, setReaction] = useState("");
  const [comments, setComments] = useState("");
  const [dateNoted, setDateNoted] = useState("");
  const [isActive, setIsActive] = useState(true);

  useEffect(() => {
    if (!open) return;
    if (allergy) {
      setDrug({ name: allergy.drugName, rxAui: allergy.rxAui ?? null, rxCui: null });
      setReaction(allergy.reaction ?? "");
      setComments(allergy.comments ?? "");
      setDateNoted(allergy.dateNoted ? allergy.dateNoted.slice(0, 10) : "");
      setIsActive(allergy.isActive);
    } else {
      setDrug(null);
      setReaction("");
      setComments("");
      setDateNoted(new Date().toISOString().slice(0, 10));
      setIsActive(true);
    }
  }, [open, allergy]);

  const reactionsQuery = useQuery({
    queryKey: ["allergy-reactions", "active"],
    queryFn: () => listAllergyReactions({ isActive: true }),
    enabled: open,
    staleTime: 5 * 60 * 1000,
  });

  const reactionOptions: ComboboxOption[] = (reactionsQuery.data ?? []).map((r) => ({
    value: String(r.id),
    label: r.term,
  }));

  // Legacy behavior: the SNOMED picker appends terms into the single Reaction text field.
  const appendReaction = (id: string | null) => {
    if (!id) return;
    const term = (reactionsQuery.data ?? []).find((r) => String(r.id) === id)?.term;
    if (!term) return;
    setReaction((prev) => {
      const parts = prev.split(",").map((p) => p.trim()).filter(Boolean);
      if (parts.includes(term)) return prev;
      return [...parts, term].join(", ");
    });
  };

  const invalidate = () => {
    void queryClient.invalidateQueries({ queryKey: ["patient-allergies", patientId] });
    void queryClient.invalidateQueries({ queryKey: ["patient", patientId] });
  };

  const createMutation = useMutation({
    mutationFn: createAllergy,
    onSuccess: () => {
      toast.success("Allergy added.");
      invalidate();
      onClose();
    },
    onError: (err) => toast.error("Failed to add allergy.", { description: describe(err) }),
  });

  const updateMutation = useMutation({
    mutationFn: updateAllergy,
    onSuccess: () => {
      toast.success("Allergy updated.");
      invalidate();
      onClose();
    },
    onError: (err) => toast.error("Failed to update allergy.", { description: describe(err) }),
  });

  const isPending = createMutation.isPending || updateMutation.isPending;

  const onSubmit = (e: FormEvent<HTMLFormElement>) => {
    e.preventDefault();
    if (!drug?.name || !dateNoted) return;
    const fields = {
      patientId,
      drugName: drug.name,
      rxAui: drug.rxAui,
      reaction: reaction.trim() || null,
      comments: comments.trim() || null,
      dateNoted,
      isActive,
    };
    if (isEdit && allergy) {
      updateMutation.mutate({ ...fields, allergyId: allergy.id });
    } else {
      createMutation.mutate(fields);
    }
  };

  return (
    <Dialog open={open} onOpenChange={(o) => (!o ? onClose() : undefined)}>
      <DialogContent className="!max-w-lg">
        <form onSubmit={onSubmit}>
          <DialogHeader>
            <DialogTitle>{isEdit ? "Edit Allergy" : "Add Allergy"}</DialogTitle>
          </DialogHeader>

          <DialogBody className="space-y-4">
            <DrugPicker value={drug} onChange={setDrug} label="Drug Name" />

            <Field id="allergy-reaction-pick" label="Add Reaction (SNOMED list)">
              <Combobox
                id="allergy-reaction-pick"
                label="Add Reaction"
                value={null}
                onChange={appendReaction}
                options={reactionOptions}
                placeholder="Pick to append…"
              />
            </Field>

            <Field id="allergy-reaction" label="Reaction">
              <Input
                id="allergy-reaction"
                type="text"
                value={reaction}
                maxLength={1000}
                onChange={(e) => setReaction(e.target.value)}
                placeholder="e.g. Rash, Hives"
              />
            </Field>

            <Field id="allergy-date-noted" label="Date Noted">
              <Input
                id="allergy-date-noted"
                type="date"
                value={dateNoted}
                onChange={(e) => setDateNoted(e.target.value)}
                required
              />
            </Field>

            <Field id="allergy-comments" label="Comments">
              <Textarea
                id="allergy-comments"
                value={comments}
                onChange={(e) => setComments(e.target.value)}
                rows={3}
                maxLength={4000}
              />
            </Field>

            {canToggleActive && (
              <div className="flex items-center gap-4 text-[13px]">
                <label className="flex items-center gap-2 cursor-pointer">
                  <input
                    type="radio"
                    name="allergy-active"
                    checked={isActive}
                    onChange={() => setIsActive(true)}
                  />
                  <span>Active</span>
                </label>
                <label className="flex items-center gap-2 cursor-pointer">
                  <input
                    type="radio"
                    name="allergy-active"
                    checked={!isActive}
                    onChange={() => setIsActive(false)}
                  />
                  <span>Inactive</span>
                </label>
              </div>
            )}

            {isEdit && allergy?.createdByName && (
              <p className="text-[12px] text-[var(--color-muted-foreground)]">
                Created: {allergy.createdByName}
                {allergy.updatedByName ? ` · Last modified: ${allergy.updatedByName}` : ""}
              </p>
            )}
          </DialogBody>

          <DialogFooter>
            <DialogClose asChild>
              <Button type="button" variant="outline" disabled={isPending}>
                Cancel
              </Button>
            </DialogClose>
            <Button type="submit" disabled={isPending || !drug?.name || !dateNoted}>
              {isPending ? "Saving…" : isEdit ? "Save Changes" : "Add Allergy"}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}
