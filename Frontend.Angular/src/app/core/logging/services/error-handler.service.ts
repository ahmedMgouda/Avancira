/**
 * Error Handler Service - Phase 3 Refactored
 * ✅ Uses ErrorClassifier for all error classification
 * ✅ Cleaner code with less duplication
 */

import { HttpErrorResponse } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';

import { LoggerService } from './logger.service';

import { ErrorClassifier } from '../../utils/error-classifier';
import { IdGenerator } from '../../utils/id-generator';
import { StandardError } from '../models/standard-error.model';
import { SourceExtractor } from '../utils/source-extractor.util';

@Injectable({ providedIn: 'root' })
export class ErrorHandlerService {
  private readonly logger = inject(LoggerService);

  handle(error: unknown): StandardError {
    if (error instanceof HttpErrorResponse) {
      return this.handleHttpError(error);
    }

    if (error instanceof Error) {
      return this.handleJavaScriptError(error);
    }

    return this.handleUnknownError(error);
  }

  private handleHttpError(error: HttpErrorResponse): StandardError {
    const errorId = IdGenerator.generateErrorId();
    
    // ✅ Use ErrorClassifier
    const classification = ErrorClassifier.classify(error);

    if (classification.isProblemDetails) {
      const problem = error.error;

      this.logger.error(`${error.status} ${problem.title}`, null, {
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
          severity: classification.category === 'network' ? 'warning' : ErrorClassifier.getSeverity(error)
        }
      });

      return {
        errorId,
        userMessage: problem.detail || problem.title,
        userTitle: problem.title,
        severity: ErrorClassifier.getSeverity(error),
        code: ErrorClassifier.extractCodeFromProblemType(problem.type),
        timestamp: new Date(),
        originalError: error
      };
    }

    // Standard HTTP error (no Problem Details)
    const code = ErrorClassifier.statusToCode(error.status);
    const userMessage = this.getHttpErrorMessage(error.status);

    this.logger.error(`${error.status} ${error.statusText}`, null, {
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
        code,
        type: 'HttpError',
        message: {
          user: userMessage,
          technical: `${error.status} ${error.statusText}: ${error.message}`
        },
        severity: ErrorClassifier.getSeverity(error)
      }
    });

    return {
      errorId,
      userMessage,
      userTitle: this.getHttpErrorTitle(error.status),
      severity: ErrorClassifier.getSeverity(error),
      code,
      timestamp: new Date(),
      originalError: error
    };
  }

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

  private getHttpErrorMessage(status: number): string {
    const messages: Record<number, string> = {
      400: 'The request was invalid. Please check your input.',
      401: 'You need to log in to access this resource.',
      403: 'You do not have permission to access this resource.',
      404: 'The requested resource was not found.',
      408: 'The request took too long. Please try again.',
      409: 'There was a conflict with the current state.',
      422: 'The data provided was invalid.',
      429: 'Too many requests. Please try again later.',
      500: 'A server error occurred. Please try again.',
      502: 'The server is temporarily unavailable.',
      503: 'The service is temporarily unavailable.',
      504: 'The request timed out. Please try again.'
    };

    return messages[status] || 'An error occurred. Please try again.';
  }

  private getHttpErrorTitle(status: number): string {
    const titles: Record<number, string> = {
      400: 'Bad Request',
      401: 'Unauthorized',
      403: 'Forbidden',
      404: 'Not Found',
      408: 'Request Timeout',
      409: 'Conflict',
      422: 'Validation Error',
      429: 'Too Many Requests',
      500: 'Server Error',
      502: 'Bad Gateway',
      503: 'Service Unavailable',
      504: 'Gateway Timeout'
    };

    return titles[status] || 'Error';
  }

  private normalizeErrorType(errorName: string): string {
    return errorName.replace(/Error$/, '').toUpperCase().replace(/([A-Z])/g, '_$1').substring(1);
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