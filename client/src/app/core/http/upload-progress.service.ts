import { computed, Injectable, signal } from '@angular/core';

export interface UploadProgress {
  id: number;
  label: string;
  loaded: number;
  total: number | null;
  percentage: number | null;
}

@Injectable({ providedIn: 'root' })
export class UploadProgressService {
  private readonly _uploads = signal<UploadProgress[]>([]);
  private nextId = 0;

  readonly uploads = this._uploads.asReadonly();
  readonly overallPercentage = computed(() => {
    const uploads = this._uploads();
    if (uploads.length === 0 || uploads.some((upload) => upload.total === null)) return null;

    const total = uploads.reduce((sum, upload) => sum + upload.total!, 0);
    if (total === 0) return null;

    const loaded = uploads.reduce((sum, upload) => sum + upload.loaded, 0);
    return Math.min(100, Math.round((loaded / total) * 100));
  });

  start(label: string): number {
    const id = ++this.nextId;
    this._uploads.update((uploads) => [
      ...uploads,
      { id, label, loaded: 0, total: null, percentage: null },
    ]);
    return id;
  }

  update(id: number, loaded: number, total?: number): void {
    this._uploads.update((uploads) =>
      uploads.map((upload) => {
        if (upload.id !== id) return upload;

        const currentTotal = total ?? upload.total;
        const currentLoaded = currentTotal === null ? loaded : Math.min(loaded, currentTotal);
        return {
          ...upload,
          loaded: currentLoaded,
          total: currentTotal,
          percentage:
            currentTotal === null || currentTotal === 0
              ? null
              : Math.min(100, Math.round((currentLoaded / currentTotal) * 100)),
        };
      }),
    );
  }

  finish(id: number): void {
    this._uploads.update((uploads) => uploads.filter((upload) => upload.id !== id));
  }
}
