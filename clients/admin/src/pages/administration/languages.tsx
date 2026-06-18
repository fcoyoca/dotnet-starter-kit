import { useAuth } from "@/auth/use-auth";
import { languagesApi } from "@/api/administration";
import { AdministrationPermissions } from "@/lib/permissions";
import { LookupListPage } from "./LookupListPage";

export function LanguagesPage() {
  const { user } = useAuth();
  const perms = user?.permissions ?? [];
  return (
    <LookupListPage
      queryKey="administration.languages"
      label="Language"
      labelPlural="Languages"
      api={languagesApi}
      canCreate={perms.includes(AdministrationPermissions.Languages.Create)}
      canUpdate={perms.includes(AdministrationPermissions.Languages.Update)}
      canDelete={perms.includes(AdministrationPermissions.Languages.Delete)}
    />
  );
}
