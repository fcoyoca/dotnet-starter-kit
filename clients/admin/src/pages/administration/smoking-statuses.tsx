import { useAuth } from "@/auth/use-auth";
import { smokingStatusesApi } from "@/api/administration";
import { AdministrationPermissions } from "@/lib/permissions";
import { LookupListPage } from "./LookupListPage";

export function SmokingStatusesPage() {
  const { user } = useAuth();
  const perms = user?.permissions ?? [];
  return (
    <LookupListPage
      queryKey="administration.smokingStatuses"
      label="Smoking Status"
      labelPlural="Smoking Statuses"
      api={smokingStatusesApi}
      withSnomedCode
      canCreate={perms.includes(AdministrationPermissions.SmokingStatuses.Create)}
      canUpdate={perms.includes(AdministrationPermissions.SmokingStatuses.Update)}
      canDelete={perms.includes(AdministrationPermissions.SmokingStatuses.Delete)}
    />
  );
}
