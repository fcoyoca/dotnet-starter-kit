import { useCallback, useEffect, useRef, useState } from "react";
import { activityStore, evaluateInactivity, type InactivityPhase } from "@/auth/inactivity";

// Genuine user-intent signals. Throttled before they touch storage so a
// mousemove storm can't hammer sessionStorage.
const ACTIVITY_EVENTS = [
  "pointerdown",
  "keydown",
  "wheel",
  "touchstart",
  "scroll",
  "mousemove",
] as const;

const WRITE_THROTTLE_MS = 1_000;
const TICK_MS = 1_000;

type Options = {
  /** Watch only while signed in. When false the hook is fully inert. */
  enabled: boolean;
  idleMs: number;
  warningMs: number;
  /** Fired once when the session crosses the expiry threshold. */
  onExpire: () => void;
};

/**
 * Drives the inactivity state machine. Tracks a per-tab "last activity"
 * timestamp (sessionStorage — each tab times out on its own), ticks once a
 * second, and surfaces the current phase + seconds left so a warning modal
 * can render. While the warning shows, the hook stops recording passive
 * activity so the prompt stays meaningful (only an explicit reset dismisses it).
 */
export function useInactivityTimeout({ enabled, idleMs, warningMs, onExpire }: Options) {
  const [phase, setPhase] = useState<InactivityPhase>("active");
  const [secondsLeft, setSecondsLeft] = useState(0);

  // Mutable mirrors so the event/interval wiring can stay mount-scoped.
  const phaseRef = useRef<InactivityPhase>("active");
  const lastWriteRef = useRef(0);
  const expiredRef = useRef(false);
  const idleRef = useRef(idleMs);
  const warnRef = useRef(warningMs);
  const onExpireRef = useRef(onExpire);
  idleRef.current = idleMs;
  warnRef.current = warningMs;
  onExpireRef.current = onExpire;

  const evaluateNow = useCallback(() => {
    const { phase: next, secondsLeft: left } = evaluateInactivity(
      Date.now(),
      activityStore.get(),
      idleRef.current,
      warnRef.current,
    );
    phaseRef.current = next;
    setPhase(next);
    setSecondsLeft(left);
    if (next === "expired" && !expiredRef.current) {
      expiredRef.current = true;
      onExpireRef.current();
    }
  }, []);

  const recordActivity = useCallback(() => {
    // Freeze passive activity once we're warning/expired in this tab.
    if (phaseRef.current !== "active") return;
    const now = Date.now();
    if (now - lastWriteRef.current < WRITE_THROTTLE_MS) return;
    lastWriteRef.current = now;
    activityStore.set(now);
  }, []);

  /** Explicit "I'm here" — refresh the shared stamp and drop back to active. */
  const reset = useCallback(() => {
    const now = Date.now();
    lastWriteRef.current = now;
    activityStore.set(now);
    expiredRef.current = false;
    phaseRef.current = "active";
    setPhase("active");
    setSecondsLeft(0);
  }, []);

  useEffect(() => {
    if (!enabled) {
      phaseRef.current = "active";
      expiredRef.current = false;
      setPhase("active");
      setSecondsLeft(0);
      return;
    }

    // Seed the stamp on enable so a fresh login isn't instantly idle.
    // The stamp lives in sessionStorage and survives logout within this tab,
    // so a value left over from a previous (possibly expired) session would
    // otherwise make the next login evaluate as "warning"/"expired" on the
    // first tick and sign the user straight back out. Refresh it when it's
    // missing OR already past the idle threshold.
    const seeded = activityStore.get();
    const stale = seeded <= 0 || Date.now() - seeded >= idleRef.current;
    if (stale) activityStore.set(Date.now());
    expiredRef.current = false;

    for (const evt of ACTIVITY_EVENTS) {
      window.addEventListener(evt, recordActivity, { passive: true, capture: true });
    }
    const onVisibility = () => {
      if (document.visibilityState === "visible") evaluateNow();
    };
    document.addEventListener("visibilitychange", onVisibility);

    evaluateNow();
    const intervalId = window.setInterval(evaluateNow, TICK_MS);

    return () => {
      window.clearInterval(intervalId);
      for (const evt of ACTIVITY_EVENTS) {
        window.removeEventListener(evt, recordActivity, {
          capture: true,
        } as EventListenerOptions);
      }
      document.removeEventListener("visibilitychange", onVisibility);
    };
  }, [enabled, recordActivity, evaluateNow]);

  return { phase, secondsLeft, dismiss: reset };
}
