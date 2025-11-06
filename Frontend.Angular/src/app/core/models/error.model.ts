// /**
//  * RFC 7807 - Problem Details for HTTP APIs
//  * Standard format for API errors from backend
//  */
// export interface ProblemDetails {
//   readonly type?: string;
//   readonly title?: string;
//   readonly status?: number;
//   readonly detail?: string;
//   readonly instance?: string;
//   readonly errors?: readonly string[] | ValidationErrors;
//   readonly extensions?: Record<string, unknown>;
// }

// export type ValidationErrors = Record<string, readonly string[]>;

// /**
//  * Unified application error structure
//  */
// export interface AppError {
//     message: string;
//     code?: string;
//     status?: number;
//     details?: unknown;
//     timestamp: Date;
//     correlationId?: string;
//     severity?: 'info' | 'warning' | 'error' | 'critical';
// }
