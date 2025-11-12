// Public API
export { GlobalLoaderComponent } from './components/global-loader.component';
export { TopProgressBarComponent } from './components/top-progress-bar.component';
export { LoadingDirective } from './directives/loading.directive';
export { loadingInterceptor } from './interceptors/loading.interceptor';
export { provideLoading } from './providers/loading.provider';
export { LoadingService } from './services/loading.service';

/**
 * Loading System - Self-Contained Components
 * ═══════════════════════════════════════════════════════════════
 * All styles are embedded in components/directives - no external CSS needed!
 * 
 * 1. Setup (app.config.ts):
 * ```typescript
 * import { provideLoading, loadingInterceptor } from '@core/loading';
 * 
 * export const appConfig = {
 *   providers: [
 *     provideHttpClient(withInterceptors([loadingInterceptor])),
 *     provideLoading()
 *   ]
 * };
 * ```
 * 
 * 2. Add to app.component.ts:
 * ```typescript
 * import { GlobalLoaderComponent, TopProgressBarComponent } from '@core/loading';
 * 
 * @Component({
 *   imports: [GlobalLoaderComponent, TopProgressBarComponent],
 *   template: `
 *     <app-global-loader />
 *     <app-top-progress-bar />
 *     <router-outlet />
 *   `
 * })
 * ```
 * 
 * 3. Button Loading:
 * ```html
 * <button [loading]="saving()">Save</button>
 * <button [loading]="saving()" size="lg" color="#10b981">Save</button>
 * ```
 * 
 * 4. Container Loading:
 * ```html
 * <div [loading]="loading()" mode="overlay">
 *   <p>Content stays visible</p>
 * </div>
 * ```
 * 
 * 5. Manual Global Loading:
 * ```typescript
 * loader = inject(LoadingService);
 * 
 * async save() {
 *   this.loader.showGlobal('Saving...');
 *   try {
 *     await this.api.save();
 *   } finally {
 *     this.loader.hideGlobal();
 *   }
 * }
 * ```
 * 
 * 6. Skip HTTP Loading:
 * ```typescript
 * http.get('/api/data', {
 *   headers: { 'X-Skip-Loading': 'true' }
 * });
 * ```
 */