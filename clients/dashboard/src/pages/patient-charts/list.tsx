import { useEffect, useMemo, useState } from "react";
import { Link } from "react-router-dom";
import { keepPreviousData, useQuery } from "@tanstack/react-query";
import { ChevronRight, ClipboardList } from "lucide-react";
import { searchPatients, type PatientListItemDto } from "@/api/patients";
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
} from "@/components/list";
import { formatDate } from "@/lib/list-helpers";

const PAGE_SIZE = 20;
const DESKTOP_COLS = "grid-cols-[1fr_140px_24px] lg:grid-cols-[1.6fr_140px_160px_24px]";

function fullName(p: PatientListItemDto): string {
  return [p.firstName, p.middleInitial, p.lastName].filter(Boolean).join(" ");
}

export function PatientChartListPage() {
  const [search, setSearch] = useState("");
  const [debouncedSearch, setDebouncedSearch] = useState("");
  const [pageNumber, setPageNumber] = useState(1);

  useEffect(() => {
    const t = setTimeout(() => {
      setDebouncedSearch(search.trim());
      setPageNumber(1);
    }, 250);
    return () => clearTimeout(t);
  }, [search]);

  const queryParams = useMemo(
    () => ({
      pageNumber,
      pageSize: PAGE_SIZE,
      search: debouncedSearch || undefined,
      isActive: true,
      sortBy: "lastName",
      sortDir: "asc" as const,
    }),
    [pageNumber, debouncedSearch],
  );

  const query = useQuery({
    queryKey: ["patient-charts", "list", queryParams],
    queryFn: () => searchPatients(queryParams),
    placeholderData: keepPreviousData,
  });

  const data = query.data;
  const items = data?.items ?? [];

  return (
    <div className="space-y-4 sm:space-y-6">
      <EntityPageHeader
        icon={ClipboardList}
        title="Patient Chart"
        total={data?.totalCount ?? null}
        unit="patient"
        description="Select a patient to open their clinical chart and manage incidents."
      />

      <EntitySearch
        value={search}
        onChange={setSearch}
        placeholder="Search by name or patient code…"
      />

      {query.isLoading && items.length === 0 ? (
        <EntityListLoading desktopColumns={DESKTOP_COLS} />
      ) : items.length === 0 ? (
        <EntityEmpty
          icon={ClipboardList}
          title={debouncedSearch ? "No patients found" : "No patients yet"}
          body={
            debouncedSearch
              ? `Nothing matches "${debouncedSearch}". Try a different search term.`
              : "No active patients are on file."
          }
        />
      ) : (
        <div>
          {/* Mobile */}
          <div className="space-y-2 md:hidden">
            {items.map((p) => (
              <EntityMobileCard
                key={p.id}
                href={`/patient-charts/${p.id}`}
                aria-label={`Open chart for ${fullName(p)}`}
              >
                <div className="flex items-center justify-between">
                  <div className="flex min-w-0 items-center gap-3">
                    <EntityInitialsAvatar name={fullName(p)} size={40} />
                    <div className="min-w-0">
                      <p className="truncate text-[14px] font-medium">{fullName(p)}</p>
                      <p className="mt-0.5 text-[11px] text-[var(--color-muted-foreground)]">
                        {p.patientCode}
                      </p>
                    </div>
                  </div>
                  <ChevronRight className="size-4 shrink-0 text-[var(--color-border)]" />
                </div>
              </EntityMobileCard>
            ))}
          </div>

          {/* Desktop */}
          <EntityListCard className="hidden md:block">
            <EntityListHeader className={DESKTOP_COLS}>
              <span>Name</span>
              <span>Patient code</span>
              <span className="hidden lg:block">Date of birth</span>
              <span />
            </EntityListHeader>

            {items.map((p, i) => (
              <EntityListRow
                key={p.id}
                className={DESKTOP_COLS}
                isLast={i === items.length - 1}
              >
                <div className="flex items-center gap-3 min-w-0">
                  <EntityInitialsAvatar name={fullName(p)} size={32} />
                  <span className="truncate text-[13px] font-medium">{fullName(p)}</span>
                </div>
                <span className="text-[13px] text-[var(--color-muted-foreground)]">
                  {p.patientCode}
                </span>
                <span className="hidden text-[13px] text-[var(--color-muted-foreground)] lg:block">
                  {formatDate(p.dateOfBirth)}
                </span>
                <Link
                  to={`/patient-charts/${p.id}`}
                  aria-label={`Open chart for ${fullName(p)}`}
                  className="flex items-center justify-end"
                  onClick={(e) => e.stopPropagation()}
                >
                  <ChevronRight className="size-4 text-[var(--color-border)] group-hover:text-[var(--color-foreground)]" />
                </Link>
              </EntityListRow>
            ))}
          </EntityListCard>

          <EntityPager
            page={data?.pageNumber ?? 1}
            totalPages={Math.max(data?.totalPages ?? 1, 1)}
            hasPrev={data?.hasPrevious ?? false}
            hasNext={data?.hasNext ?? false}
            onPrev={() => setPageNumber((p) => Math.max(1, p - 1))}
            onNext={() => setPageNumber((p) => p + 1)}
          />
        </div>
      )}
    </div>
  );
}
