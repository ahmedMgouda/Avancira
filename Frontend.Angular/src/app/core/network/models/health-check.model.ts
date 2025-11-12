export interface HealthCheckResponse {
  /** Overall system status */
  status: 'healthy' | 'degraded' | 'unhealthy';

  /** UTC timestamp of the health check */
  timestamp: string;

  /** Version of the service (optional) */
  version?: string;
}
