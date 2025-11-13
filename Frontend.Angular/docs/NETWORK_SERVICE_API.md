# Network Service API Documentation

## Overview

The `NetworkService` provides comprehensive network connectivity monitoring and health checking for Angular applications. It uses Angular Signals for reactive state management and is fully compatible with zoneless Angular.

---

## Table of Contents

- [Installation](#installation)
- [Configuration](#configuration)
- [Public API](#public-api)
- [Signals](#signals)
- [Methods](#methods)
- [Types](#types)
- [Usage Examples](#usage-examples)
- [Advanced Features](#advanced-features)
- [Troubleshooting](#troubleshooting)

---

## Installation

The service is provided in root by default:

```typescript
import { NetworkService } from '@core/network';

// Inject in component or service
class MyComponent {
  private readonly networkService = inject(NetworkService);
}
```

---

## Configuration

### Network Config Provider

```typescript
import { NETWORK_CONFIG } from '@core/config/network.config';

// In app.config.ts or component providers
export const appConfig: ApplicationConfig = {
  providers: [
    // ... other providers
    {
      provide: NETWORK_CONFIG,
      useValue: {
        healthEndpoint: '/api/health',
        checkInterval: 30000,
        maxAttempts: 3
      }
    }
  ]
};
```

### Configuration Options

```typescript
interface NetworkConfig {
  healthEndpoint: string;    // Required: Health check endpoint
  checkInterval?: number;    // Optional: Check interval in ms (default: 30000)
  maxAttempts?: number;      // Optional: Max retry attempts (default: 3)
}
```

---

## Public API

### Signals

#### `isOnline: Signal<boolean>`
Browser online status (navigator.onLine)

```typescript
const online = networkService.isOnline();
// true: Browser reports online
// false: Browser reports offline
```

#### `isHealthy: Signal<boolean>`
Overall health status (online AND backend reachable)

```typescript
const healthy = networkService.isHealthy();
// true: Online and backend responding
// false: Offline or backend unreachable
```

#### `consecutiveErrors: Signal<number>`
Number of consecutive failed health checks

```typescript
const errors = networkService.consecutiveErrors();
// 0: No errors
// 1-2: Some failures, still checking
// 3+: Marked as unhealthy
```

#### `networkState: Signal<NetworkStatus>`
Complete network state in a single object

```typescript
const state = networkService.networkState();
// {
//   online: true,
//   healthy: true,
//   consecutiveErrors: 0,
//   lastCheck: Date
// }
```

### Methods

#### `startMonitoring(config: NetworkConfig): void`
Start health check monitoring

```typescript
networkService.startMonitoring({
  healthEndpoint: '/api/health',
  checkInterval: 30000,
  maxAttempts: 3
});
```

#### `stopMonitoring(): void`
Stop health check monitoring

```typescript
networkService.stopMonitoring();
```

#### `getStatus(): NetworkStatus`
Get current network status (non-reactive)

```typescript
const status = networkService.getStatus();
console.log(status);
// {
//   online: true,
//   healthy: true,
//   consecutiveErrors: 0,
//   lastCheck: Date
// }
```

#### `markSuccess(): void`
Reset error counter (use after successful request)

```typescript
// In HTTP interceptor
if (response.ok) {
  networkService.markSuccess();
}
```

#### `trackError(networkRelated = true): void`
Increment error counter (use after failed request)

```typescript
// In HTTP interceptor
if (isNetworkError(error)) {
  networkService.trackError(true);
}
```

#### `getDiagnostics(): object`
Get comprehensive diagnostics for debugging

```typescript
const diagnostics = networkService.getDiagnostics();
console.log(diagnostics);
// {
//   status: { online, healthy, consecutiveErrors, lastCheck },
//   browserOnline: true,
//   healthCheckActive: true,
//   checkCount: 5,
//   currentInterval: 30000,
//   nextTimeout: 1000,
//   activeToasts: { offline: false, healthWarning: false }
// }
```

---

## Types

### NetworkStatus

```typescript
interface NetworkStatus {
  online: boolean;              // Browser online status
  healthy: boolean;             // Overall health (online + backend OK)
  consecutiveErrors: number;    // Failed attempts count
  lastCheck: Date | null;       // Last health check timestamp
}
```

### NetworkConfig

```typescript
interface NetworkConfig {
  healthEndpoint: string;    // Health check URL
  checkInterval?: number;    // Check interval in milliseconds
  maxAttempts?: number;      // Max retry attempts before unhealthy
}
```

---

## Usage Examples

### Example 1: Basic Component Usage

```typescript
import { Component, inject } from '@angular/core';
import { NetworkService } from '@core/network';

@Component({
  selector: 'app-my-component',
  template: `
    <div class="status">
      @if (networkService.isHealthy()) {
        <span class="online">Connected</span>
      } @else {
        <span class="offline">Disconnected</span>
      }
    </div>
  `
})
export class MyComponent {
  protected readonly networkService = inject(NetworkService);
}
```

### Example 2: Using Computed Signals

```typescript
import { Component, computed, inject } from '@angular/core';
import { NetworkService } from '@core/network';

@Component({
  selector: 'app-status-badge',
  template: `
    <div [class]="statusClass()">
      {{ statusText() }}
    </div>
  `
})
export class StatusBadgeComponent {
  private readonly networkService = inject(NetworkService);

  protected readonly statusClass = computed(() => 
    this.networkService.isHealthy() ? 'badge-success' : 'badge-danger'
  );

  protected readonly statusText = computed(() => {
    const state = this.networkService.networkState();
    if (!state.online) return 'Offline';
    if (!state.healthy) return `Server Unreachable (${state.consecutiveErrors} attempts)`;
    return 'Online';
  });
}
```

### Example 3: HTTP Interceptor Integration

```typescript
import { inject } from '@angular/core';
import { HttpInterceptorFn } from '@angular/common/http';
import { tap, catchError } from 'rxjs';
import { NetworkService } from '@core/network';

export const networkTrackingInterceptor: HttpInterceptorFn = (req, next) => {
  const networkService = inject(NetworkService);

  return next(req).pipe(
    tap(() => {
      // Mark successful requests
      networkService.markSuccess();
    }),
    catchError(error => {
      // Track network-related errors
      if (isNetworkError(error)) {
        networkService.trackError(true);
      }
      throw error;
    })
  );
};

function isNetworkError(error: any): boolean {
  return error.status === 0 || 
         error.status === 503 || 
         error.status === 504 ||
         error.name === 'TimeoutError';
}
```

### Example 4: Effect-based Reactions

```typescript
import { Component, effect, inject } from '@angular/core';
import { NetworkService } from '@core/network';

@Component({
  selector: 'app-auto-save',
  template: `...`
})
export class AutoSaveComponent {
  private readonly networkService = inject(NetworkService);

  constructor() {
    // React to network state changes
    effect(() => {
      const state = this.networkService.networkState();
      
      if (state.healthy) {
        this.enableAutoSave();
      } else {
        this.disableAutoSave();
        this.showOfflineWarning();
      }
    });
  }

  private enableAutoSave() {
    // Resume auto-save
  }

  private disableAutoSave() {
    // Pause auto-save
  }

  private showOfflineWarning() {
    // Show warning to user
  }
}
```

### Example 5: Conditional API Calls

```typescript
import { Component, inject } from '@angular/core';
import { NetworkService } from '@core/network';
import { DataService } from './data.service';

@Component({
  selector: 'app-data-loader',
  template: `...`
})
export class DataLoaderComponent {
  private readonly networkService = inject(NetworkService);
  private readonly dataService = inject(DataService);

  loadData() {
    // Check before making API call
    if (!this.networkService.isHealthy()) {
      this.showOfflineMessage();
      return;
    }

    this.dataService.loadData().subscribe({
      next: (data) => this.handleData(data),
      error: (error) => this.handleError(error)
    });
  }

  private showOfflineMessage() {
    alert('Cannot load data while offline');
  }

  private handleData(data: any) {
    // Process data
  }

  private handleError(error: any) {
    // Handle error
  }
}
```

---

## Advanced Features

### Adaptive Check Intervals

The service automatically adjusts check intervals based on network health:

- **Healthy**: 30-second intervals (configurable)
- **Unhealthy**: 10-second intervals (faster recovery detection)

```typescript
// Automatically handled by the service
// No manual intervention needed
```

### Exponential Backoff

Health checks use progressive timeouts:

- **1st attempt**: 1-second timeout (fast-fail)
- **2nd attempt**: 3-second timeout (quick retry)
- **3rd attempt**: 5-second timeout (final check)

```typescript
// Automatically handled by the service
// Configurable via maxAttempts in config
```

### Grace Period

First 2 health checks skip notifications to prevent startup spam:

```typescript
// Automatically handled by the service
// Prevents false alarms on app initialization
```

### Toast Management

Automatic toast notifications with smart deduplication:

- **Offline**: Persistent error toast
- **Server unreachable**: Persistent warning toast
- **Recovery**: Success toast with auto-dismiss

```typescript
// Automatically handled by the service
// Uses ToastManager for notifications
```

---

## Troubleshooting

### Issue: Health checks not running

**Solution**: Verify configuration is provided

```typescript
// Check if NETWORK_CONFIG is provided
const diagnostics = networkService.getDiagnostics();
console.log(diagnostics.healthCheckActive); // Should be true
```

### Issue: Too many false positives

**Solution**: Increase retry attempts

```typescript
{
  provide: NETWORK_CONFIG,
  useValue: {
    healthEndpoint: '/api/health',
    maxAttempts: 4 // Increase from 3 to 4
  }
}
```

### Issue: Slow recovery detection

**Solution**: The service already uses 10s intervals when unhealthy. If still too slow:

```typescript
// Modify in network.service.ts
const DEFAULT_UNHEALTHY_INTERVAL = 5000; // Reduce from 10s to 5s
```

### Issue: Battery drain concerns

**Solution**: Increase healthy check interval

```typescript
{
  provide: NETWORK_CONFIG,
  useValue: {
    healthEndpoint: '/api/health',
    checkInterval: 60000 // Increase from 30s to 60s
  }
}
```

### Issue: Need debugging information

**Solution**: Use diagnostics method

```typescript
const diagnostics = networkService.getDiagnostics();
console.table(diagnostics.status);
console.table(diagnostics.activeToasts);
```

---

## Best Practices

### ✅ DO:

1. **Use signals for reactive updates**
   ```typescript
   const isHealthy = computed(() => networkService.isHealthy());
   ```

2. **Check before critical operations**
   ```typescript
   if (networkService.isHealthy()) {
     await saveData();
   }
   ```

3. **Use networkState for complete information**
   ```typescript
   const state = networkService.networkState();
   ```

4. **Integrate with HTTP interceptors**
   ```typescript
   networkService.trackError(isNetworkError(error));
   ```

### ❌ DON'T:

1. **Don't poll getStatus() repeatedly**
   ```typescript
   // Bad
   setInterval(() => {
     const status = networkService.getStatus();
   }, 1000);

   // Good - use signals
   effect(() => {
     const state = networkService.networkState();
   });
   ```

2. **Don't start monitoring multiple times**
   ```typescript
   // Bad
   networkService.startMonitoring(config);
   networkService.startMonitoring(config); // Duplicate!

   // Good - service handles this internally
   networkService.startMonitoring(config);
   ```

3. **Don't forget to check isHealthy before requests**
   ```typescript
   // Bad
   this.http.post('/api/data', data).subscribe();

   // Good
   if (networkService.isHealthy()) {
     this.http.post('/api/data', data).subscribe();
   }
   ```

---

## Performance Considerations

- **Memory**: Service uses minimal memory (<1KB state)
- **CPU**: Health checks run in background, minimal CPU impact
- **Network**: 1 health check per interval (configurable)
- **Battery**: Optimized intervals reduce battery drain

---

## Browser Compatibility

- ✅ Chrome 90+
- ✅ Firefox 88+
- ✅ Safari 14+
- ✅ Edge 90+

Requires:
- `navigator.onLine` API
- Angular 17+ (Signals)
- RxJS 7+

---

## Additional Resources

- [Core Services Refactoring Guide](./CORE_SERVICES_REFACTORING.md)
- [Network Status Indicator Component](../src/app/core/network/components/network-status-indicator/)
- [Angular Signals Documentation](https://angular.io/guide/signals)

---

**Document Version:** 1.0  
**Last Updated:** 2025-01-14  
**Compatibility:** Angular 17+