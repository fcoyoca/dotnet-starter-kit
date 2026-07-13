import { useEffect, useMemo, useRef, useState } from "react";
import { Check, ChevronDown, Search, Users, X } from "lucide-react";
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuRow,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu";
import { Avatar } from "@/components/ui/avatar";
import { type ProviderDto } from "@/api/administration";
import { cn } from "@/lib/cn";

/**
 * A provider as the pickers need it. `photoUrl` is optional and not yet served
 * by the providers API — Avatar falls back to branded initials when it is
 * absent or fails to load, so the moment the backend exposes a photo the
 * pickers render faces with no change here.
 */
export type ProviderLike = ProviderDto & { photoUrl?: string | null };

/** Roster label: "Last, First" — sorts and scans well in a long list. */
export function providerName(p: ProviderLike): string {
  return `${p.lastName}, ${p.firstName}`;
}

/** Spoken label: "Dr. Jane Cruz, MD" — for avatars, headers, prose. */
export function providerFullName(p: ProviderLike): string {
  return [p.prefix, p.firstName, p.lastName, p.suffix].filter(Boolean).join(" ").trim();
}

/** Initials come from "First Last" so Avatar yields "JC", not "CJ". */
function avatarName(p: ProviderLike): string {
  return `${p.firstName} ${p.lastName}`.trim();
}

function matches(p: ProviderLike, query: string): boolean {
  const q = query.trim().toLowerCase();
  if (!q) return true;
  return [p.firstName, p.lastName, p.specialty, p.prefix, p.suffix]
    .filter(Boolean)
    .some((f) => f!.toLowerCase().includes(q));
}

/**
 * ProviderAvatar — a provider's face (or branded initials) at a consistent size.
 * Exported so calendar resource headers and any future roster surface show the
 * same mark as the pickers.
 */
export function ProviderAvatar({
  provider,
  size = "sm",
  className,
}: {
  provider: ProviderLike;
  size?: "xs" | "sm" | "md" | "lg";
  className?: string;
}) {
  return (
    <Avatar
      name={avatarName(provider)}
      src={provider.photoUrl}
      size={size}
      className={className}
    />
  );
}

/** Search box shared by both pickers; stops Radix typeahead from eating keystrokes. */
function SearchRow({
  value,
  onChange,
  inputRef,
  placeholder,
}: {
  value: string;
  onChange: (v: string) => void;
  inputRef: React.RefObject<HTMLInputElement | null>;
  placeholder: string;
}) {
  return (
    <DropdownMenuRow className="gap-2 border-b border-[var(--color-border)] px-3 py-2.5">
      <Search className="h-3.5 w-3.5 text-[var(--color-muted-foreground)]" />
      <input
        ref={inputRef}
        value={value}
        onChange={(e) => onChange(e.target.value)}
        placeholder={placeholder}
        onKeyDown={(e) => {
          if (e.key !== "Escape") e.stopPropagation();
        }}
        className={cn(
          "h-6 w-full bg-transparent text-sm",
          "placeholder:text-[var(--color-muted-foreground)]",
          "outline-none focus:outline-none focus-visible:outline-none focus-visible:shadow-none",
        )}
      />
      {value && (
        <button
          type="button"
          onClick={(e) => {
            e.stopPropagation();
            onChange("");
            inputRef.current?.focus();
          }}
          aria-label="Clear search"
          className="grid h-5 w-5 cursor-pointer place-items-center rounded text-[var(--color-muted-foreground)] hover:bg-[var(--color-muted)] hover:text-[var(--color-foreground)]"
        >
          <X className="h-3 w-3" />
        </button>
      )}
    </DropdownMenuRow>
  );
}

/** One roster row: check + avatar + name over specialty. */
function ProviderRow({
  provider,
  selected,
  onPick,
  role,
}: {
  provider: ProviderLike;
  selected: boolean;
  onPick: () => void;
  role: "menuitemradio" | "menuitemcheckbox";
}) {
  return (
    <li role="none">
      <button
        type="button"
        role={role}
        aria-checked={selected}
        onClick={onPick}
        className={cn(
          "flex w-full cursor-pointer items-center gap-2.5 px-3 py-1.5 text-left",
          "transition-colors duration-[var(--duration-fast)]",
          selected
            ? "bg-[var(--color-primary-soft)]"
            : "hover:bg-[var(--color-accent)]",
        )}
      >
        <Check
          className={cn(
            "h-3.5 w-3.5 shrink-0 text-[var(--color-primary)] transition-opacity",
            selected ? "opacity-100" : "opacity-0",
          )}
        />
        <ProviderAvatar provider={provider} size="sm" />
        <span className="min-w-0 flex-1">
          <span
            className={cn(
              "block truncate text-sm",
              selected
                ? "font-medium text-[var(--color-primary)]"
                : "text-[var(--color-foreground)]",
            )}
          >
            {providerName(provider)}
          </span>
          {provider.specialty && (
            <span className="block truncate text-[11.5px] text-[var(--color-muted-foreground)]">
              {provider.specialty}
            </span>
          )}
        </span>
      </button>
    </li>
  );
}

function EmptyRow() {
  return (
    <li className="px-3 py-3 text-center text-[12px] text-[var(--color-muted-foreground)]">
      No providers match.
    </li>
  );
}

const LIST_CLASS = "max-h-[300px] overflow-y-auto py-1";
const CONTENT_CLASS = "max-h-[min(420px,65vh)] overflow-hidden p-0";

/**
 * ProviderSelect — single-select provider chooser for forms (the appointment
 * dialog's "Provider" field). Searchable, avatar rows, specialty subtitles.
 * Replaces a native <select>, which stops being usable past a dozen clinicians.
 */
export function ProviderSelect({
  providers,
  value,
  onChange,
  disabled,
  id,
  className,
}: {
  providers: ProviderLike[];
  value: string;
  onChange: (providerId: string) => void;
  disabled?: boolean;
  id?: string;
  className?: string;
}) {
  const [open, setOpen] = useState(false);
  const [query, setQuery] = useState("");
  const inputRef = useRef<HTMLInputElement>(null);

  useEffect(() => {
    if (!open) return;
    setQuery("");
    const t = setTimeout(() => inputRef.current?.focus(), 30);
    return () => clearTimeout(t);
  }, [open]);

  const filtered = useMemo(() => providers.filter((p) => matches(p, query)), [providers, query]);
  const selected = providers.find((p) => p.id === value) ?? null;

  return (
    <DropdownMenu open={open} onOpenChange={(o) => !disabled && setOpen(o)}>
      <DropdownMenuTrigger asChild disabled={disabled}>
        <button
          id={id}
          type="button"
          disabled={disabled}
          className={cn(
            "group/field flex h-9 w-full cursor-pointer items-center justify-between gap-2",
            "rounded-lg border border-[var(--color-input)] bg-transparent px-2 text-left text-[13px] shadow-sm",
            "transition-colors duration-[var(--duration-fast)]",
            "hover:border-[var(--color-border-strong)]",
            "focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-[var(--color-ring)] focus-visible:ring-offset-2",
            "data-[state=open]:border-[oklch(from_var(--color-primary)_l_c_h_/_0.4)]",
            "disabled:cursor-not-allowed disabled:opacity-60",
            className,
          )}
        >
          <span className="flex min-w-0 flex-1 items-center gap-2">
            {selected ? (
              <>
                <ProviderAvatar provider={selected} size="xs" />
                <span className="truncate text-[var(--color-foreground)]">
                  {providerName(selected)}
                </span>
              </>
            ) : (
              <span className="truncate text-[var(--color-muted-foreground)]">Select provider…</span>
            )}
          </span>
          <ChevronDown
            aria-hidden
            className={cn(
              "h-4 w-4 shrink-0 text-[var(--color-muted-foreground)]",
              "transition-transform duration-[var(--duration-fast)]",
              "group-data-[state=open]/field:rotate-180",
            )}
          />
        </button>
      </DropdownMenuTrigger>

      <DropdownMenuContent
        align="start"
        sideOffset={6}
        className={cn("min-w-[var(--radix-dropdown-menu-trigger-width)]", CONTENT_CLASS)}
      >
        <SearchRow
          value={query}
          onChange={setQuery}
          inputRef={inputRef}
          placeholder="Search providers…"
        />
        <ul role="none" className={LIST_CLASS}>
          {filtered.length === 0 ? (
            <EmptyRow />
          ) : (
            filtered.map((p) => (
              <ProviderRow
                key={p.id}
                provider={p}
                selected={p.id === value}
                role="menuitemradio"
                onPick={() => {
                  onChange(p.id);
                  setOpen(false);
                }}
              />
            ))
          )}
        </ul>
      </DropdownMenuContent>
    </DropdownMenu>
  );
}

/** Overlapping avatar stack — the trigger's at-a-glance "who's on the calendar". */
function AvatarStack({ providers, max = 3 }: { providers: ProviderLike[]; max?: number }) {
  const shown = providers.slice(0, max);
  const overflow = providers.length - shown.length;
  return (
    <span className="flex items-center -space-x-1.5">
      {shown.map((p) => (
        <ProviderAvatar
          key={p.id}
          provider={p}
          size="xs"
          className="ring-2 ring-[var(--color-card)]"
        />
      ))}
      {overflow > 0 && (
        <span
          aria-hidden
          className={cn(
            "grid h-5 w-5 place-items-center rounded-full ring-2 ring-[var(--color-card)]",
            "bg-[var(--color-muted)] text-[9px] font-semibold text-[var(--color-muted-foreground)]",
          )}
        >
          +{overflow}
        </span>
      )}
    </span>
  );
}

/**
 * ProviderFilter — multi-select provider filter for the calendar toolbar.
 * Searchable, with Select all / Clear, so a clinic with fifty clinicians stays
 * one compact control instead of a wrapping wall of pills.
 *
 * Selection semantics match the calendar's: an EMPTY set means "all providers"
 * (nothing filtered out), which is why the trigger reads "All providers" and
 * Clear empties the set rather than hiding every column.
 */
export function ProviderFilter({
  providers,
  selected,
  onChange,
  className,
}: {
  providers: ProviderLike[];
  selected: Set<string>;
  onChange: (next: Set<string>) => void;
  className?: string;
}) {
  const [open, setOpen] = useState(false);
  const [query, setQuery] = useState("");
  const inputRef = useRef<HTMLInputElement>(null);

  useEffect(() => {
    if (!open) return;
    setQuery("");
    const t = setTimeout(() => inputRef.current?.focus(), 30);
    return () => clearTimeout(t);
  }, [open]);

  const filtered = useMemo(() => providers.filter((p) => matches(p, query)), [providers, query]);

  // Empty set = all. Resolve it once so the trigger and rows agree.
  const showingAll = selected.size === 0;
  const shown = useMemo(
    () => (showingAll ? providers : providers.filter((p) => selected.has(p.id))),
    [showingAll, providers, selected],
  );

  const toggle = (id: string) => {
    const next = new Set(selected);
    if (next.has(id)) next.delete(id);
    else next.add(id);
    onChange(next);
  };

  const label = showingAll
    ? `All providers · ${providers.length}`
    : shown.length === 1
      ? providerName(shown[0])
      : `${shown.length} of ${providers.length} providers`;

  return (
    <DropdownMenu open={open} onOpenChange={setOpen}>
      <DropdownMenuTrigger asChild>
        <button
          type="button"
          aria-label="Filter providers"
          className={cn(
            "inline-flex h-9 cursor-pointer items-center gap-2 rounded-lg border px-2.5",
            "text-[13px] transition-colors duration-[var(--duration-fast)]",
            "focus-visible:outline-none focus-visible:ring-[3px] focus-visible:ring-[oklch(from_var(--color-ring)_l_c_h_/_0.18)]",
            showingAll
              ? "border-[var(--color-input)] bg-[var(--color-card)] text-[var(--color-muted-foreground)] hover:text-[var(--color-foreground)]"
              : "border-[oklch(from_var(--color-primary)_l_c_h_/_0.25)] bg-[oklch(from_var(--color-primary)_l_c_h_/_0.10)] text-[var(--color-primary)]",
            "data-[state=open]:bg-[var(--color-muted)]",
            className,
          )}
        >
          {/* Faces identify a chosen subset; when everyone is shown they would just
              repeat the count, so the roster icon carries it instead. */}
          {showingAll || shown.length === 0 ? (
            <Users className="h-3.5 w-3.5" />
          ) : (
            <AvatarStack providers={shown} />
          )}
          <span className="truncate">{label}</span>
          <ChevronDown aria-hidden className="h-3.5 w-3.5 shrink-0 opacity-70" />
        </button>
      </DropdownMenuTrigger>

      <DropdownMenuContent align="end" sideOffset={6} className={cn("w-72", CONTENT_CLASS)}>
        <SearchRow
          value={query}
          onChange={setQuery}
          inputRef={inputRef}
          placeholder="Search providers…"
        />

        <ul role="none" className={LIST_CLASS}>
          {filtered.length === 0 ? (
            <EmptyRow />
          ) : (
            filtered.map((p) => (
              <ProviderRow
                key={p.id}
                provider={p}
                // While showing all, every row reads as checked — that is what the
                // calendar renders — but the set stays empty until the user picks.
                selected={showingAll || selected.has(p.id)}
                role="menuitemcheckbox"
                onPick={() => toggle(p.id)}
              />
            ))
          )}
        </ul>

        <div className="flex items-center justify-between gap-2 border-t border-[var(--color-border)] px-3 py-2">
          <span className="text-[11.5px] text-[var(--color-muted-foreground)]">
            {showingAll ? "Showing everyone" : `${selected.size} selected`}
          </span>
          <button
            type="button"
            onClick={() => onChange(new Set())}
            disabled={showingAll}
            className={cn(
              "cursor-pointer rounded px-1.5 py-0.5 text-[12px] font-medium text-[var(--color-primary)]",
              "hover:bg-[var(--color-accent)]",
              "disabled:cursor-default disabled:opacity-40 disabled:hover:bg-transparent",
            )}
          >
            Show all
          </button>
        </div>
      </DropdownMenuContent>
    </DropdownMenu>
  );
}
