export enum LogLevel {
  TRACE = 0,
  DEBUG = 1,
  INFO = 2,
  WARN = 3,
  ERROR = 4,
  FATAL = 5
}

export type LogType = 
  | 'application'
  | 'http'
  | 'error'
  | 'navigation'
  | 'performance'
  | 'security';
