import { useEffect, useRef, useState } from "react";

/**
 * Anti-flicker wrapper for loading flags. Returns `true` for at least
 * `minMs` once `active` first becomes truthy, so a query that resolves
 * almost instantly doesn't flash its skeleton on and back off.
 *
 * Without this, an `isLoading` that's true for ~40ms paints a skeleton
 * that vanishes before the eye settles — a jittery flash. Holding the
 * flag for a short minimum makes the transition read as deliberate.
 *
 * Usage:
 *   const showSkeleton = useMinLoading(query.isLoading);
 *   if (showSkeleton) return <Skeleton />;
 */
export function useMinLoading(active: boolean, minMs = 450): boolean {
  const [visible, setVisible] = useState(active);
  const shownAtRef = useRef<number | null>(active ? Date.now() : null);

  useEffect(() => {
    if (active) {
      if (shownAtRef.current === null) shownAtRef.current = Date.now();
      setVisible(true);
      return;
    }

    // active just went false — keep showing until the minimum has elapsed.
    const shownAt = shownAtRef.current;
    if (shownAt === null) {
      setVisible(false);
      return;
    }
    const remaining = minMs - (Date.now() - shownAt);
    if (remaining <= 0) {
      shownAtRef.current = null;
      setVisible(false);
      return;
    }
    const timer = setTimeout(() => {
      shownAtRef.current = null;
      setVisible(false);
    }, remaining);
    return () => clearTimeout(timer);
  }, [active, minMs]);

  return visible;
}
