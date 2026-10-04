import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { TranslateService } from '@ngx-translate/core';
import { AuthService } from '@core/auth/auth.service';
import { ToastService } from '@service/toast.service';
import { catchError, throwError, timeout, TimeoutError } from 'rxjs';

export const errorInterceptor: HttpInterceptorFn = (req, next) => {
  const auth = inject(AuthService);
  const toast = inject(ToastService);
  const translate = inject(TranslateService);

  return next(req).pipe(
    timeout(15_000),
    catchError((err: unknown) => {
      if (err instanceof TimeoutError) {
        toast.error(translate.instant('errors.timeout'));
      } else {
        const httpError = err as HttpErrorResponse;

        if (httpError.status === 401) {
          auth.forceLogout();
        } else if (httpError.status === 403) {
          toast.error(translate.instant('errors.forbidden'));
        } else if (httpError.status >= 500) {
          toast.error(translate.instant('errors.server'));
        } else if (httpError.status >= 400) {
          const code: string | undefined = httpError.error?.error;
          if (code) {
            const translated = translate.instant(code);
            // show translation if key exists, otherwise show raw message
            toast.error(translated !== code ? translated : code);
          }
        }
      }
      return throwError(() => err);
    }),
  );
};
