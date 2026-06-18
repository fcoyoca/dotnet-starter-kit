import { useAuth } from "@/auth/use-auth";
import { racesApi } from "@/api/administration";
import { AdministrationPermissions } from "@/lib/permissions";
import { LookupListPage } from "./LookupListPage";

export function RacesPage() {
  const { user } = useAuth();
  const perms = user?.permissions ?? [];
  return (
    <LookupListPage
      queryKey="administration.races"
      label="Race"
      labelPlural="Races"
      api={racesApi}
      canCreate={perms.includes(AdministrationPermissions.Races.Create)}
      canUpdate={perms.includes(AdministrationPermissions.Races.Update)}
      canDelete={perms.includes(AdministrationPermissions.Races.Delete)}
    />
  );
}
