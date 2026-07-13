import { apiFetch } from "@/lib/api-client";

export type TenantBrandingDto = {
  /** Resolved server-side: the theme's app name, else the tenant's own name.
   *  Null when the tenant has set neither — render the framework default. */
  appName?: string | null;
  logoUrl?: string | null;
  logoDarkUrl?: string | null;
  faviconUrl?: string | null;
};

/**
 * Branding for the signed-in user's tenant. Unlike `/tenants/theme` (which
 * needs Tenants.ViewTheme), this needs nothing but auth — every user has to
 * render the chrome.
 */
export function getMyBranding() {
  return apiFetch<TenantBrandingDto>("/api/v1/tenants/me/branding");
}
