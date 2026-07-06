/**
 * Permissions for the Patient feature. Mirrors the server registry
 * (`PatientPermissions.Patients` in `Modules.Patient.Contracts`) — convention
 * follows `Permissions.{Resource}.{Action}`, same shape as
 * [[trash-permissions]]'s `TRASH_TAB_PERMISSIONS`. If the server permission
 * set changes, mirror it here.
 */
export const PATIENT_PERMISSIONS = {
  view: "Permissions.Patient.Patients.View",
  create: "Permissions.Patient.Patients.Create",
  update: "Permissions.Patient.Patients.Update",
  delete: "Permissions.Patient.Patients.Delete",
  restore: "Permissions.Patient.Patients.Restore",
} as const;

export type PatientPermissionKey = keyof typeof PATIENT_PERMISSIONS;

/** Flat list of every Patient permission. */
export const ALL_PATIENT_PERMISSIONS: readonly string[] = Object.values(PATIENT_PERMISSIONS);

export const INCIDENT_PERMISSIONS = {
  view:   "Permissions.Patient.Incidents.View",
  create: "Permissions.Patient.Incidents.Create",
  update: "Permissions.Patient.Incidents.Update",
  close:  "Permissions.Patient.Incidents.Close",
  delete: "Permissions.Patient.Incidents.Delete",
} as const;

export type IncidentPermissionKey = keyof typeof INCIDENT_PERMISSIONS;

export const REPORT_PERMISSIONS = {
  view:   "Permissions.Patient.Reports.View",
  create: "Permissions.Patient.Reports.Create",
  update: "Permissions.Patient.Reports.Update",
  sign:   "Permissions.Patient.Reports.Sign",
  review: "Permissions.Patient.Reports.Review",
  export: "Permissions.Patient.Reports.Export",
  delete: "Permissions.Patient.Reports.Delete",
} as const;

export type ReportPermissionKey = keyof typeof REPORT_PERMISSIONS;

export const PROBLEM_PERMISSIONS = {
  view:   "Permissions.Patient.Problems.View",
  create: "Permissions.Patient.Problems.Create",
  update: "Permissions.Patient.Problems.Update",
  delete: "Permissions.Patient.Problems.Delete",
} as const;

export type ProblemPermissionKey = keyof typeof PROBLEM_PERMISSIONS;

export const ALLERGY_PERMISSIONS = {
  view:   "Permissions.Patient.Allergies.View",
  create: "Permissions.Patient.Allergies.Create",
  update: "Permissions.Patient.Allergies.Update",
  delete: "Permissions.Patient.Allergies.Delete",
} as const;

export type AllergyPermissionKey = keyof typeof ALLERGY_PERMISSIONS;

export const MEDICATION_PERMISSIONS = {
  view:   "Permissions.Patient.Medications.View",
  create: "Permissions.Patient.Medications.Create",
  update: "Permissions.Patient.Medications.Update",
  delete: "Permissions.Patient.Medications.Delete",
} as const;

export type MedicationPermissionKey = keyof typeof MEDICATION_PERMISSIONS;

export const NOTE_PERMISSIONS = {
  view:   "Permissions.Patient.Notes.View",
  create: "Permissions.Patient.Notes.Create",
  update: "Permissions.Patient.Notes.Update",
  delete: "Permissions.Patient.Notes.Delete",
} as const;

export type NotePermissionKey = keyof typeof NOTE_PERMISSIONS;

export const DOCUMENT_PERMISSIONS = {
  view:     "Permissions.Patient.Documents.View",
  create:   "Permissions.Patient.Documents.Create",
  update:   "Permissions.Patient.Documents.Update",
  download: "Permissions.Patient.Documents.Download",
  delete:   "Permissions.Patient.Documents.Delete",
} as const;

export type DocumentPermissionKey = keyof typeof DOCUMENT_PERMISSIONS;

export const SUPERBILL_PERMISSIONS = {
  view:   "Permissions.Patient.SuperBills.View",
  manage: "Permissions.Patient.SuperBills.Manage",
} as const;

export type SuperBillPermissionKey = keyof typeof SUPERBILL_PERMISSIONS;
