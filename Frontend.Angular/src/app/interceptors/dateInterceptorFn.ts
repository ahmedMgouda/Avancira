import { HttpEvent, HttpHandlerFn, HttpInterceptorFn, HttpRequest, HttpResponse } from '@angular/common/http';
import { inject } from '@angular/core';
import { map, Observable, switchMap } from 'rxjs';
import { format, isDate,isValid, parseISO } from 'date-fns';
import { toZonedTime } from 'date-fns-tz';

import { UserService } from '../services/user.service';


export const dateInterceptorFn: HttpInterceptorFn = (
  req: HttpRequest<any>,
  next: HttpHandlerFn
): Observable<HttpEvent<any>> => {
  // console.log('Date Interceptor - Checking request:', req);
  const userService = inject(UserService);

  // If request has no date fields, do nothing
  if (!hasDateFields(req.body)) {
    return next(req);
  }

  return userService.getTimeZone().pipe(
    switchMap(userTimeZone => {
      // console.log('User Time Zone:', userTimeZone);
      // Convert only new Date objects (skip already formatted strings)
      const body = convertDatesToUTC(req.body);
      req = req.clone({ body });

      return next(req).pipe(
        map((event: HttpEvent<any>) => {
          if (event instanceof HttpResponse && event.body) {
            console.log('Date Interceptor - Checking response:', event.body);

            // If response has no date fields, do nothing
            if (!hasDateFields(event.body)) {
              return event;
            }

            return event.clone({ body: convertDatesToLocal(event.body, userTimeZone) });
          }
          return event;
        })
      );
    })
  );
};

// Utility function to check if an object contains any date fields
function hasDateFields(obj: any): boolean {
  if (!obj || typeof obj !== 'object') return false;

  for (const key of Object.keys(obj)) {
    if (isDate(obj[key]) || (typeof obj[key] === 'string' && obj[key].endsWith('Z') && isValid(parseISO(obj[key])))) {
      return true; // Found a date field, so process this request/response
    } else if (typeof obj[key] === 'object' && obj[key] !== null) {
      if (hasDateFields(obj[key])) return true; // Recursively check nested objects
    }
  }
  return false;
}

// Convert only native Date objects to UTC before sending to backend
function convertDatesToUTC(obj: any): any {
  if (!obj || typeof obj !== 'object' || obj === null) return obj;

  for (const key of Object.keys(obj)) {
    if (isDate(obj[key])) {
      obj[key] = obj[key].toISOString();
    } else if (typeof obj[key] === 'object' && obj[key] !== null) {
      obj[key] = convertDatesToUTC(obj[key]); // Recursive call
    }
  }
  return obj;
}

// Convert UTC date strings back to user's local time
function convertDatesToLocal(obj: any, userTimeZone: string): any {
  if (!obj || typeof obj !== 'object' || obj === null) return obj;

  for (const key of Object.keys(obj)) {
    if (typeof obj[key] === 'string' && obj[key].endsWith('Z') && isValid(parseISO(obj[key]))) {
      const localTime = toZonedTime(parseISO(obj[key]), userTimeZone);
      obj[key] = format(localTime, 'yyyy-MM-dd HH:mm');
    } else if (typeof obj[key] === 'object' && obj[key] !== null) {
      obj[key] = convertDatesToLocal(obj[key], userTimeZone);
    }
  }
  return obj;
}
