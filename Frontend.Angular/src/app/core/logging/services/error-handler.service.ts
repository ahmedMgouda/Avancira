import { HttpErrorResponse } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';

import { LoggerService } from './logger.service';
import { ErrorClassifier } from '../../utils/error-classifier.utility';
import { IdGenerator } from '../../utils/id-generator.utility';
import { StandardError } from '../models/standard-error.model';
import { SourceExtractor } from '../utils/source-extractor.utility';

/**
 * ═══════════════════════════════════════════════════════════════════════
 * ERROR HANDLER SERVICE - IMPROVED
 * ═══════════════════════════════════════════════════════════════════════
 * 
 * SINGLE RESPONSIBILITY:
 * - Transform any error → StandardError
 * - Log error ONCE (no duplicates)
 * - Provide user-friendly messages
 * 
 * USED BY:
 * - BaseHttpService (for all HTTP errors)
 * - Global Error Handler (for uncaught errors)
 * - Components (for manual error handling)
 */
@Injectable({ providedIn: 'root' })
export class ErrorHandlerService {
  private readonly logger = inject(LoggerService);

  /**
   * Handle any error and return StandardError
   * Logs error automatically - no need for duplicate logging
   */
  handle(error: unknown): StandardError {
    if (error instanceof HttpErrorResponse) {
      return this.handleHttpError(error);
    }

    if (error instanceof Error) {
      return this.handleJavaScriptError(error);
    }

    // Check if it's already a StandardError (from BaseHttpService)
    if (this.isStandardError(error)) {
      return error as StandardError;
    }

    return this.handleUnknownError(error);
  }

  /**
   * Handle HTTP errors (from API calls)
   */
  private handleHttpError(error: HttpErrorResponse): StandardError {
    const errorId = IdGenerator.generateErrorId();
    const classification = ErrorClassifier.classify(error);
    const metadata = classification.metadata;

    // Handle Problem Details (RFC 7807) responses
    if (classification.isProblemDetails) {
      const problem = error.error;

      this.logger.error(`HTTP ${error.status}: ${problem.title}`, null, {
        log: {
          id: errorId,
          source: 'HTTP',
          type: 'http'
        },
        http: {
          method: 'UNKNOWN',
          url: error.url || 'unknown',
          status_code: error.status,
          problem_details: {
            type: problem.type,
            title: problem.title,
            status: problem.status,
            detail: problem.detail,
            instance: problem.instance
          }
        },
        error: {
          id: errorId,
          kind: 'application',
          handled: true,
          code: ErrorClassifier.extractCodeFromProblemType(problem.type),
          type: 'HttpError',
          message: {
            user: problem.detail || problem.title,
            technical: `${error.status} ${error.statusText}: ${problem.detail}`
          },
          severity: metadata.severity
        }
      });

      return {
        errorId,
        userMessage: problem.detail || problem.title,
        userTitle: problem.title,
        severity: metadata.severity,
        code: ErrorClassifier.extractCodeFromProblemType(problem.type),
        timestamp: new Date(),
        originalError: error
      };
    }

    // Handle standard HTTP errors
    this.logger.error(`HTTP ${error.status}: ${error.statusText}`, null, {
      log: {
        id: errorId,
        source: 'HTTP',
        type: 'http'
      },
      http: {
        method: 'UNKNOWN',
        url: error.url || 'unknown',
        status_code: error.status,
        error_message: error.message
      },
      error: {
        id: errorId,
        kind: 'application',
        handled: true,
        code: metadata.code,
        type: 'HttpError',
        message: {
          user: metadata.userMessage,
          technical: `${error.status} ${error.statusText}: ${error.message}`
        },
        severity: metadata.severity
      }
    });

    return {
      errorId,
      userMessage: metadata.userMessage,
      userTitle: metadata.title,
      severity: metadata.severity,
      code: metadata.code,
      timestamp: new Date(),
      originalError: error
    };
  }

  /**
   * Handle JavaScript errors (exceptions, runtime errors)
   */
  private handleJavaScriptError(error: Error): StandardError {
    const errorId = IdGenerator.generateErrorId();
    const source = SourceExtractor.extractFromError(error);

    this.logger.error(error.message, error, {
      log: {
        id: errorId,
        source,
        type: 'error'
      },
      error: {
        id: errorId,
        kind: 'system',
        handled: false,
        code: this.normalizeErrorType(error.name),
        type: error.name,
        message: {
          user: 'An unexpected error occurred. Our team has been notified.',
          technical: error.message
        },
        severity: 'error',
        stack: error.stack,
        source: this.extractErrorSource(error)
      }
    });

    return {
      errorId,
      userMessage: 'An unexpected error occurred. Our team has been notified.',
      userTitle: 'Unexpected Error',
      severity: 'error',
      code: this.normalizeErrorType(error.name),
      timestamp: new Date(),
      originalError: error
    };
  }

  /**
   * Handle unknown errors
   */
  private handleUnknownError(error: unknown): StandardError {
    const errorId = IdGenerator.generateErrorId();
    const errorMessage = String(error);

    this.logger.error(errorMessage, null, {
      log: {
        id: errorId,
        source: 'System',
        type: 'error'
      },
      error: {
        id: errorId,
        kind: 'system',
        handled: false,
        code: 'UNKNOWN_ERROR',
        type: 'Unknown',
        message: {
          user: 'An unexpected error occurred. Our team has been notified.',
          technical: errorMessage
        },
        severity: 'error'
      }
    });

    return {
      errorId,
      userMessage: 'An unexpected error occurred. Our team has been notified.',
      userTitle: 'Unexpected Error',
      severity: 'error',
      code: 'UNKNOWN_ERROR',
      timestamp: new Date(),
      originalError: error
    };
  }

  // ═══════════════════════════════════════════════════════════════════════
  // HELPER METHODS
  // ═══════════════════════════════════════════════════════════════════════

  private isStandardError(error: unknown): boolean {
    return (
      typeof error === 'object' &&
      error !== null &&
      'errorId' in error &&
      'userMessage' in error &&
      'severity' in error
    );
  }

  private normalizeErrorType(errorName: string): string {
    return errorName
      .replace(/Error$/, '')
      .toUpperCase()
      .replace(/([A-Z])/g, '_$1')
      .substring(1);
  }

  private extractErrorSource(error: Error): { component?: string; file?: string; line?: number } | undefined {
    if (!error.stack) {
      return undefined;
    }

    const stackLines = error.stack.split('\n');
    if (stackLines.length < 2) {
      return undefined;
    }

    const match = stackLines[1].match(/at\s+(.+?)\s+\((.+?):(\d+):\d+\)/);
    if (match) {
      return {
        component: match[1],
        file: match[2],
        line: parseInt(match[3], 10)
      };
    }

    return undefined;
  }
}
