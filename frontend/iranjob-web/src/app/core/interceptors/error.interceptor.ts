import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, throwError } from 'rxjs';
import { ErrorHandlerService } from '../services/error-handler.service';

export const errorInterceptor: HttpInterceptorFn = (request, next) => {
  const errorHandler = inject(ErrorHandlerService);

  return next(request).pipe(
    catchError((error: HttpErrorResponse) => {
      errorHandler.handleHttpError(error);
      return throwError(() => error);
    })
  );
};
