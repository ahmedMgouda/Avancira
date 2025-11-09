import { HttpErrorResponse } from '@angular/common/http';
import { ErrorHandler, inject, Injectable } from '@angular/core';

import { ErrorHandlerService } from '../logging/services/error-handler.service';
import { ToastManager } from '../toast/services/toast-manager.service';

import { environment } from '../../environments/environment';
import { StandardError } from '../logging/models/standard-error.model';

@Injectable()
export class GlobalErrorHandler implements ErrorHandler {
  private readonly toast = inject(ToastManager);
  private readonly errorHandler = inject(ErrorHandlerService);

  handleError(error: unknown): void {
    // Skip if already logged by HTTP error interceptor
    if ((error as any)?.__logged) {
      return;
    }

    const handledError = this.errorHandler.handle(error);
    const originalError = handledError.originalError;

    if (originalError instanceof HttpErrorResponse) {
      this.handleHttpToast(originalError);
      return;
    }

    if (originalError instanceof Error) {
      this.handleClientErrorToast(handledError);
      return;
    }

    this.handleUnknownErrorToast(handledError);
  }

  private handleHttpToast(error: HttpErrorResponse): void {
    // Should not reach here if interceptor is working correctly
    // But handle it anyway as a safety net

    if (error.status >= 500 && environment.clientErrorHandling.showClientErrorToasts) {
      this.toast.error(
        'A server error occurred. Please try again later.',
        'Server Error'
      );
    }
  }

  private handleClientErrorToast(handledError: StandardError): void {
    if (environment.clientErrorHandling.showClientErrorToasts) {
      this.toast.error(
        handledError.userMessage,
        handledError.userTitle,
        0 // Permanent toast
      );
    }

    console.error('[GlobalErrorHandler] Uncaught error:', handledError.originalError);
  }

  private handleUnknownErrorToast(handledError: StandardError): void {
    if (environment.clientErrorHandling.showClientErrorToasts) {
      this.toast.error(
        handledError.userMessage,
        handledError.userTitle,
        0 // Permanent toast
      );
    }

    console.error('[GlobalErrorHandler] Unknown error:', handledError.originalError);
  }
}
