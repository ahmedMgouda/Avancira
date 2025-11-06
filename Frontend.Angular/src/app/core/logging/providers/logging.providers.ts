import { Provider } from '@angular/core';

import { ErrorHandlerService } from '../services/error-handler.service';
import { LogMonitorService } from '../services/log-monitor.service';
import { LoggerService } from '../services/logger.service';
import { NavigationTraceService } from '../services/navigation-trace.service';
import { SpanManagerService } from '../services/span-manager.service';
import { TraceManagerService } from '../services/trace-manager.service';

export function provideLogging(): Provider[] {
  return [
    LoggerService,
    LogMonitorService,
    ErrorHandlerService,
    TraceManagerService,
    SpanManagerService,
    NavigationTraceService
  ];
}