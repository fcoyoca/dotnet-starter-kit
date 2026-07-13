import { useState } from "react";
import { cn } from "@/lib/cn";
import { useBranding } from "@/components/branding/branding-context";

/**
 * The tenant's logo, or a monogram tile when they haven't uploaded one.
 *
 * `className` sizes/shapes both variants so callers get the same footprint
 * either way; `markClassName` carries the typography that only the monogram
 * needs. A logo that fails to load degrades to the monogram rather than
 * leaving a broken-image gap in the chrome.
 */
export function BrandLogo({
  className,
  markClassName,
}: {
  className?: string;
  markClassName?: string;
}) {
  const { logoUrl, monogram, appName } = useBranding();
  const [failed, setFailed] = useState(false);

  if (logoUrl && !failed) {
    return (
      <img
        src={logoUrl}
        alt={appName}
        onError={() => setFailed(true)}
        className={cn("shrink-0 object-contain", className)}
      />
    );
  }

  return (
    <span
      aria-hidden
      className={cn(
        "brand-mark grid shrink-0 place-items-center text-[var(--color-primary-foreground)]",
        className,
        markClassName,
      )}
    >
      {monogram}
    </span>
  );
}

/**
 * The tenant's app name. Keeps the two-tone "fullstack·hero" split only for
 * the framework default — splitting an arbitrary tenant name at a fixed
 * offset would be nonsense.
 */
export function BrandWordmark({ className }: { className?: string }) {
  const { appName, isDefaultName } = useBranding();

  if (isDefaultName) {
    return (
      <span className={className}>
        fullstack<span className="text-[var(--color-primary)]">hero</span>
      </span>
    );
  }

  return (
    <span className={className} title={appName}>
      {appName}
    </span>
  );
}
