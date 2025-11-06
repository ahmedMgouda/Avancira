# Avancira Health Check System

Comprehensive health monitoring following industry best practices for microservices.

## 📋 Overview

The health check system provides three levels of monitoring:

1. **Public Health Endpoint** (`/health`) - Safe for external monitoring
2. **Liveness Probe** (`/health/live`) - For container orchestrators
3. **Readiness Probe** (`/health/ready`) - For load balancers
4. **Detailed Diagnostics** (`/health/detailed`) - For internal debugging

## 🔒 Security Best Practices

### Public Endpoint (`/health`)

✅ **Safe for public internet**
- Returns only: `status`, `timestamp`, `version`
- No internal details
- No error messages
- No dependency information
- No stack traces

**Response Example:**
```json
{
  "status": "healthy",
  "timestamp": "2024-01-15T10:30:00Z",
  "version": "1.0.0"
}
```

**Status Codes:**
- `200 OK` - healthy or degraded
- `503 Service Unavailable` - unhealthy

### Internal Endpoints

⚠️ **Should be protected in production** (use authentication or network policies)

#### `/health/live` - Liveness Probe
Checks if the service process is running.

```json
{
  "status": "Healthy",
  "timestamp": "2024-01-15T10:30:00Z"
}
```

#### `/health/ready` - Readiness Probe
Checks if the service can handle requests (includes dependencies).

```json
{
  "status": "Healthy",
  "timestamp": "2024-01-15T10:30:00Z",
  "checks": [
    {
      "name": "database",
      "status": "Degraded",
      "description": "High latency detected"
    }
  ]
}
```

#### `/health/detailed` - Full Diagnostics
Detailed health information for debugging.

```json
{
  "status": "Healthy",
  "timestamp": "2024-01-15T10:30:00Z",
  "totalDuration": 145.3,
  "checks": [
    {
      "name": "self",
      "status": "Healthy",
      "description": "Service is running",
      "duration": 0.1,
      "tags": ["live"],
      "data": {},
      "exception": null
    },
    {
      "name": "database",
      "status": "Healthy",
      "description": "Can connect to database",
      "duration": 42.5,
      "tags": ["ready", "db"],
      "data": { "connectionTime": "42ms" },
      "exception": null
    }
  ]
}
```

## 🚀 Usage

### Backend Configuration

**In `Program.cs`:**
```csharp
var builder = WebApplication.CreateBuilder(args);

// Add health checks
builder.Services.AddAvanciraHealthChecks(builder.Configuration, options =>
{
    options.CheckDatabase = true;
    options.CheckRedis = true;
    options.CheckMemory = true;
    options.MemoryThresholdMb = 256;
});

var app = builder.Build();

// Map health check endpoints
app.MapAvanciraHealthChecks();
```

**In `appsettings.json`:**
```json
{
  "HealthCheck": {
    "Redis": {
      "Enabled": true,
      "FailureStatus": "Degraded"
    },
    "Memory": {
      "Enabled": true,
      "ThresholdMb": 256
    },
    "Downstream": {
      "Api": {
        "Enabled": true,
        "Url": "https://api.example.com",
        "HealthPath": "health/ready",
        "Timeout": 5,
        "FailureStatus": "Unhealthy"
      }
    }
  }
}
```

### Frontend Integration

**In Angular Service:**
```typescript
import { inject } from '@angular/core';
import { NetworkStatusService } from '@app/core/network/network-status.service';

export class MyComponent {
  private network = inject(NetworkStatusService);
  
  readonly isOnline = this.network.isOnline;
  readonly backendStatus = this.network.backendStatus;
  
  async retryOperation() {
    const isOnline = await this.network.retry();
    if (isOnline) {
      // Retry failed operation
    }
  }
}
```

## 📊 Health Check Tags

- `live` - Basic liveness check (process running)
- `ready` - Readiness check (can handle requests)
- `db` - Database checks
- `cache` - Cache/Redis checks
- `downstream` - External service checks

## 🔧 Custom Health Checks

```csharp
public class CustomHealthCheck : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Your health check logic
            var isHealthy = await CheckSomethingAsync();
            
            return isHealthy
                ? HealthCheckResult.Healthy("Custom check passed")
                : HealthCheckResult.Unhealthy("Custom check failed");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy(
                "Custom check threw exception",
                ex);
        }
    }
}

// Register in DI
services.AddHealthChecks()
    .AddCheck<CustomHealthCheck>(
        "custom",
        failureStatus: HealthStatus.Degraded,
        tags: new[] { "ready", "custom" });
```

## 🐋 Kubernetes Configuration

```yaml
apiVersion: v1
kind: Pod
metadata:
  name: avancira-bff
spec:
  containers:
  - name: bff
    image: avancira-bff:latest
    livenessProbe:
      httpGet:
        path: /health/live
        port: 8080
      initialDelaySeconds: 10
      periodSeconds: 10
      timeoutSeconds: 5
      failureThreshold: 3
    
    readinessProbe:
      httpGet:
        path: /health/ready
        port: 8080
      initialDelaySeconds: 5
      periodSeconds: 5
      timeoutSeconds: 3
      failureThreshold: 2
```

## 🔍 Monitoring Tools Integration

### Prometheus
```yaml
- job_name: 'avancira-services'
  metrics_path: '/health'
  scrape_interval: 30s
  static_configs:
    - targets: ['bff:8080', 'api:8080']
```

### AWS Application Load Balancer
```json
{
  "HealthCheck": {
    "Path": "/health",
    "Protocol": "HTTPS",
    "Port": "443",
    "HealthyThresholdCount": 2,
    "UnhealthyThresholdCount": 3,
    "TimeoutSeconds": 5,
    "IntervalSeconds": 30,
    "Matcher": {
      "HttpCode": "200"
    }
  }
}
```

## 📈 Best Practices

1. ✅ **Use different endpoints for different purposes**
   - Public monitoring: `/health`
   - Container orchestration: `/health/live`, `/health/ready`
   - Debugging: `/health/detailed`

2. ✅ **Return appropriate status codes**
   - `200 OK` - Healthy or degraded
   - `503 Service Unavailable` - Unhealthy

3. ✅ **Keep health checks lightweight**
   - Should complete in < 1 second
   - Don't perform expensive operations
   - Cache results if needed

4. ✅ **Use proper tags**
   - `live` for basic process checks
   - `ready` for dependency checks

5. ✅ **Protect sensitive endpoints**
   - `/health/detailed` should require authentication
   - Use network policies in Kubernetes

6. ✅ **Monitor health check performance**
   - Track response times
   - Alert on slow checks
   - Set appropriate timeouts

## 🚨 Troubleshooting

### Health Check Always Returns Unhealthy

1. Check logs for exceptions
2. Verify all dependencies are accessible
3. Check timeouts configuration
4. Test individual health checks

### Slow Health Checks

1. Review database query performance
2. Check network latency to dependencies
3. Consider caching health check results
4. Reduce check frequency

### False Positives

1. Adjust failure thresholds
2. Use `Degraded` status for non-critical issues
3. Review health check logic
4. Consider implementing circuit breakers

## 📚 References

- [Microsoft Health Checks](https://docs.microsoft.com/en-us/aspnet/core/host-and-deploy/health-checks)
- [Kubernetes Liveness and Readiness Probes](https://kubernetes.io/docs/tasks/configure-pod-container/configure-liveness-readiness-startup-probes/)
- [OWASP API Security](https://owasp.org/www-project-api-security/)
