// ─────────────────────────────────────────────────────────────
// Setup Function (use in app.config.ts)
// ─────────────────────────────────────────────────────────────
export { loadingInterceptor,provideLoading } from './provide-loading';

// ─────────────────────────────────────────────────────────────
// Service & Types (use in components/services)
// ─────────────────────────────────────────────────────────────
export { 
  type LoadingConfig,
  type LoadingDiagnostics,
  type LoadingService,
  type OperationInfo,
  type RequestInfo,
  type RequestMetadata} from './loading.service';

// ─────────────────────────────────────────────────────────────
// Directive (use in templates)
// ─────────────────────────────────────────────────────────────
export { LoadingDirective } from './loading.directive';

// ─────────────────────────────────────────────────────────────
// UI Components (use in app.component.html)
// ─────────────────────────────────────────────────────────────
export { GlobalLoaderComponent } from './global-loader.component';
export { TopProgressBarComponent } from './top-progress-bar.component';

// ─────────────────────────────────────────────────────────────
// Internal Implementation (DO NOT EXPORT)
// ─────────────────────────────────────────────────────────────
// - provideRouteLoading (used internally by provideLoading)
// - LOADING_CONFIG (use provideLoading config instead)