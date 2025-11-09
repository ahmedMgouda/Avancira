export class SourceExtractor {
  static extract(skipFrames: number = 3): string {
    try {
      const error = new Error();
      
      if (!error.stack) {
        return 'Application';
      }
      
      const stackLines = error.stack.split('\n');
      
      if (stackLines.length <= skipFrames) {
        return 'Application';
      }
      
      const targetLine = stackLines[skipFrames];
      
      const angularMatch = targetLine.match(/at\s+(\w+)\.(\w+)\s+\(/);
      if (angularMatch) {
        return `${angularMatch[1]}.${angularMatch[2]}`;
      }
      
      const functionMatch = targetLine.match(/at\s+(\w+)\s+\(/);
      if (functionMatch) {
        return functionMatch[1];
      }
      
      const anonymousMatch = targetLine.match(/at\s+.*?([A-Z]\w+)\.(\w+)/);
      if (anonymousMatch) {
        return `${anonymousMatch[1]}.${anonymousMatch[2]}`;
      }
      
      return 'Application';
    } catch {
      return 'Application';
    }
  }
  
  static extractFromError(error: Error): string {
    try {
      if (!error.stack) {
        return 'System';
      }
      
      const stackLines = error.stack.split('\n');
      
      if (stackLines.length < 2) {
        return 'System';
      }
      
      const targetLine = stackLines[1];
      
      const angularMatch = targetLine.match(/at\s+(\w+)\.(\w+)\s+\(/);
      if (angularMatch) {
        return `${angularMatch[1]}.${angularMatch[2]}`;
      }
      
      const functionMatch = targetLine.match(/at\s+(\w+)\s+\(/);
      if (functionMatch) {
        return functionMatch[1];
      }
      
      const anonymousMatch = targetLine.match(/at\s+.*?([A-Z]\w+)\.(\w+)/);
      if (anonymousMatch) {
        return `${anonymousMatch[1]}.${anonymousMatch[2]}`;
      }
      
      return 'System';
    } catch {
      return 'System';
    }
  }
}
