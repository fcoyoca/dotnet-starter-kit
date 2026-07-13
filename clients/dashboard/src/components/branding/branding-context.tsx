import { createContext, useContext, useMemo, type ReactNode } from "react";
import { useQuery } from "@tanstack/react-query";
import { getMyBranding } from "@/api/branding";
import { useAuth } from "@/auth/use-auth";
import { useTheme } from "@/components/theme/theme-provider";

/** Framework fallbacks — what we render when a tenant has set no branding. */
export const DEFAULT_APP_NAME = "fullstackhero";
export const DEFAULT_LOGO_URL = "/logo-fullstackhero.png";

export type Branding = {
  /** The tenant's app name, or the framework default. Never empty. */
  appName: string;
  /** True when `appName` is the framework default — lets the wordmark keep
   *  its "fullstack·hero" two-tone split, which is meaningless for a real
   *  tenant name. */
  isDefaultName: boolean;
  /** Logo to render, already resolved for the active light/dark theme.
   *  Null when the tenant uploaded none — callers fall back to a monogram. */
  logoUrl: string | null;
  /** Single letter shown when there's no logo. */
  monogram: string;
};

const BrandingContext = createContext<Branding | null>(null);

const DEFAULT_BRANDING: Branding = {
  appName: DEFAULT_APP_NAME,
  isDefaultName: true,
  logoUrl: null,
  monogram: "F",
};

/**
 * Tenant branding for the app chrome.
 *
 * Fetches only once the user is authenticated — the endpoint resolves the
 * tenant from the caller's token, so there's nothing to ask for before login.
 * Unauthenticated renders (the login page lives under this provider) get the
 * framework defaults, which is exactly what we want there.
 */
export function BrandingProvider({ children }: { children: ReactNode }) {
  const { isAuthenticated } = useAuth();
  const { resolved } = useTheme();

  const { data } = useQuery({
    queryKey: ["tenant", "branding"],
    queryFn: () => getMyBranding(),
    enabled: isAuthenticated,
    // Branding changes rarely and is operator-driven; a stale wordmark for a
    // few minutes is harmless, and this sits on every authenticated page.
    staleTime: 5 * 60_000,
    retry: false,
  });

  const value = useMemo<Branding>(() => {
    if (!data) return DEFAULT_BRANDING;

    const appName = data.appName?.trim() || DEFAULT_APP_NAME;
    // Fall back to the light logo in dark mode when no dark variant was
    // uploaded — a tenant with one logo still beats no logo.
    const logoUrl =
      (resolved === "dark" ? data.logoDarkUrl || data.logoUrl : data.logoUrl) || null;

    return {
      appName,
      isDefaultName: appName === DEFAULT_APP_NAME,
      logoUrl,
      monogram: appName.charAt(0).toUpperCase(),
    };
  }, [data, resolved]);

  return <BrandingContext.Provider value={value}>{children}</BrandingContext.Provider>;
}

export function useBranding(): Branding {
  const ctx = useContext(BrandingContext);
  if (!ctx) throw new Error("useBranding must be used within BrandingProvider");
  return ctx;
}
