import { HttpErrorResponse } from '@angular/common/http';
import { ErrorHandler, inject,Injectable } from '@angular/core';

import { LoggerService } from '../logging/services/logger.service';
import { ToastService } from '../toast/toast.service';

import { environment } from '../../environments/environment';

@Injectable()
export class GlobalErrorHandler implements ErrorHandler {
  private readonly logger = inject(LoggerService);
  private readonly toast = inject(ToastService);

  handleError(error: any): void {
    // Skip if already logged by HTTP error interceptor
    if (error.__logged) {
      return;
    }

    // Handle HTTP errors
    if (error instanceof HttpErrorResponse) {
      this.handleHttpError(error);
      return;
    }

    // Handle client-side errors
    if (error instanceof Error) {
      this.handleClientError(error);
      return;
    }

    // Handle unknown errors
    this.handleUnknownError(error);
  }

  private handleHttpError(error: HttpErrorResponse): void {
    // Should not reach here if interceptor is working correctly
    // But handle it anyway as a safety net
    
    this.logger.error(
      `Unhandled HTTP Error: ${error.status} ${error.statusText}`,
      error,
      {
        log: {
          source: 'GlobalErrorHandler',
          type: 'http_error'
        },
        http: {
          method: 'UNKNOWN',
          url: error.url || 'unknown',
          status_code: error.status
        }
      }
    );

    // Show toast for 5xx errors only
    if (error.status >= 500 && environment.clientErrorHandling.showClientErrorToasts) {
      this.toast.error(
        'A server error occurred. Please try again later.',
        'Server Error'
      );
    }
  }

  private handleClientError(error: Error): void {
    // Log client-side errors
    this.logger.fatal(
      `Uncaught Error: ${error.message}`,
      error,
      {
        log: {
          source: 'GlobalErrorHandler',
          type: 'uncaught_error'
        },
        error: {
          id: `err-${Date.now()}`,
          kind: 'system',
          handled: false,
          code: error.name.toUpperCase().replace(/ERROR$/, ''),
          type: error.name,
          message: {
            user: 'An unexpected error occurred. Please refresh the page.',
            technical: error.message
          },
          severity: 'critical',
          stack: environment.clientErrorHandling.logStackTraces ? error.stack : undefined
        }
      }
    );

    // Show toast in development
    if (environment.clientErrorHandling.showClientErrorToasts) {
      this.toast.error(
        'An unexpected error occurred. Please refresh the page.',
        'Application Error',
        0 // Permanent toast
      );
    }

    // Log to console for debugging
    console.error('[GlobalErrorHandler] Uncaught error:', error);
  }

  private handleUnknownError(error: any): void {
    const errorMessage = String(error);

    this.logger.fatal(
      `Unknown Error: ${errorMessage}`,
      null,
      {
        log: {
          source: 'GlobalErrorHandler',
          type: 'unknown_error'
        }
      }
    );

    console.error('[GlobalErrorHandler] Unknown error:', error);
  }
}