/**
 * Standard Error Model
 * Extended to support Symbol-based type discrimination
 */

// Symbol for type-safe error checking
export const STANDARD_ERROR_BRAND = Symbol.for('StandardError');

export interface StandardError {
  errorId: string;
  userMessage: string;
  userTitle: string;
  severity: 'info' | 'warning' | 'error' | 'critical';
  code: string;
  timestamp: Date;
  originalError?: unknown;
  // Symbol for type checking (optional - added at runtime)
  [STANDARD_ERROR_BRAND]?: true;
}
