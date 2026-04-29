import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, throwError } from 'rxjs';
import { Auth } from '../services/auth';

export const errorHandlerInterceptor: HttpInterceptorFn = (req, next) => {
  const auth = inject(Auth);

  return next(req).pipe(
    catchError(err => {
      if (err.status === 401) {
        auth.logout();
      }
      return throwError(() => err);
    })
  );
};
