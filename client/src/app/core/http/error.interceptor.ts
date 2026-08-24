import { HttpInterceptorFn, HttpErrorResponse } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, throwError } from 'rxjs';
import { AuthService } from '@core/auth/auth.service';
import { ToastService } from '@service/toast.service';

export const errorInterceptor: HttpInterceptorFn = (req, next) => {
  const auth = inject(AuthService);
  const toast = inject(ToastService);
  return next(req).pipe(
    catchError((err: HttpErrorResponse) => {
      if (err.status === 401) {
        auth.forceLogout();
      } else if (err.status === 403) {
        toast.error('Vous n\'êtes pas autorisé à effectuer cette action.');
      } else if (err.status >= 500) {
        toast.error('Une erreur serveur est survenue. Veuillez réessayer.');
      }
      return throwError(() => err);
    })
  );
};
