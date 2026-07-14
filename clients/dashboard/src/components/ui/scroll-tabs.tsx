import { useCallback, useEffect, useRef, useState, type ReactNode } from "react";
import { ChevronLeft, ChevronRight } from "lucide-react";
import { cn } from "@/lib/cn";

type ScrollTabsProps = {
  children: ReactNode;
  /** Accessible name for the scrolling tab list. */
  ariaLabel: string;
  className?: string;
};

/** How much of the visible strip one chevron click travels. */
const PAGE_FRACTION = 0.8;

/**
 * A single-line, horizontally scrollable row of tabs with left/right scroll
 * buttons. Tabs never wrap onto a second line, so a chart with many open
 * reports keeps the strip one row tall. The strip also scrolls directly — by
 * wheel, trackpad, or dragging its scrollbar — and the buttons only appear
 * once the content actually overflows.
 */
export function ScrollTabs({ children, ariaLabel, className }: ScrollTabsProps) {
  const scrollerRef = useRef<HTMLDivElement>(null);
  // Where an in-flight smooth scroll is heading. Without it, a second click
  // lands relative to the animation's CURRENT position rather than its
  // destination, so a fast click-click-click travels barely one page.
  const targetRef = useRef<number | null>(null);
  const [canScrollLeft, setCanScrollLeft] = useState(false);
  const [canScrollRight, setCanScrollRight] = useState(false);

  const syncScrollState = useCallback(() => {
    const el = scrollerRef.current;
    if (!el) return;
    // Sub-pixel widths make an exactly-scrolled-to-the-end strip report a
    // remainder of ~0.5px, which would leave the right button enabled forever.
    const remaining = el.scrollWidth - el.clientWidth - el.scrollLeft;
    setCanScrollLeft(el.scrollLeft > 1);
    setCanScrollRight(remaining > 1);
    if (targetRef.current !== null && Math.abs(el.scrollLeft - targetRef.current) <= 1) {
      targetRef.current = null;
    }
  }, []);

  useEffect(() => {
    const el = scrollerRef.current;
    if (!el) return;
    syncScrollState();
    // Observing the row itself catches viewport resizes; observing its children
    // catches tabs being opened or closed.
    const observer = new ResizeObserver(syncScrollState);
    observer.observe(el);
    for (const child of Array.from(el.children)) observer.observe(child);
    return () => observer.disconnect();
  }, [syncScrollState, children]);

  const scrollByPage = (direction: -1 | 1) => {
    const el = scrollerRef.current;
    if (!el) return;
    const max = el.scrollWidth - el.clientWidth;
    const from = targetRef.current ?? el.scrollLeft;
    const to = Math.min(max, Math.max(0, from + direction * el.clientWidth * PAGE_FRACTION));
    targetRef.current = to;
    el.scrollTo({ left: to, behavior: "smooth" });
  };

  /** Any hand-driven scroll abandons whatever a chevron was animating toward. */
  const dropTarget = () => {
    targetRef.current = null;
  };

  const showButtons = canScrollLeft || canScrollRight;

  return (
    <div className={cn("flex min-w-0 items-center gap-1", className)}>
      {showButtons && (
        <ScrollButton direction="left" disabled={!canScrollLeft} onClick={() => scrollByPage(-1)} />
      )}
      <div
        ref={scrollerRef}
        role="tablist"
        aria-label={ariaLabel}
        onScroll={syncScrollState}
        onPointerDown={dropTarget}
        onWheel={(e) => {
          // A plain wheel over a horizontal strip does nothing by default; map
          // it onto the axis the strip actually has. Trackpads send deltaX of
          // their own, which the browser already handles — leave those alone.
          if (e.deltaX !== 0 || e.deltaY === 0) return;
          const el = scrollerRef.current;
          if (!el) return;
          dropTarget();
          el.scrollBy({ left: e.deltaY });
        }}
        className="flex min-w-0 flex-1 items-center gap-1.5 overflow-x-auto scroll-smooth [scrollbar-width:thin]"
      >
        {children}
      </div>
      {showButtons && (
        <ScrollButton direction="right" disabled={!canScrollRight} onClick={() => scrollByPage(1)} />
      )}
    </div>
  );
}

function ScrollButton({
  direction,
  disabled,
  onClick,
}: {
  direction: "left" | "right";
  disabled: boolean;
  onClick: () => void;
}) {
  const Icon = direction === "left" ? ChevronLeft : ChevronRight;
  return (
    <button
      type="button"
      data-testid={`scroll-tabs-${direction}`}
      aria-label={direction === "left" ? "Scroll tabs left" : "Scroll tabs right"}
      disabled={disabled}
      onClick={onClick}
      className={cn(
        "grid size-6 shrink-0 place-items-center rounded-md border border-[var(--color-border)]",
        "text-[var(--color-muted-foreground)] transition-colors duration-[var(--duration-fast)]",
        "hover:bg-[var(--color-accent)] hover:text-[var(--color-foreground)]",
        "focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-[var(--color-ring)]",
        "disabled:cursor-default disabled:opacity-40 disabled:hover:bg-transparent",
      )}
    >
      <Icon className="size-3.5" />
    </button>
  );
}
