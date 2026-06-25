import {
  Activity,
  AlertTriangle,
  Building,
  Building2,
  CalendarClock,
  ClipboardList,
  ClipboardPlus,
  CreditCard,
  FileText,
  FolderOpen,
  FolderTree,
  HeartPulse,
  Layers,
  Mail,
  LayoutDashboard,
  ListChecks,
  MessageCircle,
  Network,
  Package,
  Receipt,
  ScrollText,
  Settings,
  ShieldCheck,
  ShieldPlus,
  Stethoscope,
  Tags,
  Ticket,
  Trash2,
  Users,
  UsersRound,
  Wifi,
} from "lucide-react";
import { ALL_TRASH_PERMISSIONS } from "@/lib/trash-permissions";
import { INCIDENT_PERMISSIONS, PATIENT_PERMISSIONS } from "@/lib/patient-permissions";

export type NavSpec = {
  to: string;
  label: string;
  icon: React.ComponentType<{ className?: string }>;
  /**
   * Permission required to see this item. Items without a `perm` are visible to
   * every authenticated tenant user; gated items are hidden when the current
   * user (or impersonated user) lacks the permission, so they never land on a
   * page the API will reject with 403.
   */
  perm?: string;
  /**
   * Visible only if the user holds *at least one* of these permissions. Use for
   * an item that fronts several independently-gated sub-views (e.g. Trash, whose
   * tabs each require a different permission) — the entry should show as long as
   * the user can reach any one of them. Combined with `perm` via AND.
   */
  anyPerm?: readonly string[];
};

export type NavSection = {
  id: string;
  caption: string;
  /** Section-level icon used as a fallback when the sidebar is
   *  collapsed and the section is rendered as a stack of item icons. */
  icon: React.ComponentType<{ className?: string }>;
  items: NavSpec[];
};

// Top-level items live OUTSIDE any section. Overview opens the app;
// Settings is account-scoped and lives at the very bottom.
export const topNavTop: NavSpec[] = [
  { to: "/", label: "Overview", icon: LayoutDashboard },
  // Each gate mirrors the permission the page's primary list endpoint enforces
  // server-side (Chat → channels list, Files → /files/mine). Same convention
  // as trash-permissions.ts: if the endpoint's permission changes, mirror it.
  { to: "/chat", label: "Chat", icon: MessageCircle, perm: "Permissions.Chat.Channels.View" },
  { to: "/files", label: "My Files", icon: FolderOpen, perm: "Permissions.Files.Upload" },
];

export const topNavBottom: NavSpec[] = [
  { to: "/settings", label: "Settings", icon: Settings },
];

// Section accordion. Single-select — only one section open at a time.
export const sections: NavSection[] = [
  {
    id: "patients",
    caption: "Patients",
    icon: Stethoscope,
    items: [
      { to: "/patients", label: "Patients", icon: Stethoscope, perm: PATIENT_PERMISSIONS.view },
      { to: "/patient-charts", label: "Patient Chart", icon: ClipboardList, perm: INCIDENT_PERMISSIONS.view },
    ],
  },
  {
    id: "scheduling",
    caption: "Scheduling",
    icon: CalendarClock,
    items: [
      {
        to: "/scheduling/appointments",
        label: "Appointments",
        icon: CalendarClock,
        perm: "Permissions.Scheduling.Appointments.View",
      },
    ],
  },
  {
    id: "operations",
    caption: "Operations",
    icon: Activity,
    items: [
      // Live activity is SSE-backed; the stream is auth-only (no permission), so no gate.
      { to: "/activity", label: "Live activity", icon: Activity },
      { to: "/subscription", label: "Subscription", icon: CreditCard, perm: "Permissions.Billing.View" },
      { to: "/invoices", label: "Invoices", icon: Receipt, perm: "Permissions.Billing.View" },
    ],
  },
  {
    id: "administration",
    caption: "Administration",
    icon: Building2,
    items: [
      // Each gate mirrors the permission the page's list endpoint enforces
      // server-side (Administration.{Resource}.View). View is IsBasic, so members
      // can reach the page; create/update/delete actions hide for those lacking
      // the manage perms.
      {
        to: "/administration/clinics",
        label: "Clinics",
        icon: Building2,
        perm: "Permissions.Administration.Clinics.View",
      },
      {
        to: "/administration/departments",
        label: "Departments",
        icon: Network,
        perm: "Permissions.Administration.Departments.View",
      },
      {
        to: "/administration/providers",
        label: "Providers",
        icon: Stethoscope,
        perm: "Permissions.Administration.Providers.View",
      },
      {
        to: "/administration/insurance-types",
        label: "Insurance Types",
        icon: ShieldPlus,
        perm: "Permissions.Administration.InsuranceTypes.View",
      },
      {
        to: "/administration/insurance-companies",
        label: "Insurance Companies",
        icon: Building,
        perm: "Permissions.Administration.InsuranceCompanies.View",
      },
      {
        to: "/administration/diagnostic-categories",
        label: "Diagnostic Categories",
        icon: FolderTree,
        perm: "Permissions.Administration.DiagnosticCategories.View",
      },
      {
        to: "/administration/custom-diagnostics",
        label: "Custom Diagnostics",
        icon: ClipboardPlus,
        perm: "Permissions.Administration.CustomDiagnostics.View",
      },
      {
        to: "/administration/procedure-categories",
        label: "Procedure Categories",
        icon: Layers,
        perm: "Permissions.Administration.ProcedureCategories.View",
      },
      {
        to: "/administration/procedure-codes",
        label: "Procedure Codes",
        icon: ListChecks,
        perm: "Permissions.Administration.ProcedureCodes.View",
      },
      {
        to: "/administration/code-sources",
        label: "Code Sources",
        icon: Tags,
        perm: "Permissions.Administration.CodeSources.View",
      },
      {
        to: "/administration/incident-types",
        label: "Incident Types",
        icon: AlertTriangle,
        perm: "Permissions.Administration.IncidentTypes.View",
      },
      {
        to: "/administration/patient-document-types",
        label: "Patient Document Types",
        icon: FileText,
        perm: "Permissions.Administration.PatientDocumentTypes.View",
      },
      {
        to: "/administration/macros",
        label: "Macros",
        icon: ScrollText,
        perm: "Permissions.Administration.Macros.View",
      },
      {
        to: "/administration/schedule",
        label: "Schedule",
        icon: CalendarClock,
        perm: "Permissions.Administration.ScheduleConfig.View",
      },
      {
        to: "/administration/email-settings",
        label: "Email Settings",
        icon: Mail,
        perm: "Permissions.Administration.EmailSettings.View",
      },
    ],
  },
  {
    id: "catalog",
    caption: "Catalog",
    icon: Package,
    items: [
      { to: "/catalog/products", label: "Products", icon: Package, perm: "Permissions.Catalog.Products.View" },
      { to: "/catalog/brands", label: "Brands", icon: Tags, perm: "Permissions.Catalog.Brands.View" },
      { to: "/catalog/categories", label: "Categories", icon: FolderTree, perm: "Permissions.Catalog.Categories.View" },
    ],
  },
  {
    id: "helpdesk",
    caption: "Helpdesk",
    icon: Ticket,
    items: [
      { to: "/tickets", label: "Tickets", icon: Ticket, perm: "Permissions.Tickets.View" },
    ],
  },
  {
    id: "identity",
    caption: "Identity",
    icon: Users,
    items: [
      // Gate the identity-management pages on a manage permission (not View): View Users/Roles/Groups
      // are IsBasic so every member holds them (the chat/user picker relies on Users.View), but only
      // managers should see these admin pages. Basic lacks the *.Update perms, so the items hide for them.
      { to: "/identity/users", label: "Users", icon: Users, perm: "Permissions.Users.Update" },
      { to: "/identity/roles", label: "Roles", icon: ShieldCheck, perm: "Permissions.Roles.Update" },
      { to: "/identity/groups", label: "Groups", icon: UsersRound, perm: "Permissions.Groups.Update" },
    ],
  },
  {
    id: "system",
    caption: "System",
    icon: HeartPulse,
    items: [
      // Health hits the anonymous /health/ready probe — visible to everyone.
      { to: "/system/health", label: "Health", icon: HeartPulse },
      { to: "/system/audits", label: "Audit trail", icon: ScrollText, perm: "Permissions.AuditTrails.View" },
      { to: "/system/sessions", label: "Sessions", icon: Wifi, perm: "Permissions.Sessions.ViewAll" },
      // Trash fronts five tabs, each gated on a different resource's restore /
      // view-trash permission. Show the entry if the user can reach any tab; the
      // page hides the individual tabs they can't (see trash-permissions.ts).
      { to: "/system/trash", label: "Trash", icon: Trash2, anyPerm: ALL_TRASH_PERMISSIONS },
    ],
  },
];

/** True when the user satisfies the item's gates: the single `perm` (if any)
 *  AND at least one of `anyPerm` (if any). Ungated items are always visible. */
function isNavItemVisible(item: NavSpec, permissions: readonly string[]): boolean {
  if (item.perm && !permissions.includes(item.perm)) return false;
  if (item.anyPerm && !item.anyPerm.some((p) => permissions.includes(p))) return false;
  return true;
}

/** Drop items the user can't access, then drop any section left empty. */
export function visibleSections(permissions: readonly string[]): NavSection[] {
  return sections
    .map((s) => ({ ...s, items: s.items.filter((i) => isNavItemVisible(i, permissions)) }))
    .filter((s) => s.items.length > 0);
}

/** Filter a flat nav list (top/bottom) by permission. */
export function visibleItems(items: NavSpec[], permissions: readonly string[]): NavSpec[] {
  return items.filter((i) => isNavItemVisible(i, permissions));
}

/** Find the section whose items contain the given path (best prefix match). */
export function findSectionForPath(pathname: string): string | null {
  let bestId: string | null = null;
  let bestLen = 0;
  for (const s of sections) {
    for (const item of s.items) {
      if (
        (item.to === "/" && pathname === "/") ||
        (item.to !== "/" && pathname.startsWith(item.to))
      ) {
        if (item.to.length > bestLen) {
          bestLen = item.to.length;
          bestId = s.id;
        }
      }
    }
  }
  return bestId;
}
