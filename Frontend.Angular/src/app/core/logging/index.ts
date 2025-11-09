// core/logging/index.ts
/**
 * Logging Module - Public API
 * ═══════════════════════════════════════════════════════════════════════
 * Comprehensive logging system with W3C trace context
 * 
 * CHANGES:
 * ✅ Removed merged services (TraceManagerService, SpanManagerService)
 * ✅ Removed duplicate model exports
 * ✅ Fixed import paths
 * ✅ Cleaner exports
 */

// ─────────────────────────────────────────────────────────────────────
// Models & Types
// ─────────────────────────────────────────────────────────────────────
export type { 
  BaseLogEntry,
  Span,
  StandardError,
  TraceContext} from './models';
export type { LogType } from './models';
export { LogLevel } from './models';

// ─────────────────────────────────────────────────────────────────────
// Services
// ─────────────────────────────────────────────────────────────────────
export { ErrorHandlerService } from './services/error-handler.service';
export { LogMonitorService } from './services/log-monitor.service';
export { LoggerService } from './services/logger.service';
export { NavigationTraceService } from './services/navigation-trace.service';
export { TraceService } from './services/trace.service';

// ─────────────────────────────────────────────────────────────────────
// Interceptors
// ─────────────────────────────────────────────────────────────────────
export { httpLoggingInterceptor } from './interceptors/http-logging.interceptor';

// ─────────────────────────────────────────────────────────────────────
// Providers
// ─────────────────────────────────────────────────────────────────────
export { provideLogging } from './providers/logging.providers';

// ─────────────────────────────────────────────────────────────────────
// Utilities
// ─────────────────────────────────────────────────────────────────────
export { DataSanitizer } from './utils/data-sanitizer.utility';
export { SourceExtractor } from './utils/source-extractor.utility';

/**
 * ═════════════════════════════════════════════════════════════════════
 * Usage Examples
 * ═════════════════════════════════════════════════════════════════════
 * 
 * 1. Basic Logging:
 * ```typescript
 * import { LoggerService, LogLevel } from '@core/logging';
 * 
 * export class MyComponent {
 *   private logger = inject(LoggerService);
 *   
 *   doSomething() {
 *     this.logger.info('Operation started');
 *     this.logger.error('Operation failed', error);
 *   }
 * }
 * ```
 * 
 * 2. Trace Context:
 * ```typescript
 * import { TraceService } from '@core/logging';
 * 
 * export class MyService {
 *   private trace = inject(TraceService);
 *   
 *   async fetchData() {
 *     const span = this.trace.createSpan('fetch-data');
 *     try {
 *       await this.api.getData();
 *       this.trace.endSpan(span.spanId);
 *     } catch (error) {
 *       this.trace.endSpan(span.spanId, { error });
 *     }
 *   }
 * }
 * ```
 * 
 * 3. Error Handling:
 * ```typescript
 * import { ErrorHandlerService } from '@core/logging';
 * 
 * export class MyComponent {
 *   private errorHandler = inject(ErrorHandlerService);
 *   
 *   handleError(error: unknown) {
 *     const standardError = this.errorHandler.handle(error);
 *     console.error(standardError.userMessage);
 *   }
 * }
 * ```
 * 
 * 4. App Configuration (app.config.ts):
 * ```typescript
 * import { provideLogging, httpLoggingInterceptor } from '@core/logging';
 * 
 * export const appConfig: ApplicationConfig = {
 *   providers: [
 *     provideHttpClient(
 *       withInterceptors([
 *         httpLoggingInterceptor,
 *         // ... other interceptors
 *       ])
 *     ),
 *     ...provideLogging()
 *   ]
 * };
 * ```
 */