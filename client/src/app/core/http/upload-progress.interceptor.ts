import { HttpEventType, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { finalize, tap } from 'rxjs';
import { UploadProgressService } from './upload-progress.service';

export const uploadProgressInterceptor: HttpInterceptorFn = (req, next) => {
  if (!(req.body instanceof FormData)) return next(req);

  const progress = inject(UploadProgressService);
  const uploadId = progress.start(getUploadLabel(req.body));

  return next(req.clone({ reportProgress: true })).pipe(
    tap((event) => {
      if (event.type === HttpEventType.UploadProgress) {
        progress.update(uploadId, event.loaded, event.total);
      }
    }),
    finalize(() => progress.finish(uploadId)),
  );
};

function getUploadLabel(formData: FormData): string {
  for (const value of formData.values()) {
    if (typeof File !== 'undefined' && value instanceof File && value.name) return value.name;
  }

  return 'upload';
}
