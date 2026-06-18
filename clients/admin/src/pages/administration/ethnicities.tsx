import { useAuth } from "@/auth/use-auth";
import { ethnicitiesApi } from "@/api/administration";
import { AdministrationPermissions } from "@/lib/permissions";
import { LookupListPage } from "./LookupListPage";

export function EthnicitiesPage() {
  const { user } = useAuth();
  const perms = user?.permissions ?? [];
  return (
    <LookupListPage
      queryKey="administration.ethnicities"
      label="Ethnicity"
      labelPlural="Ethnicities"
      api={ethnicitiesApi}
      canCreate={perms.includes(AdministrationPermissions.Ethnicities.Create)}
      canUpdate={perms.includes(AdministrationPermissions.Ethnicities.Update)}
      canDelete={perms.includes(AdministrationPermissions.Ethnicities.Delete)}
    />
  );
}
