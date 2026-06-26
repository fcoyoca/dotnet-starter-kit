import { useRef, useState, type PointerEvent as ReactPointerEvent } from "react";
import Cropper, { type ReactCropperElement } from "react-cropper";
import "cropperjs/dist/cropper.css";
import {
  Dialog,
  DialogBody,
  DialogClose,
  DialogContent,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import { Button } from "@/components/ui/button";

type Mode = "upload" | "draw";

export type SignatureEditorProps = {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  /** Current signature image URL, shown as the starting preview. */
  value?: string | null;
  /** Receives the new signature as a PNG data URL ("data:image/png;base64,..."). */
  onSave: (pngBase64: string) => void;
  targetWidth?: number;
  targetHeight?: number;
};

export function SignatureEditor({
  open,
  onOpenChange,
  value,
  onSave,
  targetWidth = 180,
  targetHeight = 30,
}: SignatureEditorProps) {
  const [mode, setMode] = useState<Mode>("upload");
  const [uploadSrc, setUploadSrc] = useState<string | null>(null);
  const cropperRef = useRef<ReactCropperElement>(null);
  const canvasRef = useRef<HTMLCanvasElement>(null);
  const drawing = useRef(false);
  const hasStrokes = useRef(false);

  const aspect = targetWidth / targetHeight;

  const onFile = (file: File | undefined) => {
    if (!file) return;
    const reader = new FileReader();
    reader.onload = () => setUploadSrc(typeof reader.result === "string" ? reader.result : null);
    reader.readAsDataURL(file);
  };

  // ── Draw mode helpers ──
  const ctx = () => canvasRef.current?.getContext("2d") ?? null;
  const pos = (e: ReactPointerEvent<HTMLCanvasElement>) => {
    const rect = canvasRef.current!.getBoundingClientRect();
    return { x: e.clientX - rect.left, y: e.clientY - rect.top };
  };
  const onDown = (e: ReactPointerEvent<HTMLCanvasElement>) => {
    const c = ctx();
    if (!c) return;
    drawing.current = true;
    hasStrokes.current = true;
    const { x, y } = pos(e);
    c.beginPath();
    c.moveTo(x, y);
    canvasRef.current!.setPointerCapture(e.pointerId);
  };
  const onMove = (e: ReactPointerEvent<HTMLCanvasElement>) => {
    if (!drawing.current) return;
    const c = ctx();
    if (!c) return;
    const { x, y } = pos(e);
    c.lineWidth = 2;
    c.lineCap = "round";
    c.strokeStyle = "#111827";
    c.lineTo(x, y);
    c.stroke();
  };
  const onUp = () => {
    drawing.current = false;
  };
  const clearCanvas = () => {
    const c = ctx();
    if (c && canvasRef.current) c.clearRect(0, 0, canvasRef.current.width, canvasRef.current.height);
    hasStrokes.current = false;
  };

  const handleSave = () => {
    if (mode === "upload") {
      const cropper = cropperRef.current?.cropper;
      if (!cropper) return;
      const out = cropper.getCroppedCanvas({ width: targetWidth, height: targetHeight });
      onSave(out.toDataURL("image/png"));
    } else {
      if (!hasStrokes.current || !canvasRef.current) return;
      onSave(canvasRef.current.toDataURL("image/png"));
    }
    onOpenChange(false);
    reset();
  };

  const reset = () => {
    setUploadSrc(null);
    clearCanvas();
    setMode("upload");
  };

  return (
    <Dialog
      open={open}
      onOpenChange={(o) => {
        if (!o) reset();
        onOpenChange(o);
      }}
    >
      <DialogContent className="!max-w-xl">
        <DialogHeader>
          <DialogTitle>Edit signature</DialogTitle>
        </DialogHeader>
        <DialogBody className="space-y-4">
          <div className="inline-flex rounded-lg border border-[var(--color-border)] p-0.5">
            <button
              type="button"
              onClick={() => setMode("upload")}
              className={`rounded-md px-3 py-1 text-[13px] font-medium ${mode === "upload" ? "bg-[var(--color-muted)] text-[var(--color-foreground)]" : "text-[var(--color-muted-foreground)]"}`}
            >
              Upload
            </button>
            <button
              type="button"
              onClick={() => setMode("draw")}
              className={`rounded-md px-3 py-1 text-[13px] font-medium ${mode === "draw" ? "bg-[var(--color-muted)] text-[var(--color-foreground)]" : "text-[var(--color-muted-foreground)]"}`}
            >
              Draw
            </button>
          </div>

          {mode === "upload" ? (
            <div className="space-y-3">
              <input
                type="file"
                accept="image/*"
                onChange={(e) => onFile(e.target.files?.[0])}
                className="block text-[13px]"
              />
              {uploadSrc ? (
                <Cropper
                  ref={cropperRef}
                  src={uploadSrc}
                  style={{ height: 240, width: "100%" }}
                  aspectRatio={aspect}
                  viewMode={1}
                  background={false}
                  autoCropArea={1}
                  responsive
                  guides
                />
              ) : value ? (
                <img src={value} alt="Current signature" className="h-16 border border-[var(--color-border)] object-contain" />
              ) : (
                <p className="text-[13px] text-[var(--color-muted-foreground)]">Select an image to crop to the signature box.</p>
              )}
            </div>
          ) : (
            <div className="space-y-2">
              <canvas
                ref={canvasRef}
                width={540}
                height={120}
                onPointerDown={onDown}
                onPointerMove={onMove}
                onPointerUp={onUp}
                className="w-full touch-none rounded-md border border-[var(--color-border)] bg-white"
              />
              <Button type="button" variant="outline" size="sm" onClick={clearCanvas}>
                Clear
              </Button>
            </div>
          )}
        </DialogBody>
        <DialogFooter>
          <DialogClose asChild>
            <Button type="button" variant="outline">
              Cancel
            </Button>
          </DialogClose>
          <Button type="button" onClick={handleSave}>
            Save signature
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
