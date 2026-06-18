import { useAuth } from "@/auth/use-auth";
import { contactMethodsApi } from "@/api/administration";
import { AdministrationPermissions } from "@/lib/permissions";
import { LookupListPage } from "./LookupListPage";

export function ContactMethodsPage() {
  const { user } = useAuth();
  const perms = user?.permissions ?? [];
  return (
    <LookupListPage
      queryKey="administration.contactMethods"
      label="Contact Method"
      labelPlural="Preferred Contact Methods"
      api={contactMethodsApi}
      canCreate={perms.includes(AdministrationPermissions.PreferredContactMethods.Create)}
      canUpdate={perms.includes(AdministrationPermissions.PreferredContactMethods.Update)}
      canDelete={perms.includes(AdministrationPermissions.PreferredContactMethods.Delete)}
    />
  );
}
