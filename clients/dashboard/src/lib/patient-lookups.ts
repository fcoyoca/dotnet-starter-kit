import type { ComboboxOption } from "@/components/list";

// ─── Sourced from the legacy Bronston DB ((localdb)\mssqllocaldb) ─────────

/** `Bronston.dbo.Patients.pSex`. */
export const GENDER_OPTIONS: ComboboxOption[] = [
  { value: "M", label: "Male" },
  { value: "F", label: "Female" },
  { value: "UN", label: "Unknown" },
];

/** `Bronston.dbo.Patients.pMaritalStatus`. */
export const MARITAL_STATUS_OPTIONS: ComboboxOption[] = [
  { value: "S", label: "Single" },
  { value: "M", label: "Married" },
  { value: "D", label: "Divorced" },
  { value: "W", label: "Widowed" },
  { value: "O", label: "Other" },
];

/**
 * `BronstonAuthenticatingDB.dbo.RoleCode` where `isRelationValue = 1` — the
 * HL7 v3 PersonalRelationshipRoleType value set (105 rows). `value` is the
 * role code (`ConceptCode`) — the unique key for the relation — and doubles
 * as `nextOfKinRelationRoleCode`; `label` is `PreferedConceptName`. Selecting
 * a Relation option must set both `nextOfKinRelation` (label) and
 * `nextOfKinRelationRoleCode` (value) together — Role Code is never typed
 * independently. See [[findRelationOption]].
 */
export const RELATION_OPTIONS: ComboboxOption[] = [
  { value: "CHLDADOPT", label: "Adopted child" },
  { value: "DAUADOPT", label: "Adopted daughter" },
  { value: "SONADOPT", label: "Adopted son" },
  { value: "ADOPTF", label: "Adoptive father" },
  { value: "ADOPTM", label: "Adoptive mother" },
  { value: "ADOPTP", label: "Adoptive parent" },
  { value: "AUNT", label: "Aunt" },
  { value: "BRO", label: "Brother" },
  { value: "BROINLAW", label: "Brother-in-law" },
  { value: "CHILD", label: "Child" },
  { value: "CHLDINLAW", label: "Child-in-law" },
  { value: "COUSN", label: "Cousin" },
  { value: "DAUC", label: "Daughter" },
  { value: "DAUINLAW", label: "Daughter in-law" },
  { value: "DOMPART", label: "Domestic partner" },
  { value: "EXT", label: "Extended family member" },
  { value: "FAMMEMB", label: "Family member" },
  { value: "FTH", label: "Father" },
  { value: "FTHINLAW", label: "Father-in-law" },
  { value: "CHLDFOST", label: "Foster child" },
  { value: "DAUFOST", label: "Foster daughter" },
  { value: "SONFOST", label: "Foster son" },
  { value: "FTWIN", label: "Fraternal twin" },
  { value: "FTWINBRO", label: "Fraternal twin brother" },
  { value: "FTWINSIS", label: "Fraternal twin sister" },
  { value: "GESTM", label: "Gestational mother" },
  { value: "GRNDCHILD", label: "Grandchild" },
  { value: "GRNDDAU", label: "Granddaughter" },
  { value: "GRFTH", label: "Grandfather" },
  { value: "GRMTH", label: "Grandmother" },
  { value: "GRPRN", label: "Grandparent" },
  { value: "GRNDSON", label: "Grandson" },
  { value: "GGRFTH", label: "Great grandfather" },
  { value: "GGRMTH", label: "Great grandmother" },
  { value: "GGRPRN", label: "Great grandparent" },
  { value: "HBRO", label: "Half-brother" },
  { value: "HSIB", label: "Half-sibling" },
  { value: "HSIS", label: "Half-sister" },
  { value: "HUSB", label: "Husband" },
  { value: "ITWIN", label: "Identical twin" },
  { value: "ITWINBRO", label: "Identical twin brother" },
  { value: "ITWINSIS", label: "Identical twin sister" },
  { value: "INLAW", label: "Inlaw" },
  { value: "MAUNT", label: "Maternal aunt" },
  { value: "MCOUSN", label: "Maternal cousin" },
  { value: "MGRFTH", label: "Maternal grandfather" },
  { value: "MGRMTH", label: "Maternal grandmother" },
  { value: "MGRPRN", label: "Maternal grandparent" },
  { value: "MGGRFTH", label: "Maternal great-grandfather" },
  { value: "MGGRMTH", label: "Maternal great-grandmother" },
  { value: "MGGRPRN", label: "Maternal great-grandparent" },
  { value: "MUNCLE", label: "Maternal uncle" },
  { value: "MTH", label: "Mother" },
  { value: "MTHINLAW", label: "Mother-in-law" },
  { value: "NBRO", label: "Natural brother" },
  { value: "NCHILD", label: "Natural child" },
  { value: "DAU", label: "Natural daughter" },
  { value: "NFTH", label: "Natural father" },
  { value: "NFTHF", label: "Natural father of fetus" },
  { value: "NMTH", label: "Natural mother" },
  { value: "NMTHF", label: "Natural mother of fetus" },
  { value: "NPRN", label: "Natural parent" },
  { value: "NSIB", label: "Natural sibling" },
  { value: "NSIS", label: "Natural sister" },
  { value: "SON", label: "Natural son" },
  { value: "NBOR", label: "Neighbor" },
  { value: "NEPHEW", label: "Nephew" },
  { value: "NIECE", label: "Niece" },
  { value: "NIENEPH", label: "Niece/nephew" },
  { value: "PRN", label: "Parent" },
  { value: "PRNINLAW", label: "Parent in-law" },
  { value: "PAUNT", label: "Paternal aunt" },
  { value: "PCOUSN", label: "Paternal cousin" },
  { value: "PGRFTH", label: "Paternal grandfather" },
  { value: "PGRMTH", label: "Paternal grandmother" },
  { value: "PGRPRN", label: "Paternal grandparent" },
  { value: "PGGRFTH", label: "Paternal great-grandfather" },
  { value: "PGGRMTH", label: "Paternal great-grandmother" },
  { value: "PGGRPRN", label: "Paternal great-grandparent" },
  { value: "PUNCLE", label: "Paternal uncle" },
  { value: "ROOM", label: "Roommate" },
  { value: "ONESELF", label: "Self" },
  { value: "SIB", label: "Sibling" },
  { value: "SIBINLAW", label: "Sibling in-law" },
  { value: "SIGOTHR", label: "Significant other" },
  { value: "SIS", label: "Sister" },
  { value: "SISINLAW", label: "Sister-in-law" },
  { value: "SONC", label: "Son" },
  { value: "SONINLAW", label: "Son in-law" },
  { value: "SPS", label: "Spouse" },
  { value: "STPCHLD", label: "Step child" },
  { value: "STPPRN", label: "Step parent" },
  { value: "STPSIB", label: "Step sibling" },
  { value: "STPBRO", label: "Stepbrother" },
  { value: "STPDAU", label: "Stepdaughter" },
  { value: "STPFTH", label: "Stepfather" },
  { value: "STPMTH", label: "Stepmother" },
  { value: "STPSIS", label: "Stepsister" },
  { value: "STPSON", label: "Stepson" },
  { value: "TWIN", label: "Twin" },
  { value: "TWINBRO", label: "Twin brother" },
  { value: "TWINSIS", label: "Twin sister" },
  { value: "UNCLE", label: "Uncle" },
  { value: "FRND", label: "Unrelated friend" },
  { value: "WIFE", label: "Wife" },
];

export function findRelationOption(roleCode: string | null | undefined): ComboboxOption | undefined {
  if (!roleCode) return undefined;
  return RELATION_OPTIONS.find((option) => option.value === roleCode);
}

// Race/Ethnicity/Language/SmokingStatus/PreferredContactMethod/ReferralType
// are managed by the Administration module and served live from the API.
// Import hooks from @/api/administration and call them inside components.
export {
  useRaceOptions,
  useEthnicityOptions,
  useLanguageOptions,
  useSmokingStatusOptions,
  useContactMethodOptions,
  useReferralTypeOptions,
} from "@/api/administration";
