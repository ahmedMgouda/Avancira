# Network Status Service

Comprehensive network monitoring with backend health check integration.

## 📋 Overview

The Network Status Service provides:

- ✅ Real-time online/offline detection
- ✅ Backend health verification via `/health` endpoint
- ✅ Connection quality monitoring (latency)
- ✅ Automatic retry with exponential backoff
- ✅ Toast notifications for status changes
- ✅ Reactive state management with signals

## 🚀 Usage

### Basic Usage

```typescript
import { inject, Component, effect } from '@angular/core';
import { NetworkStatusService } from '@app/core/network/network-status.service';

export class MyComponent {
  private network = inject(NetworkStatusService);
  
  // Reactive signals
  readonly isOnline = this.network.isOnline;
  readonly quality = this.network.quality;
  readonly backendStatus = this.network.backendStatus;
  
  constructor() {
    // React to connection changes
    effect(() => {
      if (this.network.isOnline()) {
        console.log('Connection restored!');
        this.retryFailedOperations();
      }
    });
  }
  
  private retryFailedOperations() {
    // Retry logic here
  }
}
```

### Manual Retry

```typescript
async onRetryClick() {
  const isOnline = await this.network.retry();
  
  if (isOnline) {
    this.toast.showSuccess('Connection restored!');
    // Retry failed operations
  } else {
    this.toast.showError('Still offline. Please check your connection.');
  }
}
```

### Wait for Connection

```typescript
async performCriticalOperation() {
  try {
    // Wait up to 30 seconds for connection
    await this.network.waitForOnline(30000);
    
    // Proceed with operation
    await this.api.saveCriticalData();
    
  } catch (error) {
    this.toast.showError('Operation timed out waiting for connection.');
  }
}
```

### Check Connection Quality

```typescript
checkUploadReady() {
  const quality = this.network.quality();
  
  if (quality === 'poor') {
    return confirm('Poor connection detected. Upload may be slow. Continue?');
  }
  
  return true;
}
```

## 📊 Available Signals

### State Signals

- `isOnline: Signal<boolean>` - Whether connected to backend
- `isOffline: Signal<boolean>` - Computed opposite of isOnline
- `isVerifying: Signal<boolean>` - Whether currently checking health
- `lastCheck: Signal<number>` - Timestamp of last health check
- `latency: Signal<number | null>` - Response time in milliseconds
- `backendStatus: Signal<string>` - Backend health status ('healthy', 'degraded', 'unhealthy')

### Computed Signals

- `quality: Signal<string>` - Connection quality ('excellent', 'good', 'fair', 'poor', 'unknown')
- `qualityMessage: Signal<string>` - Human-readable quality description

## 🔧 Configuration

The service has sensible defaults but can be customized:

```typescript
export class NetworkStatusService {
  // Configuration
  private readonly HEALTH_ENDPOINT = '/health';
  private readonly CHECK_INTERVAL = 30000; // 30 seconds
  private readonly REQUEST_TIMEOUT = 5000; // 5 seconds
  private readonly RETRY_ATTEMPTS = 3;
  private readonly RETRY_DELAY = 2000; // 2 seconds
}
```

## 🎯 Best Practices

### 1. Use Health Endpoint, Not Favicon

❌ **Bad:**
```typescript
fetch('/favicon.ico', { method: 'HEAD' })
```

✅ **Good:**
```typescript
this.http.get<HealthCheckResponse>('/health')
```

The dedicated health endpoint:
- Returns structured data
- Provides backend status
- Supports proper error handling
- Follows REST conventions

### 2. Show User-Friendly Notifications

✅ **Good:**
```typescript
private showOfflineNotification(): void {
  if (this._lastNotification() !== 'offline') {
    this.toast.showError('No internet connection. Please check your network.');
    this._lastNotification.set('offline');
  }
}
```

Avoid:
- Showing duplicate notifications
- Technical error messages
- Annoying the user with too many alerts

### 3. Handle Degraded State

```typescript
const status = this.network.backendStatus();

switch (status) {
  case 'healthy':
    // All features available
    break;
    
  case 'degraded':
    // Show warning, disable non-critical features
    this.toast.showWarning('Some features may be unavailable.');
    break;
    
  case 'unhealthy':
    // Show error, disable all features
    this.toast.showError('Service is temporarily unavailable.');
    break;
}
```

### 4. Implement Retry Logic

```typescript
async saveWithRetry(data: any) {
  let attempts = 0;
  const maxAttempts = 3;
  
  while (attempts < maxAttempts) {
    try {
      return await this.api.save(data);
    } catch (error) {
      attempts++;
      
      if (!this.network.isOnline()) {
        // Wait for connection
        await this.network.waitForOnline();
      } else if (attempts < maxAttempts) {
        // Exponential backoff
        await new Promise(resolve => 
          setTimeout(resolve, 1000 * Math.pow(2, attempts))
        );
      } else {
        throw error;
      }
    }
  }
}
```

## 🔍 Diagnostics

Get detailed diagnostics for debugging:

```typescript
const diagnostics = this.network.getDiagnostics();
console.log('Network Diagnostics:', diagnostics);

// Output:
// {
//   isOnline: true,
//   isVerifying: false,
//   lastCheck: Date,
//   latency: 45,
//   quality: 'excellent',
//   qualityMessage: 'Excellent connection',
//   backendStatus: 'healthy',
//   navigatorOnLine: true,
//   connectionType: '4g',
//   healthEndpoint: '/health'
// }
```

## 🎨 UI Integration (Optional)

While the service provides toast notifications by default, you can create custom UI:

### Status Indicator

```html
<div class="status-indicator" [class.online]="network.isOnline()">
  @if (network.isOnline()) {
    <span class="dot green"></span>
    <span>Online</span>
  } @else {
    <span class="dot red"></span>
    <span>Offline</span>
  }
</div>
```

### Connection Quality Badge

```html
<div class="quality-badge" [attr.data-quality]="network.quality()">
  {{ network.qualityMessage() }}
  @if (network.latency(); as latency) {
    <span class="latency">{{ latency }}ms</span>
  }
</div>
```

### Retry Button

```html
<button 
  (click)="onRetry()"
  [disabled]="network.isVerifying()">
  @if (network.isVerifying()) {
    <span class="spinner"></span>
    Checking...
  } @else {
    <span class="icon-retry"></span>
    Retry Connection
  }
</button>
```

## 🐛 Troubleshooting

### Health Check Always Fails

1. Verify `/health` endpoint is accessible
2. Check CORS configuration
3. Verify SSL certificate (if using HTTPS)
4. Check network tab in dev tools

### False Offline Detection

1. Check timeout settings (may be too aggressive)
2. Verify backend health check is fast (< 5s)
3. Look for network issues
4. Check for ad blockers

### No Notifications Shown

1. Verify ToastService is properly configured
2. Check browser console for errors
3. Ensure toast container element exists

## 📚 Related

- [Backend Health Check Documentation](../../../api/Avancira.Infrastructure/Health/README.md)
- [Toast Service Documentation](../../services/toast.service.ts)
- [HTTP Interceptors](../interceptors/)
