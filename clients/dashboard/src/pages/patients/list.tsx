import { useEffect, useMemo, useState } from "react";
import { Link } from "react-router-dom";
import {
  keepPreviousData,
  useQuery,
} from "@tanstack/react-query";
import { ChevronRight, Plus, Stethoscope } from "lucide-react";
import {
  searchPatients,
  type PatientListItemDto,
} from "@/api/patients";
import { Button } from "@/components/ui/button";
import {
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
  EntityStatusBadge,
} from "@/components/list";
import { describe, formatDate } from "@/lib/list-helpers";
import { CreatePatientDialog } from "@/pages/patients/create-patient-dialog";

const PAGE_SIZE = 20;

type StatusFilter = "all" | "active" | "inactive";

// Desktop grid template, shared by header + rows + skeleton.
const DESKTOP_COLS = "grid-cols-[1fr_140px_24px] lg:grid-cols-[1.6fr_140px_180px_24px]";

function fullName(p: PatientListItemDto): string {
  const parts = [p.firstName, p.middleInitial, p.lastName].filter(Boolean);
  return parts.join(" ");
}

// ───────────────────────────────────────────────────────────────────────
//  Page
// ───────────────────────────────────────────────────────────────────────

export function PatientsListPage() {
  const [search, setSearch] = useState("");
  const [debouncedSearch, setDebouncedSearch] = useState("");
  const [pageNumber, setPageNumber] = useState(1);
  const [statusFilter, setStatusFilter] = useState<StatusFilter>("all");
  const [createOpen, setCreateOpen] = useState(false);

  useEffect(() => {
    const t = setTimeout(() => {
      setDebouncedSearch(search.trim());
      setPageNumber(1);
    }, 250);
    return () => clearTimeout(t);
  }, [search]);

  useEffect(() => {
    setPageNumber(1);
  }, [statusFilter]);

  const queryParams = useMemo(
    () => ({
      pageNumber,
      pageSize: PAGE_SIZE,
      search: debouncedSearch || undefined,
      isActive: statusFilter === "all" ? null : statusFilter === "active",
      sortBy: "lastName",
      sortDir: "asc" as const,
    }),
    [pageNumber, debouncedSearch, statusFilter],
  );

  const query = useQuery({
    queryKey: ["patients", "list", queryParams],
    queryFn: () => searchPatients(queryParams),
    placeholderData: keepPreviousData,
  });

  const data = query.data;
  const items = data?.items ?? [];

  const searchActive = debouncedSearch.length > 0 || statusFilter !== "all";

  const clearFilters = () => {
    setSearch("");
    setStatusFilter("all");
  };

  return (
    <div className="space-y-4 sm:space-y-6">
      <EntityPageHeader
        icon={Stethoscope}
        title="Patients"
        total={data?.totalCount ?? null}
        unit="patient"
        description="Every patient on file for this clinic. Register newcomers and manage their records."
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

      <div className="flex flex-wrap items-center gap-2">
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
      </div>

      {query.isLoading && items.length === 0 ? (
        <EntityListLoading desktopColumns={DESKTOP_COLS} />
      ) : items.length === 0 ? (
        <EntityEmpty
          icon={Stethoscope}
          title={searchActive ? "No patients found" : "No patients yet"}
          body={
            searchActive
              ? debouncedSearch
                ? `Nothing matches "${debouncedSearch}". Try a different term or clear the filters.`
                : "No patients match the current filters."
              : "Register the first patient to get started."
          }
          action={
            searchActive ? (
              <Button
                variant="outline"
                onClick={clearFilters}
                className="h-9 rounded-lg px-4 text-[13px]"
              >
                Clear filters
              </Button>
            ) : (
              <Button
                onClick={() => setCreateOpen(true)}
                className="h-9 rounded-lg px-4 text-[13px]"
              >
                <Plus className="mr-1.5 size-4" />
                New patient
              </Button>
            )
          }
        />
      ) : (
        <div>
          <div className="mb-3 flex items-center justify-between">
            <p className="text-[12px] font-medium text-[var(--color-muted-foreground)]">
              {data?.totalCount ?? 0} patient
              {(data?.totalCount ?? 0) !== 1 ? "s" : ""} found
            </p>
          </div>

          {/* Mobile: card list */}
          <div className="space-y-2 md:hidden">
            {items.map((patient) => (
              <PatientMobileCard key={patient.id} patient={patient} />
            ))}
          </div>

          {/* Desktop: table */}
          <EntityListCard className="hidden md:block">
            <EntityListHeader className={DESKTOP_COLS}>
              <span>Name</span>
              <span>Patient code</span>
              <span className="hidden lg:block">Date of birth</span>
              <span />
            </EntityListHeader>

            {items.map((patient, i) => (
              <PatientDesktopRow
                key={patient.id}
                patient={patient}
                isLast={i === items.length - 1}
              />
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

      <CreatePatientDialog open={createOpen} onClose={() => setCreateOpen(false)} />
    </div>
  );
}

// ───────────────────────────────────────────────────────────────────────
//  Rows
// ───────────────────────────────────────────────────────────────────────

function PatientMobileCard({ patient }: { patient: PatientListItemDto }) {
  const display = fullName(patient);
  return (
    <EntityMobileCard
      href={`/patients/${patient.id}`}
      aria-label={`Open patient ${display}`}
      dim={!patient.isActive}
    >
      <div className="flex items-center justify-between">
        <div className="flex min-w-0 items-center gap-3">
          <EntityInitialsAvatar name={display} size={40} />
          <div className="min-w-0">
            <p className="truncate text-[14px] font-medium text-[var(--color-foreground)]">
              {display}
            </p>
            <p className="mt-0.5 truncate text-[11px] text-[var(--color-muted-foreground)]">
              {patient.patientCode}
            </p>
          </div>
        </div>
        <ChevronRight className="size-4 shrink-0 text-[var(--color-border)]" />
      </div>
      <div className="mt-2 ml-[52px] flex flex-wrap items-center gap-1.5">
        <EntityStatusBadge tone={patient.isActive ? "success" : "default"}>
          {patient.isActive ? "Active" : "Inactive"}
        </EntityStatusBadge>
        <EntityStatusBadge tone="info">{formatDate(patient.dateOfBirth)}</EntityStatusBadge>
      </div>
    </EntityMobileCard>
  );
}

function PatientDesktopRow({
  patient,
  isLast,
}: {
  patient: PatientListItemDto;
  isLast: boolean;
}) {
  const display = fullName(patient);
  return (
    <EntityListRow className={DESKTOP_COLS} isLast={isLast} dim={!patient.isActive}>
      {/* Name + email */}
      <Link
        to={`/patients/${patient.id}`}
        className="flex min-w-0 items-center gap-3 outline-none"
      >
        <EntityInitialsAvatar name={display} size={36} />
        <div className="min-w-0">
          <span className="block truncate text-[14px] font-medium text-[var(--color-foreground)] transition-colors group-hover:text-[var(--color-primary)]">
            {display}
          </span>
          <span className="block truncate text-[12px] text-[var(--color-muted-foreground)]">
            {patient.email ?? "no email on file"}
          </span>
        </div>
      </Link>

      {/* Patient code */}
      <code
        title={patient.patientCode}
        className="truncate font-mono text-[12px] text-[var(--color-muted-foreground)]"
      >
        {patient.patientCode}
      </code>

      {/* DOB + status (lg+) */}
      <div className="hidden items-center gap-1.5 lg:flex">
        <span className="text-[13px] text-[var(--color-foreground)]">
          {formatDate(patient.dateOfBirth)}
        </span>
        <EntityStatusBadge tone={patient.isActive ? "success" : "default"}>
          {patient.isActive ? "Active" : "Inactive"}
        </EntityStatusBadge>
      </div>

      <div className="flex items-center justify-end">
        <ChevronRight className="size-4 text-[var(--color-border)] transition-colors group-hover:text-[var(--color-muted-foreground)]" />
      </div>
    </EntityListRow>
  );
}

