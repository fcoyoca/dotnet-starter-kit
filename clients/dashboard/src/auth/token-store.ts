// Tokens live in sessionStorage: per-tab, cleared when the tab closes, and
// never written to the browser's on-disk sessionStorage store. Each tab is its
// own session — signing in/out in one tab does not affect another.
const ACCESS_KEY = "fsh.dashboard.accessToken";
const REFRESH_KEY = "fsh.dashboard.refreshToken";
const TENANT_KEY = "fsh.dashboard.tenant";
const PERMS_KEY = "fsh.dashboard.permissions";

// Impersonation stash. While an operator is impersonating another user,
// the live token store holds the impersonation tokens; the operator's
// original tokens sit under these keys so the End flow can fall back to
// them locally if the server-side End call fails (e.g. network down).
const STASH_ACCESS_KEY = "fsh.dashboard.impersonation.actorAccessToken";
const STASH_REFRESH_KEY = "fsh.dashboard.impersonation.actorRefreshToken";
const STASH_TENANT_KEY = "fsh.dashboard.impersonation.actorTenant";

type Listener = () => void;

const listeners = new Set<Listener>();

function emit() {
  for (const listener of listeners) listener();
}

export const tokenStore = {
  getAccessToken: () => sessionStorage.getItem(ACCESS_KEY),
  getRefreshToken: () => sessionStorage.getItem(REFRESH_KEY),
  getTenant: () => sessionStorage.getItem(TENANT_KEY),

  /**
   * Permissions are fetched separately from the JWT (the token only carries
   * role names — see GetCurrentUserPermissionsEndpoint server-side). Cached
   * here so gated UI can read them synchronously; re-hydrated on each login
   * and whenever the signed-in subject changes (incl. impersonation swaps).
   */
  getPermissions(): string[] {
    try {
      const raw = sessionStorage.getItem(PERMS_KEY);
      if (!raw) return [];
      const parsed = JSON.parse(raw) as unknown;
      return Array.isArray(parsed) ? parsed.filter((p): p is string => typeof p === "string") : [];
    } catch {
      return [];
    }
  },

  setPermissions(permissions: string[]) {
    sessionStorage.setItem(PERMS_KEY, JSON.stringify(permissions));
    emit();
  },

  setTokens(accessToken: string, refreshToken: string) {
    sessionStorage.setItem(ACCESS_KEY, accessToken);
    sessionStorage.setItem(REFRESH_KEY, refreshToken);
    emit();
  },

  setTenant(tenant: string) {
    sessionStorage.setItem(TENANT_KEY, tenant);
    emit();
  },

  clear() {
    sessionStorage.removeItem(ACCESS_KEY);
    sessionStorage.removeItem(REFRESH_KEY);
    sessionStorage.removeItem(PERMS_KEY);
    // Also clear any impersonation stash so a fresh login doesn't
    // inherit half of a previous operator's session.
    sessionStorage.removeItem(STASH_ACCESS_KEY);
    sessionStorage.removeItem(STASH_REFRESH_KEY);
    sessionStorage.removeItem(STASH_TENANT_KEY);
    emit();
  },

  /**
   * Swap the active token to an impersonation access token while preserving
   * the original operator's tokens locally. The impersonation token has no
   * refresh counterpart server-side, so we drop the refresh slot — auto-
   * refresh in the api client checks for refreshToken presence and will
   * skip silently (impersonation sessions are intentionally short-lived).
   */
  beginImpersonation(impersonationAccessToken: string, impersonatedTenant: string | null) {
    const access = sessionStorage.getItem(ACCESS_KEY);
    const refresh = sessionStorage.getItem(REFRESH_KEY);
    const tenant = sessionStorage.getItem(TENANT_KEY);
    if (access) sessionStorage.setItem(STASH_ACCESS_KEY, access);
    if (refresh) sessionStorage.setItem(STASH_REFRESH_KEY, refresh);
    if (tenant) sessionStorage.setItem(STASH_TENANT_KEY, tenant);

    sessionStorage.setItem(ACCESS_KEY, impersonationAccessToken);
    sessionStorage.removeItem(REFRESH_KEY);
    // Drop the operator's permissions — the impersonated subject has its own;
    // the auth context re-hydrates on the subject change.
    sessionStorage.removeItem(PERMS_KEY);
    if (impersonatedTenant) sessionStorage.setItem(TENANT_KEY, impersonatedTenant);
    emit();
  },

  /**
   * Replace the live tokens with a fresh actor pair returned by the End
   * Impersonation endpoint, and clear the stash. Use this on End success.
   */
  endImpersonationWithFreshTokens(accessToken: string, refreshToken: string) {
    const stashTenant = sessionStorage.getItem(STASH_TENANT_KEY);
    sessionStorage.setItem(ACCESS_KEY, accessToken);
    sessionStorage.setItem(REFRESH_KEY, refreshToken);
    sessionStorage.removeItem(PERMS_KEY);
    if (stashTenant) sessionStorage.setItem(TENANT_KEY, stashTenant);
    sessionStorage.removeItem(STASH_ACCESS_KEY);
    sessionStorage.removeItem(STASH_REFRESH_KEY);
    sessionStorage.removeItem(STASH_TENANT_KEY);
    emit();
  },

  /**
   * Last-resort local restore — used if the End endpoint fails. Reinstall
   * the stashed actor tokens so the operator at least has *some* session
   * (the original access token may itself be expired by now, in which
   * case auto-refresh with the stashed refresh token kicks in).
   */
  restoreStashedActor(): boolean {
    const access = sessionStorage.getItem(STASH_ACCESS_KEY);
    const refresh = sessionStorage.getItem(STASH_REFRESH_KEY);
    const tenant = sessionStorage.getItem(STASH_TENANT_KEY);
    if (!access) return false;
    sessionStorage.setItem(ACCESS_KEY, access);
    if (refresh) sessionStorage.setItem(REFRESH_KEY, refresh);
    sessionStorage.removeItem(PERMS_KEY);
    if (tenant) sessionStorage.setItem(TENANT_KEY, tenant);
    sessionStorage.removeItem(STASH_ACCESS_KEY);
    sessionStorage.removeItem(STASH_REFRESH_KEY);
    sessionStorage.removeItem(STASH_TENANT_KEY);
    emit();
    return true;
  },

  hasImpersonationStash: () => sessionStorage.getItem(STASH_ACCESS_KEY) !== null,

  subscribe(listener: Listener) {
    listeners.add(listener);
    return () => {
      listeners.delete(listener);
    };
  },
};
