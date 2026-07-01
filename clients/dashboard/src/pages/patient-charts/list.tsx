import { useEffect, useMemo, useState } from "react";
import { Link, useNavigate } from "react-router-dom";
import { keepPreviousData, useQuery } from "@tanstack/react-query";
import { ChevronRight, ClipboardList, Plus, Search } from "lucide-react";
import { searchPatients, type PatientListItemDto } from "@/api/patients";
import { useClinicOptions, useProviderOptions } from "@/api/administration";
import { CreatePatientDialog } from "@/pages/patients/create-patient-dialog";
import { Button } from "@/components/ui/button";
import {
  Combobox,
  EntityEmpty,
  EntityFilterPill,
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
import { describe, formatDate } from "@/lib/list-helpers";

const PAGE_SIZE = 20;
const DESKTOP_COLS = "grid-cols-[1.4fr_120px_120px_24px] lg:grid-cols-[1.6fr_120px_120px_120px_24px]";

type StatusFilter = "all" | "active" | "inactive";

function fullName(p: PatientListItemDto): string {
  return [p.firstName, p.middleInitial, p.lastName].filter(Boolean).join(" ");
}

export function PatientChartListPage() {
  const navigate = useNavigate();

  const [search, setSearch] = useState("");
  const [debouncedSearch, setDebouncedSearch] = useState("");
  const [providerId, setProviderId] = useState<string | null>(null);
  const [clinicId, setClinicId] = useState<string | null>(null);
  const [statusFilter, setStatusFilter] = useState<StatusFilter>("all");
  const [pageNumber, setPageNumber] = useState(1);
  const [createOpen, setCreateOpen] = useState(false);

  const providerOptions = useProviderOptions();
  const clinicOptions = useClinicOptions();

  useEffect(() => {
    const t = setTimeout(() => {
      setDebouncedSearch(search.trim());
      setPageNumber(1);
    }, 250);
    return () => clearTimeout(t);
  }, [search]);

  useEffect(() => {
    setPageNumber(1);
  }, [providerId, clinicId, statusFilter]);

  // The page is search-first: only fetch once at least one filter is active.
  const hasActiveFilter =
    debouncedSearch.length > 0 ||
    providerId !== null ||
    clinicId !== null ||
    statusFilter !== "all";

  const queryParams = useMemo(
    () => ({
      pageNumber,
      pageSize: PAGE_SIZE,
      search: debouncedSearch || undefined,
      providerId: providerId ?? undefined,
      clinicId: clinicId ?? undefined,
      isActive: statusFilter === "all" ? null : statusFilter === "active",
      sortBy: "lastName",
      sortDir: "asc" as const,
    }),
    [pageNumber, debouncedSearch, providerId, clinicId, statusFilter],
  );

  const query = useQuery({
    queryKey: ["patient-charts", "search", queryParams],
    queryFn: () => searchPatients(queryParams),
    enabled: hasActiveFilter,
    placeholderData: keepPreviousData,
  });

  const data = query.data;
  const items = hasActiveFilter ? data?.items ?? [] : [];

  const clearFilters = () => {
    setSearch("");
    setProviderId(null);
    setClinicId(null);
    setStatusFilter("all");
  };

  return (
    <div className="space-y-4 sm:space-y-6">
      <EntityPageHeader
        icon={ClipboardList}
        title="Patient Chart"
        total={hasActiveFilter ? data?.totalCount ?? null : null}
        unit="patient"
        description="Search for a patient to open their clinical chart, or register a new one."
      >
        <Button
          onClick={() => setCreateOpen(true)}
          className="h-9 flex-1 gap-1.5 rounded-lg px-4 text-[13px] font-semibold sm:flex-none"
        >
          <Plus className="size-4" />
          New patient
        </Button>
      </EntityPageHeader>

      <EntitySearch
        value={search}
        onChange={setSearch}
        placeholder="Search by name or patient code…"
      />

      <div className="flex flex-wrap items-center gap-3">
        <div className="w-52">
          <Combobox
            id="filter-provider"
            label="Provider"
            value={providerId}
            onChange={setProviderId}
            options={providerOptions ?? []}
            placeholder="All providers"
            clearable
          />
        </div>
        <div className="w-52">
          <Combobox
            id="filter-clinic"
            label="Clinic"
            value={clinicId}
            onChange={setClinicId}
            options={clinicOptions ?? []}
            placeholder="All clinics"
            clearable
          />
        </div>
        <EntityFilterPill
          label="Status"
          value={statusFilter}
          onChange={setStatusFilter}
          options={[
            { value: "all", label: "All" },
            { value: "active", label: "Active" },
            { value: "inactive", label: "Inactive" },
          ]}
        />
        {hasActiveFilter && (
          <Button
            variant="outline"
            onClick={clearFilters}
            className="h-9 rounded-lg px-4 text-[13px]"
          >
            Clear
          </Button>
        )}
      </div>
      <p className="text-[11px] text-[var(--color-muted-foreground)]">
        Provider / Clinic match patients who have a report for that provider or clinic.
      </p>

      {!hasActiveFilter ? (
        <EntityEmpty
          icon={Search}
          title="Search for a patient"
          body="Search for a patient to open their chart, or register a new one."
          action={
            <Button
              onClick={() => setCreateOpen(true)}
              className="h-9 rounded-lg px-4 text-[13px]"
            >
              <Plus className="mr-1.5 size-4" />
              New patient
            </Button>
          }
        />
      ) : query.isLoading && items.length === 0 ? (
        <EntityListLoading desktopColumns={DESKTOP_COLS} />
      ) : items.length === 0 ? (
        <EntityEmpty
          icon={ClipboardList}
          title="No patients found"
          body="Nothing matches the current search and filters."
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
              <span>Date of birth</span>
              <span className="hidden lg:block">Last visit</span>
              <span />
            </EntityListHeader>

            {items.map((p, i) => (
              <EntityListRow key={p.id} className={DESKTOP_COLS} isLast={i === items.length - 1}>
                <div className="flex items-center gap-3 min-w-0">
                  <EntityInitialsAvatar name={fullName(p)} size={32} />
                  <span className="truncate text-[13px] font-medium">{fullName(p)}</span>
                </div>
                <span className="text-[13px] text-[var(--color-muted-foreground)]">
                  {p.patientCode}
                </span>
                <span className="text-[13px] text-[var(--color-muted-foreground)]">
                  {formatDate(p.dateOfBirth)}
                </span>
                <span className="hidden text-[13px] text-[var(--color-muted-foreground)] lg:block">
                  {formatDate(p.lastVisitDate)}
                </span>
                <Link
                  to={`/patient-charts/${p.id}`}
                  aria-label={`Open chart for ${fullName(p)}`}
                  className="flex items-center justify-end"
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

      {query.isError && (
        <div
          role="alert"
          className="rounded-lg border border-[oklch(from_var(--color-destructive)_l_c_h_/_0.30)] bg-[oklch(from_var(--color-destructive)_l_c_h_/_0.06)] px-3 py-2 text-sm text-[var(--color-destructive)]"
        >
          {describe(query.error)}
        </div>
      )}

      <CreatePatientDialog
        open={createOpen}
        onClose={() => setCreateOpen(false)}
        onCreated={(id) => navigate(`/patient-charts/${id}`)}
      />
    </div>
  );
}
