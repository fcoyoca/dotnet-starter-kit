import { useMemo, useState } from "react";
import { useQuery } from "@tanstack/react-query";
import { listDrugs } from "@/api/administration";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";

export type DrugSelection = {
  name: string;
  rxAui: string | null;
  rxCui: string | null;
};

type Props = {
  value: DrugSelection | null;
  onChange(value: DrugSelection | null): void;
  label?: string;
};

/** Drug-catalog search picker used by the allergy and medication dialogs.
 *  Free text is allowed (legacy allowed unlisted drug names). */
export function DrugPicker({ value, onChange, label = "Drug" }: Props) {
  const [search, setSearch] = useState("");

  const drugsQuery = useQuery({
    queryKey: ["drug-search", search],
    queryFn: () => listDrugs({ search, isActive: true, pageSize: 50 }),
    enabled: search.length >= 2,
  });

  const options = useMemo(() => drugsQuery.data?.items ?? [], [drugsQuery.data]);

  return (
    <div className="space-y-2">
      <label className="text-[11.5px] font-semibold uppercase tracking-wider text-[var(--color-muted-foreground)]">
        {label}
      </label>
      {value ? (
        <div className="flex items-start justify-between gap-2 rounded-lg border border-[var(--color-border)] p-2.5">
          <div className="min-w-0">
            <p className="text-[13px] font-medium">{value.name}</p>
            {value.rxCui && (
              <p className="text-[12px] text-[var(--color-muted-foreground)]">RxCUI {value.rxCui}</p>
            )}
          </div>
          <Button type="button" variant="ghost" size="xs" onClick={() => onChange(null)}>
            Change
          </Button>
        </div>
      ) : (
        <>
          <Input
            type="text"
            value={search}
            onChange={(e) => setSearch(e.target.value)}
            placeholder="Search the drug catalog (min 2 chars)…"
            autoFocus
          />
          {search.length >= 2 && (
            <ul className="max-h-40 overflow-y-auto rounded-md border border-[var(--color-border)] bg-[var(--color-card)]">
              {options.map((d) => (
                <li key={d.id}>
                  <button
                    type="button"
                    onClick={() => {
                      onChange({ name: d.name, rxAui: d.rxAui ?? null, rxCui: d.rxCui ?? null });
                      setSearch("");
                    }}
                    className="w-full px-3 py-1.5 text-left text-[12px] hover:bg-[var(--color-accent)]"
                  >
                    {d.name}
                    {d.tty ? ` (${d.tty})` : ""}
                  </button>
                </li>
              ))}
              <li>
                <button
                  type="button"
                  onClick={() => {
                    onChange({ name: search.trim(), rxAui: null, rxCui: null });
                    setSearch("");
                  }}
                  className="w-full px-3 py-1.5 text-left text-[12px] italic text-[var(--color-muted-foreground)] hover:bg-[var(--color-accent)]"
                >
                  Use "{search.trim()}" as typed
                </button>
              </li>
            </ul>
          )}
        </>
      )}
    </div>
  );
}
