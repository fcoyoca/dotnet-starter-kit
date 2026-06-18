import { useAuth } from "@/auth/use-auth";
import { referralTypesApi } from "@/api/administration";
import { AdministrationPermissions } from "@/lib/permissions";
import { LookupListPage } from "./LookupListPage";

export function ReferralTypesPage() {
  const { user } = useAuth();
  const perms = user?.permissions ?? [];
  return (
    <LookupListPage
      queryKey="administration.referralTypes"
      label="Referral Type"
      labelPlural="Referral Types"
      api={referralTypesApi}
      canCreate={perms.includes(AdministrationPermissions.ReferralTypes.Create)}
      canUpdate={perms.includes(AdministrationPermissions.ReferralTypes.Update)}
      canDelete={perms.includes(AdministrationPermissions.ReferralTypes.Delete)}
    />
  );
}
