import { useParams, Link } from "react-router-dom";
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import {
  getClaim, markClaimReady, submitClaim, markClaimPaid, markClaimDenied, voidClaim,
  type ClaimStatus,
} from "@/api/claims";
import { ClaimStatusPill } from "@/pages/billing/claims-list";

export function ClaimDetailPage() {
  const { claimId = "" } = useParams();
  const qc = useQueryClient();
  const claimQuery = useQuery({ queryKey: ["claim", claimId], queryFn: () => getClaim(claimId) });

  const invalidate = () => {
    qc.invalidateQueries({ queryKey: ["claim", claimId] });
    qc.invalidateQueries({ queryKey: ["claims"] });
  };
  const ready = useMutation({ mutationFn: markClaimReady, onSuccess: invalidate });
  const submit = useMutation({ mutationFn: submitClaim, onSuccess: invalidate });
  const paid = useMutation({ mutationFn: markClaimPaid, onSuccess: invalidate });
  const denied = useMutation({ mutationFn: markClaimDenied, onSuccess: invalidate });
  const doVoid = useMutation({ mutationFn: voidClaim, onSuccess: invalidate });

  const claim = claimQuery.data;
  if (!claim) return <div className="p-6 text-muted-foreground">Loading…</div>;

  const s: ClaimStatus = claim.status;
  const busy = ready.isPending || submit.isPending || paid.isPending || denied.isPending || doVoid.isPending;

  return (
    <div className="flex flex-col gap-4 pb-20">
      <div className="flex items-center justify-between">
        <div>
          <div className="flex items-center gap-2">
            <h1 className="text-xl font-semibold">Claim</h1>
            <ClaimStatusPill status={s} />
          </div>
          <div className="text-sm text-muted-foreground">
            Source <Link className="text-teal-600 hover:underline" to={`/patient-charts/${claim.patientId}`}>report</Link>
            {claim.controlNumber ? ` · ${claim.controlNumber}` : ""}
          </div>
        </div>
      </div>

      <div className="grid grid-cols-3 gap-4">
        {/* Left: procedures + diagnoses */}
        <div className="col-span-2 space-y-4">
          <section className="rounded-lg border p-4">
            <h2 className="mb-2 text-xs font-bold uppercase text-muted-foreground">Procedures (snapshot)</h2>
            <table className="w-full text-sm">
              <tbody>
                {claim.lines.map((l) => (
                  <tr key={l.procedureCodeId} className="border-b last:border-0">
                    <td className="py-1">{l.code} · {l.description ?? ""}</td>
                    <td className="py-1 text-right font-medium">${l.charge.toLocaleString()}</td>
                  </tr>
                ))}
                <tr>
                  <td className="py-1 font-bold">Total</td>
                  <td className="py-1 text-right font-bold text-teal-600">${claim.totalCharge.toLocaleString()}</td>
                </tr>
              </tbody>
            </table>
          </section>
        </div>

        {/* Right: payer + activity rail */}
        <div className="space-y-4">
          <section className="rounded-lg border p-4">
            <h2 className="mb-2 text-xs font-bold uppercase text-muted-foreground">Payer</h2>
            <div className="text-sm">Insurance type: {claim.insuranceTypeId ?? "—"}</div>
          </section>
          <section className="rounded-lg border p-4 text-xs text-muted-foreground">
            <h2 className="mb-2 font-bold uppercase">Activity</h2>
            <div>Created {new Date(claim.createdAtUtc).toLocaleString()}</div>
            {claim.submittedAtUtc && <div>Submitted {new Date(claim.submittedAtUtc).toLocaleString()}</div>}
            {claim.resolvedAtUtc && <div>Resolved {new Date(claim.resolvedAtUtc).toLocaleString()}</div>}
          </section>
        </div>
      </div>

      {/* Sticky action bar — buttons enabled per legal transition */}
      <div className="fixed inset-x-0 bottom-0 flex justify-end gap-2 border-t bg-background/95 p-3">
        {s !== "Paid" && s !== "Denied" && s !== "Voided" && (
          <button type="button" disabled={busy} onClick={() => doVoid.mutate({ id: claim.id })}
            className="rounded-md border px-3 py-1.5 text-sm">Void</button>
        )}
        {s === "Draft" && (
          <button type="button" disabled={busy} onClick={() => ready.mutate(claim.id)}
            className="rounded-md bg-teal-600 px-3 py-1.5 text-sm font-semibold text-white">Mark Ready →</button>
        )}
        {s === "Ready" && (
          <button type="button" disabled={busy} onClick={() => submit.mutate(claim.id)}
            className="rounded-md bg-teal-600 px-3 py-1.5 text-sm font-semibold text-white">Submit →</button>
        )}
        {s === "Submitted" && (
          <>
            <button type="button" disabled={busy} onClick={() => denied.mutate(claim.id)}
              className="rounded-md border px-3 py-1.5 text-sm">Mark Denied</button>
            <button type="button" disabled={busy} onClick={() => paid.mutate(claim.id)}
              className="rounded-md bg-green-600 px-3 py-1.5 text-sm font-semibold text-white">Mark Paid</button>
          </>
        )}
      </div>
    </div>
  );
}
