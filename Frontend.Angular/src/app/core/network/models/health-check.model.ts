import { RetryStrategy } from "../../http/services/resilience.service";

export interface HealthCheckResponse {
  /** Overall system status */
  status: 'healthy' | 'degraded' | 'unhealthy';
  
  /** UTC timestamp of the health check */
  timestamp: string;
  
  /** Version of the service (optional) */
  version?: string;
}

/**
 * Network connection quality levels
 */
export type ConnectionQuality = 'excellent' | 'good' | 'fair' | 'poor' | 'unknown';

/**
 * Network diagnostics information
 */
export interface NetworkDiagnostics {
  isOnline: boolean;
  isOffline: boolean;
  isVerifying: boolean;
  wasOffline: boolean;
  lastCheck: Date;
  latency: number | null;
  quality: ConnectionQuality;
  qualityMessage: string;
  statusIcon: string;
  navigatorOnLine: boolean;
  connectionType: string;
  activeWaitCount: number;
  retryStrategy: RetryStrategy;
  healthEndpoint: string;
  checkInterval: number;
  maxAttempts: number;
  bypassesInterceptors: boolean;
}
