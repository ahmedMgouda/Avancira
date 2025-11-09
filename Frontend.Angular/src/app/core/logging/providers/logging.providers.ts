// core/logging/providers/logging.providers.ts
/**
 * Logging Providers - UPDATED
 * ═══════════════════════════════════════════════════════════════════════
 * 
 * CHANGES:
 * ✅ Removed TraceManagerService (merged into TraceService)
 * ✅ Removed SpanManagerService (merged into TraceService)
 * ✅ Added TraceService
 */

import { Provider } from '@angular/core';

import { ErrorHandlerService } from '../services/error-handler.service';
import { LoggerService } from '../services/logger.service';
import { NavigationTraceService } from '../services/navigation-trace.service';
import { TraceService } from '../services/trace.service';

export function provideLogging(): Provider[] {
  return [
    LoggerService,
    ErrorHandlerService,
    TraceService,          
    NavigationTraceService
  ];
}