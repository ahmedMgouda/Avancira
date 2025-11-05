import { ErrorHandler, inject,Injectable } from '@angular/core';

import { ErrorHandlerService } from '../services/error-handler.service';

@Injectable()
export class GlobalErrorHandler implements ErrorHandler {
  private readonly errorHandlerService = inject(ErrorHandlerService);
  
  handleError(error: any): void {
    const standardError = this.errorHandlerService.handle(error);
    
    console.error('[GlobalErrorHandler]', {
      errorId: standardError.errorId,
      message: standardError.userMessage,
      code: standardError.code
    });
  }
}
