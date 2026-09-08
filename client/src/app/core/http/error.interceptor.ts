import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { AuthService } from '@core/auth/auth.service';
import { ToastService } from '@service/toast.service';
import { catchError, throwError, timeout, TimeoutError } from 'rxjs';

export const errorInterceptor: HttpInterceptorFn = (req, next) => {
  const auth = inject(AuthService);
  const toast = inject(ToastService);
  return next(req).pipe(
    timeout(15_000),
    catchError((err: unknown) => {
      if (err instanceof TimeoutError) {
        toast.error('La requête a expiré. Veuillez réessayer.');
      } else {
        const httpError = err as HttpErrorResponse;

        if (httpError.status === 401) {
          auth.forceLogout();
        } else if (httpError.status === 403) {
          toast.error("Vous n'êtes pas autorisé à effectuer cette action.");
        } else if (httpError.status >= 500) {
          toast.error('Une erreur serveur est survenue. Veuillez réessayer.');
        }
      }
      return throwError(() => err);
    }),
  );
};
