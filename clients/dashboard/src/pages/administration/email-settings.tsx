import { useEffect, useRef, useState, type FormEvent } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { Mail } from "lucide-react";
import { toast } from "sonner";
import {
  getEmailSettings,
  updateEmailSettings,
  type EmailSettingsDto,
} from "@/api/administration";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Switch } from "@/components/ui/switch";
import { EntityPageHeader, Field } from "@/components/list";
import { RichTextEditor } from "@/components/rich-text-editor";
import { describe } from "@/lib/list-helpers";

const QUERY_KEY = ["administration", "email-settings"] as const;

/** Tiptap emits "<p></p>" for empty content — treat that (and blank) as null. */
function htmlOrNull(html: string): string | null {
  const stripped = html.replace(/<p><\/p>/g, "").replace(/<br\s*\/?>/g, "").trim();
  return stripped ? html : null;
}

type FormState = {
  useCustomSmtp: boolean;
  host: string;
  port: string;
  useSsl: boolean;
  username: string;
  password: string;
  fromAddress: string;
  fromName: string;
  replyTo: string;
  footerHtml: string;
  passwordResetSubject: string;
  passwordResetBody: string;
  passwordResetFooter: string;
};

function toForm(dto: EmailSettingsDto): FormState {
  return {
    useCustomSmtp: dto.useCustomSmtp,
    host: dto.host ?? "",
    port: dto.port != null ? String(dto.port) : "",
    useSsl: dto.useSsl,
    username: dto.username ?? "",
    password: "",
    fromAddress: dto.fromAddress ?? "",
    fromName: dto.fromName ?? "",
    replyTo: dto.replyTo ?? "",
    footerHtml: dto.footerHtml ?? "",
    passwordResetSubject: dto.passwordResetSubject ?? "",
    passwordResetBody: dto.passwordResetBody ?? "",
    passwordResetFooter: dto.passwordResetFooter ?? "",
  };
}

export function EmailSettingsPage() {
  const queryClient = useQueryClient();
  const query = useQuery({ queryKey: QUERY_KEY, queryFn: getEmailSettings });
  const settings = query.data;

  const [form, setForm] = useState<FormState | null>(null);
  const seededRef = useRef(false);

  // Seed once so a background refetch can't clobber in-progress edits.
  useEffect(() => {
    if (!seededRef.current && settings) {
      setForm(toForm(settings));
      seededRef.current = true;
    }
  }, [settings]);

  const set = <K extends keyof FormState>(key: K, value: FormState[K]) =>
    setForm((f) => (f ? { ...f, [key]: value } : f));

  const saveMutation = useMutation({
    mutationFn: () => {
      if (!form) throw new Error("not ready");
      return updateEmailSettings({
        useCustomSmtp: form.useCustomSmtp,
        host: form.host.trim() || null,
        port: form.port.trim() ? Number(form.port) : null,
        useSsl: form.useSsl,
        username: form.username.trim() || null,
        password: form.password.trim() ? form.password : null,
        fromAddress: form.fromAddress.trim() || null,
        fromName: form.fromName.trim() || null,
        replyTo: form.replyTo.trim() || null,
        footerHtml: htmlOrNull(form.footerHtml),
        passwordResetSubject: form.passwordResetSubject.trim() || null,
        passwordResetBody: htmlOrNull(form.passwordResetBody),
        passwordResetFooter: htmlOrNull(form.passwordResetFooter),
      });
    },
    onSuccess: () => {
      toast.success("Email settings saved");
      seededRef.current = false;
      queryClient.invalidateQueries({ queryKey: QUERY_KEY });
    },
    onError: (err) => toast.error("Save failed", { description: describe(err) }),
  });

  const onSubmit = (e: FormEvent<HTMLFormElement>) => {
    e.preventDefault();
    saveMutation.mutate();
  };

  const saving = saveMutation.isPending;

  return (
    <div className="space-y-4 sm:space-y-6">
      <EntityPageHeader
        icon={Mail}
        title="Email Settings"
        description="Configure how outbound email is sent for your organization."
      />

      {query.isLoading || !form ? (
        <div className="rounded-xl border border-[var(--color-border)] p-6 text-[13px] text-[var(--color-muted-foreground)]">
          Loading…
        </div>
      ) : (
        <form onSubmit={onSubmit} className="space-y-6">
          <section className="rounded-xl border border-[var(--color-border)] p-4 sm:p-5">
            <div className="flex items-center justify-between gap-4">
              <div>
                <h2 className="text-[14px] font-semibold text-[var(--color-foreground)]">SMTP server</h2>
                <p className="mt-0.5 text-[12px] text-[var(--color-muted-foreground)]">
                  {form.useCustomSmtp
                    ? "Send through your own mail server."
                    : "Using the system default mail server. Turn on custom settings to use your own."}
                </p>
              </div>
              <Switch
                checked={form.useCustomSmtp}
                onCheckedChange={(v) => set("useCustomSmtp", v)}
                aria-label="Use a custom SMTP server"
              />
            </div>

            {form.useCustomSmtp && (
              <div className="mt-5 grid gap-5 sm:grid-cols-2">
                <Field id="smtp-host" label="Host" required>
                  <Input
                    id="smtp-host"
                    value={form.host}
                    onChange={(e) => set("host", e.target.value)}
                    placeholder="smtp.example.com"
                    maxLength={256}
                  />
                </Field>
                <Field id="smtp-port" label="Port" required>
                  <Input
                    id="smtp-port"
                    type="number"
                    value={form.port}
                    onChange={(e) => set("port", e.target.value)}
                    placeholder="587"
                    min={1}
                    max={65535}
                  />
                </Field>
                <Field id="smtp-user" label="Username">
                  <Input
                    id="smtp-user"
                    value={form.username}
                    onChange={(e) => set("username", e.target.value)}
                    autoComplete="off"
                    maxLength={256}
                  />
                </Field>
                <Field id="smtp-pass" label="Password">
                  <Input
                    id="smtp-pass"
                    type="password"
                    value={form.password}
                    onChange={(e) => set("password", e.target.value)}
                    autoComplete="new-password"
                    placeholder={settings?.hasPassword ? "•••••••• (unchanged)" : ""}
                    maxLength={512}
                  />
                  <p className="mt-1 text-[11px] text-[var(--color-muted-foreground)]">
                    {settings?.hasPassword
                      ? "Leave blank to keep the current password."
                      : "Stored securely and never shown again."}
                  </p>
                </Field>
                <div className="flex items-center justify-between rounded-lg border border-[var(--color-border)] px-3 py-2.5 sm:col-span-2">
                  <div>
                    <p className="text-[13px] font-medium text-[var(--color-foreground)]">Use SSL/TLS</p>
                    <p className="text-[12px] text-[var(--color-muted-foreground)]">
                      Encrypt the connection to the mail server.
                    </p>
                  </div>
                  <Switch
                    checked={form.useSsl}
                    onCheckedChange={(v) => set("useSsl", v)}
                    aria-label="Use SSL"
                  />
                </div>
              </div>
            )}
          </section>

          <section className="rounded-xl border border-[var(--color-border)] p-4 sm:p-5">
            <h2 className="text-[14px] font-semibold text-[var(--color-foreground)]">Sender identity</h2>
            <p className="mt-0.5 text-[12px] text-[var(--color-muted-foreground)]">
              The from and reply-to addresses recipients see on your emails.
            </p>
            <div className="mt-5 grid gap-5 sm:grid-cols-2">
              <Field id="from-address" label="From address">
                <Input
                  id="from-address"
                  type="email"
                  value={form.fromAddress}
                  onChange={(e) => set("fromAddress", e.target.value)}
                  placeholder="clinic@example.com"
                  maxLength={256}
                />
              </Field>
              <Field id="from-name" label="From name">
                <Input
                  id="from-name"
                  value={form.fromName}
                  onChange={(e) => set("fromName", e.target.value)}
                  placeholder="Your Clinic"
                  maxLength={256}
                />
              </Field>
              <Field id="reply-to" label="Reply-to address">
                <Input
                  id="reply-to"
                  type="email"
                  value={form.replyTo}
                  onChange={(e) => set("replyTo", e.target.value)}
                  placeholder="reply@example.com"
                  maxLength={256}
                />
              </Field>
            </div>
          </section>

          <section className="rounded-xl border border-[var(--color-border)] p-4 sm:p-5">
            <h2 className="text-[14px] font-semibold text-[var(--color-foreground)]">Email footer</h2>
            <p className="mt-0.5 text-[12px] text-[var(--color-muted-foreground)]">
              Appended to the bottom of all outbound emails.
            </p>
            <div className="mt-4">
              <RichTextEditor
                value={form.footerHtml}
                onChange={(html) => set("footerHtml", html)}
                ariaLabel="Email footer"
                placeholder="Your Clinic · 123 Main St · (555) 123-4567"
              />
            </div>
          </section>

          <section className="rounded-xl border border-[var(--color-border)] p-4 sm:p-5">
            <h2 className="text-[14px] font-semibold text-[var(--color-foreground)]">Password reset email</h2>
            <p className="mt-0.5 text-[12px] text-[var(--color-muted-foreground)]">
              The message sent when a user resets their password.
            </p>
            <div className="mt-5 space-y-5">
              <Field id="pwreset-subject" label="Subject" hint="Appears on the subject line of the reset email.">
                <Input
                  id="pwreset-subject"
                  value={form.passwordResetSubject}
                  onChange={(e) => set("passwordResetSubject", e.target.value)}
                  placeholder="Reset your password"
                  maxLength={256}
                />
              </Field>
              <div>
                <p className="mb-1.5 text-[13px] font-medium text-[var(--color-foreground)]">Body</p>
                <RichTextEditor
                  value={form.passwordResetBody}
                  onChange={(html) => set("passwordResetBody", html)}
                  ariaLabel="Password reset body"
                  placeholder="Click the link below to reset your password…"
                />
              </div>
              <div>
                <p className="mb-1.5 text-[13px] font-medium text-[var(--color-foreground)]">Footer</p>
                <p className="mb-1.5 text-[12px] text-[var(--color-muted-foreground)]">
                  Shown after the body — e.g. to confirm a new password was set.
                </p>
                <RichTextEditor
                  value={form.passwordResetFooter}
                  onChange={(html) => set("passwordResetFooter", html)}
                  ariaLabel="Password reset footer"
                  placeholder="If you didn't request this, you can ignore this email."
                />
              </div>
            </div>
          </section>

          <div className="flex items-center justify-end gap-2">
            <Button type="submit" disabled={saving}>
              {saving ? "Saving…" : "Save changes"}
            </Button>
          </div>
        </form>
      )}

      {query.isError && (
        <div
          role="alert"
          className="rounded-lg border border-[oklch(from_var(--color-destructive)_l_c_h_/_0.30)] bg-[oklch(from_var(--color-destructive)_l_c_h_/_0.06)] px-3 py-2 text-sm text-[var(--color-destructive)]"
        >
          {describe(query.error)}
        </div>
      )}
    </div>
  );
}
