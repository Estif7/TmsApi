import { HttpInterceptorFn, HttpErrorResponse } from '@angular/common/http';
import { catchError, throwError } from 'rxjs';

export const errorInterceptor: HttpInterceptorFn = (req, next) => {
  return next(req).pipe(
    catchError((error: HttpErrorResponse) => {
      let message = 'An unexpected error occurred.';

      if (error.error?.detail) {
        message = error.error.detail;
      } else if (error.status === 401) {
        message = 'Your session has expired. Please log in again.';
      } else if (error.status === 403) {
        message = 'You do not have permission to perform this action.';
      }

      console.error(`[API Error ${error.status}]:`, message);
      return throwError(() => new Error(message));
    })
  );
};