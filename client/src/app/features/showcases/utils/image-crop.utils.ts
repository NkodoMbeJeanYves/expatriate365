export interface ImageCropRect {
  x: number;
  y: number;
  width: number;
  height: number;
}

export function calculateImageCrop(
  sourceWidth: number,
  sourceHeight: number,
  focusX: number,
  focusY: number,
  zoom: number,
): ImageCropRect {
  if (sourceWidth <= 0 || sourceHeight <= 0) {
    throw new RangeError('Image dimensions must be positive.');
  }

  const aspectRatio = 16 / 9;
  const baseWidth = Math.min(sourceWidth, sourceHeight * aspectRatio);
  const baseHeight = baseWidth / aspectRatio;
  const width = baseWidth / Math.max(1, Math.min(3, zoom));
  const height = baseHeight / Math.max(1, Math.min(3, zoom));
  const horizontalFocus = Math.max(0, Math.min(100, focusX)) / 100;
  const verticalFocus = Math.max(0, Math.min(100, focusY)) / 100;

  return {
    x: (sourceWidth - width) * horizontalFocus,
    y: (sourceHeight - height) * verticalFocus,
    width,
    height,
  };
}
