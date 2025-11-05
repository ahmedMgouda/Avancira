export interface StandardError {
  errorId: string;
  userMessage: string;
  userTitle: string;
  severity: 'info' | 'warning' | 'error' | 'critical';
  code: string;
  timestamp: Date;
  originalError?: unknown;
}
