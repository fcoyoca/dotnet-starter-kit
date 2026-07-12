import { lazy, type ComponentType, type LazyExoticComponent } from "react";
import { administrationHubItems } from "@/components/layout/nav-data";

function lazyNamed<T extends Record<string, unknown>, K extends keyof T>(
  importer: () => Promise<T>,
  name: K,
): LazyExoticComponent<ComponentType> {
  return lazy(async () => {
    const mod = await importer();
    return { default: mod[name] as ComponentType<unknown> };
  });
}

/** slug (last path segment of each administrationHubItems `to`) -> lazy
 *  page component. Single place that decides which existing Administration
 *  page backs each hub card / dialog / `/administration/:section` deep link. */
export const ADMIN_SECTION_COMPONENTS: Record<string, LazyExoticComponent<ComponentType>> = {
  "allergy-reactions": lazyNamed(
    () => import("@/pages/administration/allergy-reactions"),
    "AllergyReactionsPage",
  ),
  clinics: lazyNamed(() => import("@/pages/administration/clinics"), "ClinicsPage"),
  "code-sources": lazyNamed(() => import("@/pages/administration/code-sources"), "CodeSourcesPage"),
  "custom-diagnostics": lazyNamed(
    () => import("@/pages/administration/custom-diagnostics"),
    "CustomDiagnosticsPage",
  ),
  departments: lazyNamed(() => import("@/pages/administration/departments"), "DepartmentsPage"),
  "diagnostic-categories": lazyNamed(
    () => import("@/pages/administration/diagnostic-categories"),
    "DiagnosticCategoriesPage",
  ),
  diagnostics: lazyNamed(() => import("@/pages/administration/diagnostics"), "DiagnosticsPage"),
  drugs: lazyNamed(() => import("@/pages/administration/drugs"), "DrugsPage"),
  "email-settings": lazyNamed(
    () => import("@/pages/administration/email-settings"),
    "EmailSettingsPage",
  ),
  "incident-types": lazyNamed(
    () => import("@/pages/administration/incident-types"),
    "IncidentTypesPage",
  ),
  "insurance-companies": lazyNamed(
    () => import("@/pages/administration/insurance-companies"),
    "InsuranceCompaniesPage",
  ),
  "insurance-types": lazyNamed(
    () => import("@/pages/administration/insurance-types"),
    "InsuranceTypesPage",
  ),
  macros: lazyNamed(() => import("@/pages/administration/macros"), "MacrosPage"),
  "medication-dose-units": lazyNamed(
    () => import("@/pages/administration/medication-dose-units"),
    "MedicationDoseUnitsPage",
  ),
  "patient-document-types": lazyNamed(
    () => import("@/pages/administration/patient-document-types"),
    "PatientDocumentTypesPage",
  ),
  "procedure-categories": lazyNamed(
    () => import("@/pages/administration/procedure-categories"),
    "ProcedureCategoriesPage",
  ),
  "procedure-codes": lazyNamed(
    () => import("@/pages/administration/procedure-codes"),
    "ProcedureCodesPage",
  ),
  providers: lazyNamed(() => import("@/pages/administration/providers"), "ProvidersPage"),
  schedule: lazyNamed(() => import("@/pages/administration/schedule"), "SchedulePage"),
};

export type AdminHubSection = {
  slug: string;
  label: string;
  icon: React.ComponentType<{ className?: string }>;
  perm?: string;
};

/** The Administration hub entries, derived from nav-data.ts's
 *  `administrationHubItems` (single source of truth for labels/icons/
 *  permission strings). The tenant-config pages Email Settings and Schedule
 *  are hub sections too — no Administration page lives outside the hub. */
export const ADMIN_HUB_SECTIONS: AdminHubSection[] = administrationHubItems.map((item) => ({
  slug: item.to.replace("/administration/", ""),
  label: item.label,
  icon: item.icon,
  perm: item.perm,
}));
