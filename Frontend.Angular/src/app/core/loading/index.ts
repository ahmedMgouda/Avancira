// core/loading/index.ts
/**
 * Loading Module - Public API
 * ═══════════════════════════════════════════════════════════════════════
 * Centralized exports for loading system
 */

// ─────────────────────────────────────────────────────────────────────
// Services
// ─────────────────────────────────────────────────────────────────────
export { LoadingService } from './services/loading.service';

// ─────────────────────────────────────────────────────────────────────
// Types & Interfaces (only what exists)
// ─────────────────────────────────────────────────────────────────────
export type { 
  RequestInfo, 
  RequestMetadata 
} from './services/loading.service';

// ─────────────────────────────────────────────────────────────────────
// Directives
// ─────────────────────────────────────────────────────────────────────
export { LoadingDirective } from './directives/loading.directive';

// ─────────────────────────────────────────────────────────────────────
// Components
// ─────────────────────────────────────────────────────────────────────
export { GlobalLoaderComponent } from './components/global-loader.component';
export { TopProgressBarComponent } from './components/top-progress-bar.component';

// ─────────────────────────────────────────────────────────────────────
// Interceptors
// ─────────────────────────────────────────────────────────────────────
export { loadingInterceptor } from './interceptors/loading.interceptor';

// ─────────────────────────────────────────────────────────────────────
// Providers
// ─────────────────────────────────────────────────────────────────────
export { provideLoading } from './providers/loading.provider';

/**
 * ═════════════════════════════════════════════════════════════════════
 * Usage Examples
 * ═════════════════════════════════════════════════════════════════════
 * 
 * 1. In app.config.ts:
 * ```typescript
 * import { provideLoading } from '@core/loading';
 * 
 * export const appConfig = {
 *   providers: [
 *     provideLoading()
 *   ]
 * };
 * ```
 * 
 * 2. In components:
 * ```typescript
 * import { LoadingService, LoadingDirective } from '@core/loading';
 * 
 * @Component({
 *   imports: [LoadingDirective]
 * })
 * export class MyComponent {
 *   loader = inject(LoadingService);
 *   
 *   async save() {
 *     this.loader.showGlobal('Saving...');
 *     await this.api.save();
 *     this.loader.hideGlobal();
 *   }
 * }
 * ```
 * 
 * 3. In templates:
 * ```html
 * <button [appLoading]="saving">Save</button>
 * <app-global-loader />
 * <app-top-progress-bar />
 * ```
 */