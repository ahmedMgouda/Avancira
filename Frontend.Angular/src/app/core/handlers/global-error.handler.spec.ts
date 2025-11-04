// core/handlers/global-error.handler.spec.ts

import { HttpErrorResponse } from '@angular/common/http';
import { ErrorHandler, NgZone } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';

import { ErrorHandlerService } from '../services/error-handler.service';
import { LoggerService } from '../services/logger.service';
import { NotificationService } from '../services/notification.service';

import { GlobalErrorHandler } from './global-error.handler';

describe('GlobalErrorHandler', () => {
  let handler: GlobalErrorHandler;
  let errorHandlerService: jasmine.SpyObj<ErrorHandlerService>;
  let loggerService: jasmine.SpyObj<LoggerService>;
  let _notificationService: jasmine.SpyObj<NotificationService>;
  let _router: jasmine.SpyObj<Router>;
  let _ngZone: NgZone;

  beforeEach(() => {
    const errorHandlerSpy = jasmine.createSpyObj('ErrorHandlerService', [
      'handleHttpError',
      'isAppError'
    ]);
    const loggerSpy = jasmine.createSpyObj('LoggerService', [
      'error',
      'warn',
      'fatal',
      'debug',
      'getCorrelationId',
      'setCorrelationId'
    ]);
    const notificationSpy = jasmine.createSpyObj('NotificationService', [
      'fromAppError'
    ]);
    const routerSpy = jasmine.createSpyObj('Router', ['navigate'], { url: '/test' });

    TestBed.configureTestingModule({
      providers: [
        { provide: ErrorHandler, useClass: GlobalErrorHandler },
        { provide: ErrorHandlerService, useValue: errorHandlerSpy },
        { provide: LoggerService, useValue: loggerSpy },
        { provide: NotificationService, useValue: notificationSpy },
        { provide: Router, useValue: routerSpy }
      ]
    });

    handler = TestBed.inject(ErrorHandler) as GlobalErrorHandler;
    errorHandlerService = TestBed.inject(ErrorHandlerService) as jasmine.SpyObj<ErrorHandlerService>;
    loggerService = TestBed.inject(LoggerService) as jasmine.SpyObj<LoggerService>;
    _notificationService = TestBed.inject(NotificationService) as jasmine.SpyObj<NotificationService>;
    _router = TestBed.inject(Router) as jasmine.SpyObj<Router>;
    _ngZone = TestBed.inject(NgZone);

    loggerService.getCorrelationId.and.returnValue('test-correlation-id');
    errorHandlerService.isAppError.and.returnValue(false);
  });

  // ───────────────────────────────────────────────
  it('should handle JavaScript TypeError', () => {
    const error = new TypeError('Cannot read property of undefined');
    handler.handleError(error);
    expect(loggerService.error).toHaveBeenCalled();
  });

  it('should handle ReferenceError', () => {
    const error = new ReferenceError('x is not defined');
    handler.handleError(error);
    expect(loggerService.error).toHaveBeenCalled();
  });

  it('should handle Angular change detection errors', () => {
    const error = new Error('ExpressionChangedAfterItHasBeenCheckedError');
    handler.handleError(error);
    expect(loggerService.warn).toHaveBeenCalled();
  });

  it('should handle Angular template errors', () => {
    const error = new Error('NG0100: ExpressionChangedAfterItHasBeenCheckedError');
    handler.handleError(error);
    expect(loggerService.warn).toHaveBeenCalled();
  });

  // ───────────────────────────────────────────────
  // FIXED promise rejection tests
  // ───────────────────────────────────────────────

  it('should handle unhandled promise rejections (object reason)', () => {
    const event = { reason: { message: 'Async failure' } } as unknown as PromiseRejectionEvent;
    handler.handleError(event);
    expect(loggerService.error).toHaveBeenCalled();
  });

  it('should handle unhandled promise rejections (string reason)', () => {
    const event = { reason: 'String rejection' } as unknown as PromiseRejectionEvent;
    handler.handleError(event);
    expect(loggerService.error).toHaveBeenCalled();
  });

  // ───────────────────────────────────────────────
  it('should handle HTTP errors defensively', () => {
    const httpError = new HttpErrorResponse({
      error: 'Server error',
      status: 500,
      statusText: 'Internal Server Error',
      url: '/api/test'
    });
    errorHandlerService.handleHttpError.and.returnValue({
      message: 'Server error',
      code: 'SERVER_ERROR',
      status: 500,
      timestamp: new Date(),
      correlationId: 'test-id',
      severity: 'error'
    });
    handler.handleError(httpError);
    expect(loggerService.warn).toHaveBeenCalled();
  });

  it('should skip already handled errors', () => {
    errorHandlerService.isAppError.and.returnValue(true);
    handler.handleError({ message: 'Already handled' });
    expect(loggerService.debug).toHaveBeenCalled();
  });

  it('should handle chunk loading errors', () => {
    const error = new Error('Loading chunk 5 failed');
    handler.handleError(error);
    expect(loggerService.error).toHaveBeenCalled();
  });

  it('should suppress errors after threshold reached', () => {
    const error = new Error('Test error');
    for (let i = 0; i < 11; i++) handler.handleError(error);
    loggerService.error.calls.reset();
    handler.handleError(error);
    expect(loggerService.error).not.toHaveBeenCalled();
  });

  // ✅ FIX: adjust pattern for UUID correlation IDs
  it('should generate correlation ID if none exists', () => {
    loggerService.getCorrelationId.and.returnValue(null);
    handler.handleError(new Error('Test error'));
    expect(loggerService.setCorrelationId).toHaveBeenCalledWith(
      jasmine.stringMatching(/^[a-f0-9-]{36}$/)
    );
  });

  it('should notify user for chunk load errors in production', () => {
    (globalThis as any).environment = { production: true };
    handler.handleError(new Error('Loading chunk failed'));
    expect(_notificationService.fromAppError).toHaveBeenCalled();
    (globalThis as any).environment = { production: false };
  });

  it('should not throw if handler encounters internal error', () => {
    loggerService.error.and.throwError('Logger failed');
    expect(() => handler.handleError(new Error('Test error'))).not.toThrow();
  });
});
