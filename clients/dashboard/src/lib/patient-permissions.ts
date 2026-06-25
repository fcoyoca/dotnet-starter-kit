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
