import { useEffect } from "react";
import { EditorContent, useEditor, type Editor } from "@tiptap/react";
import StarterKit from "@tiptap/starter-kit";
import Underline from "@tiptap/extension-underline";
import Link from "@tiptap/extension-link";
import TextAlign from "@tiptap/extension-text-align";
import {
  Bold,
  Italic,
  Link2,
  List,
  ListOrdered,
  Underline as UnderlineIcon,
  AlignLeft,
  AlignCenter,
  AlignRight,
} from "lucide-react";
import { cn } from "@/lib/cn";

type RichTextEditorProps = {
  value: string;
  onChange: (html: string) => void;
  placeholder?: string;
  ariaLabel?: string;
  minHeight?: number;
};

/**
 * Lightweight WYSIWYG (Tiptap) producing HTML — used for email templates. Mirrors the legacy
 * Syncfusion editor's toolbar (bold / italic / underline / lists / alignment / link).
 */
export function RichTextEditor({
  value,
  onChange,
  placeholder,
  ariaLabel,
  minHeight = 160,
}: RichTextEditorProps) {
  const editor = useEditor({
    extensions: [
      // StarterKit bundles Underline/Link in v3 — disable them so the explicit ones register once.
      StarterKit.configure({ underline: false, link: false }),
      Underline,
      Link.configure({ openOnClick: false, autolink: true }),
      TextAlign.configure({ types: ["heading", "paragraph"] }),
    ],
    content: value || "",
    editorProps: {
      attributes: {
        class: "tiptap-content focus:outline-none",
        ...(ariaLabel ? { "aria-label": ariaLabel } : {}),
        style: `min-height:${minHeight}px`,
      },
    },
    onUpdate: ({ editor: e }) => onChange(e.getHTML()),
  });

  // Keep the editor in sync when the value is (re)seeded from outside (e.g. form load).
  useEffect(() => {
    if (!editor) return;
    const incoming = value || "";
    if (incoming !== editor.getHTML()) {
      editor.commands.setContent(incoming, { emitUpdate: false });
    }
  }, [value, editor]);

  if (!editor) {
    return (
      <div
        className="rounded-lg border border-[var(--color-input)] bg-transparent"
        style={{ minHeight: minHeight + 40 }}
      />
    );
  }

  return (
    <div className="overflow-hidden rounded-lg border border-[var(--color-input)] bg-transparent shadow-xs focus-within:border-[var(--color-ring)] focus-within:ring-[3px] focus-within:ring-[oklch(from_var(--color-ring)_l_c_h_/_0.5)]">
      <div className="flex flex-wrap items-center gap-0.5 border-b border-[var(--color-border)] bg-[var(--color-muted)] px-1.5 py-1">
        <ToolbarButton label="Bold" active={editor.isActive("bold")} onClick={() => editor.chain().focus().toggleBold().run()}>
          <Bold className="size-3.5" />
        </ToolbarButton>
        <ToolbarButton label="Italic" active={editor.isActive("italic")} onClick={() => editor.chain().focus().toggleItalic().run()}>
          <Italic className="size-3.5" />
        </ToolbarButton>
        <ToolbarButton label="Underline" active={editor.isActive("underline")} onClick={() => editor.chain().focus().toggleUnderline().run()}>
          <UnderlineIcon className="size-3.5" />
        </ToolbarButton>
        <Divider />
        <ToolbarButton label="Bullet list" active={editor.isActive("bulletList")} onClick={() => editor.chain().focus().toggleBulletList().run()}>
          <List className="size-3.5" />
        </ToolbarButton>
        <ToolbarButton label="Numbered list" active={editor.isActive("orderedList")} onClick={() => editor.chain().focus().toggleOrderedList().run()}>
          <ListOrdered className="size-3.5" />
        </ToolbarButton>
        <Divider />
        <ToolbarButton label="Align left" active={editor.isActive({ textAlign: "left" })} onClick={() => editor.chain().focus().setTextAlign("left").run()}>
          <AlignLeft className="size-3.5" />
        </ToolbarButton>
        <ToolbarButton label="Align center" active={editor.isActive({ textAlign: "center" })} onClick={() => editor.chain().focus().setTextAlign("center").run()}>
          <AlignCenter className="size-3.5" />
        </ToolbarButton>
        <ToolbarButton label="Align right" active={editor.isActive({ textAlign: "right" })} onClick={() => editor.chain().focus().setTextAlign("right").run()}>
          <AlignRight className="size-3.5" />
        </ToolbarButton>
        <Divider />
        <ToolbarButton label="Link" active={editor.isActive("link")} onClick={() => setLink(editor)}>
          <Link2 className="size-3.5" />
        </ToolbarButton>
      </div>

      <div className="px-3 py-2 text-[13px] text-[var(--color-foreground)]">
        <EditorContent editor={editor} />
        {placeholder && editor.isEmpty ? (
          <p className="pointer-events-none -mt-[calc(1.5rem+2px)] select-none text-[var(--color-muted-foreground)] opacity-60">
            {placeholder}
          </p>
        ) : null}
      </div>
    </div>
  );
}

function setLink(editor: Editor) {
  const previous = (editor.getAttributes("link").href as string) ?? "";
  const url = window.prompt("Link URL", previous);
  if (url === null) return;
  if (url === "") {
    editor.chain().focus().extendMarkRange("link").unsetLink().run();
    return;
  }
  editor.chain().focus().extendMarkRange("link").setLink({ href: url }).run();
}

function Divider() {
  return <span className="mx-0.5 h-5 w-px bg-[var(--color-border)]" />;
}

function ToolbarButton({
  label,
  active,
  onClick,
  children,
}: {
  label: string;
  active: boolean;
  onClick: () => void;
  children: React.ReactNode;
}) {
  return (
    <button
      type="button"
      aria-label={label}
      aria-pressed={active}
      title={label}
      onClick={onClick}
      className={cn(
        "grid size-7 cursor-pointer place-items-center rounded-md transition-colors",
        active
          ? "bg-[var(--color-primary)] text-[var(--color-primary-foreground)]"
          : "text-[var(--color-muted-foreground)] hover:bg-[var(--color-background)] hover:text-[var(--color-foreground)]",
      )}
    >
      {children}
    </button>
  );
}
