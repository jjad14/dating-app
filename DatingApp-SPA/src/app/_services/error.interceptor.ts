import { Injectable } from '@angular/core';
import { HttpInterceptor, HttpErrorResponse, HTTP_INTERCEPTORS } from '@angular/common/http';
import { catchError } from 'rxjs/operators';
import { throwError } from 'rxjs';

// intercept method - intercept the request and catch any errors
@Injectable()
export class ErrorInterceptor implements HttpInterceptor {
  intercept(
    req: import('@angular/common/http').HttpRequest<any>, // request itself
    next: import('@angular/common/http').HttpHandler      // what happens next
  ): import('rxjs').Observable<import('@angular/common/http').HttpEvent<any>> {
    return next.handle(req).pipe(
        catchError(error => {
            // 401 errors
            if (error.status === 401) {
                return throwError(error.statusText);
            }

            if (error instanceof HttpErrorResponse) {
                // application error will be 500 internal server error type of errors, these are expceptions we'll get back from the server
                const applicationError = error.headers.get('Application-Error');
                if (applicationError) {
                    return throwError(applicationError);
                }

                // In Console we want to save error from HttpErrorResponse.error
                const serverError = error.error;

                // store model state errors - validations stuff like the password is too short
                let modelStateErrors = '';
                if (serverError.errors && typeof serverError.errors === 'object') {
                    for (const key in serverError.errors) {
                        if (serverError.errors[key]) {
                            modelStateErrors += serverError.errors[key] + '\n';
                        }
                    }
                }
                // return modelStateErros or serverError (if either is empty then only show what exists)
                // Server Error is for errors that might not have been captured
                return throwError(modelStateErrors || serverError || 'Server Error');
            }
        })
    );
  }
}

// add to the providers array
// registering a new interceptor provider to the anuglar http interceptor array of providers that already exists that we dont see
export const ErrorInterceptorProvider = {
    provide: HTTP_INTERCEPTORS,  // assign HTTP_INTERCEPTORS to ErrorInterceptorProvider
    useClass: ErrorInterceptor,  // specify interceptor class
    multi: true                  // HTTP_INTERCEPTORS can have multiple interceptors
};
