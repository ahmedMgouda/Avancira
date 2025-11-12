import { CommonModule } from '@angular/common';
import { Component, computed, inject, signal } from '@angular/core';
import { BaseLogEntry } from '@core/logging/models/base-log-entry.model';

import { LogMonitorService } from '@core/logging/services/log-monitor.service';

interface MetricStats {
  totalLogs: number;
  totalErrors: number;
  totalWarnings: number;
  httpRequests: number;
  slowRequests: number;
  errorRate: string;
}

@Component({
  selector: 'app-dev-monitor',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './dev-monitor.component.html',
  styleUrls: ['./dev-monitor.component.scss']
})
export class DevMonitorComponent {
  private readonly logMonitor = inject(LogMonitorService);
  
  // ────────────────────────────────────────────────────────────────
  // STATE SIGNALS
  // ────────────────────────────────────────────────────────────────
  readonly maxLogs = signal(200);
  readonly isPaused = signal(false);
  readonly selectedLog = signal<BaseLogEntry | null>(null);
  
  // Use the LogMonitor's signal directly (no subscription needed!)
  private readonly allLogs = this.logMonitor.logs;
  readonly logs = computed(() => this.allLogs().slice(-this.maxLogs()));
  
  // ────────────────────────────────────────────────────────────────
  // FILTERS
  // ────────────────────────────────────────────────────────────────
  readonly levelFilter = signal<string>('all');
  readonly typeFilter = signal<string>('all');
  readonly statusFilter = signal<number | 'all'>('all');
  readonly searchTerm = signal('');
  
  // Extract available types and status codes
  readonly availableTypes = computed(() => {
    const types = new Set<string>();
    for (const log of this.allLogs()) {
      if (log.log?.type) {
        types.add(log.log.type);
      }
    }
    return Array.from(types).sort();
  });
  
  readonly availableStatuses = computed(() => {
    const statuses = new Set<number>();
    for (const log of this.allLogs()) {
      if (log.http?.status_code) {
        statuses.add(log.http.status_code);
      }
    }
    return Array.from(statuses).sort((a, b) => a - b);
  });
  
  readonly availableLevels = computed(() => {
    const levels = new Set<string>();
    for (const log of this.allLogs()) {
      if (log.log?.level) {
        levels.add(log.log.level);
      }
    }
    return Array.from(levels).sort();
  });
  
  // ────────────────────────────────────────────────────────────────
  // FILTERED LOGS
  // ────────────────────────────────────────────────────────────────
  readonly filteredLogs = computed(() => {
    const term = this.searchTerm().toLowerCase().trim();
    const level = this.levelFilter();
    const type = this.typeFilter();
    const status = this.statusFilter();
    let list = this.logs();
    
    if (level !== 'all') {
      list = list.filter(l => l.log?.level?.toLowerCase() === level.toLowerCase());
    }
    
    if (type !== 'all') {
      list = list.filter(l => l.log?.type === type);
    }
    
    if (status !== 'all') {
      list = list.filter(l => l.http?.status_code === status);
    }
    
    if (term) {
      const deepMatch = (obj: any): boolean => {
        if (obj == null) return false;
        if (['string', 'number', 'boolean'].includes(typeof obj)) {
          return String(obj).toLowerCase().includes(term);
        }
        if (typeof obj === 'object') {
          return Object.values(obj).some(v => deepMatch(v));
        }
        return false;
      };
      
      list = list.filter(l => deepMatch(l));
    }
    
    return list;
  });
  
  // ────────────────────────────────────────────────────────────────
  // METRICS
  // ────────────────────────────────────────────────────────────────
  readonly metrics = computed<MetricStats>(() => {
    const logs = this.allLogs();
    const errors = logs.filter(l => l.log?.level === 'ERROR' || l.log?.level === 'FATAL');
    const warnings = logs.filter(l => l.log?.level === 'WARN');
    const httpLogs = logs.filter(l => l.log?.type === 'http');
    const slowRequests = logs.filter(l => 
      l.http?.duration_ms && l.http.duration_ms > 3000
    );
    
    return {
      totalLogs: logs.length,
      totalErrors: errors.length,
      totalWarnings: warnings.length,
      httpRequests: httpLogs.length,
      slowRequests: slowRequests.length,
      errorRate: logs.length > 0 
        ? ((errors.length / logs.length) * 100).toFixed(1) + '%'
        : '0%'
    };
  });
  
  readonly logLevelCounts = computed(() => {
    const logs = this.allLogs();
    const counts: Record<string, number> = {};

    for (const log of logs) {
      const level = log.log?.level || 'UNKNOWN';
      counts[level] = (counts[level] || 0) + 1;
    }

    return counts;
  });

  readonly levelCards = computed(() => {
    const counts = this.logLevelCounts();

    const levelDefinitions: Array<{ level: string; label: string; description: string }> = [
      { level: 'FATAL', label: 'Fatal', description: 'Critical failures' },
      { level: 'ERROR', label: 'Errors', description: 'Runtime issues' },
      { level: 'WARN', label: 'Warnings', description: 'Potential problems' },
      { level: 'INFO', label: 'Info', description: 'General updates' },
      { level: 'DEBUG', label: 'Debug', description: 'Diagnostic details' },
      { level: 'TRACE', label: 'Trace', description: 'Verbose traces' }
    ];

    return levelDefinitions.map(def => ({
      ...def,
      count: counts[def.level] ?? 0
    }));
  });
  
  readonly hasLogs = computed(() => this.logs().length > 0);
  readonly hasFilteredLogs = computed(() => this.filteredLogs().length > 0);
  readonly isFiltering = computed(() =>
    this.levelFilter() !== 'all' ||
    this.typeFilter() !== 'all' ||
    this.statusFilter() !== 'all' ||
    !!this.searchTerm().trim()
  );
  
  // ────────────────────────────────────────────────────────────────
  // INTERACTIONS
  // ────────────────────────────────────────────────────────────────
  
  togglePause(): void {
    this.isPaused.update(v => !v);
  }
  
  clearLogs(): void {
    if (confirm('Clear all logs?')) {
      this.logMonitor.clearLogs();
      this.selectedLog.set(null);
    }
  }
  
  exportLogs(): void {
    const blob = new Blob([JSON.stringify(this.filteredLogs(), null, 2)], {
      type: 'application/json'
    });
    const url = URL.createObjectURL(blob);
    const link = document.createElement('a');
    link.href = url;
    link.download = `logs-${new Date().toISOString()}.json`;
    link.click();
    URL.revokeObjectURL(url);
  }
  
  selectLog(log: BaseLogEntry): void {
    this.selectedLog.set(log);
  }
  
  closeLogDetails(): void {
    this.selectedLog.set(null);
  }
  
  onSearchInput(event: Event): void {
    const value = (event.target as HTMLInputElement).value;
    this.searchTerm.set(value);
  }
  
  onLevelFilterChange(event: Event): void {
    const value = (event.target as HTMLSelectElement).value;
    this.levelFilter.set(value);
  }
  
  onTypeFilterChange(event: Event): void {
    const value = (event.target as HTMLSelectElement).value;
    this.typeFilter.set(value);
  }
  
  onStatusFilterChange(event: Event): void {
    const value = (event.target as HTMLSelectElement).value;
    this.statusFilter.set(value === 'all' ? 'all' : +value);
  }

  onLevelCardClick(level: string): void {
    this.levelFilter.update(current => (current === level ? 'all' : level));
  }

  clearFilters(): void {
    this.searchTerm.set('');
    this.levelFilter.set('all');
    this.typeFilter.set('all');
    this.statusFilter.set('all');
  }
  
  onMaxLogsChange(event: Event): void {
    const value = (event.target as HTMLInputElement).value;
    const num = +value;
    if (num > 0 && num <= 1000) this.maxLogs.set(num);
  }
  
  // ────────────────────────────────────────────────────────────────
  // HELPERS
  // ────────────────────────────────────────────────────────────────
  
  copyLogToClipboard(log: BaseLogEntry): void {
    navigator.clipboard
      .writeText(JSON.stringify(log, null, 2))
      .then(() => alert('Copied to clipboard'))
      .catch(() => alert('Failed to copy'));
  }
  
  trackByLog = (_: number, log: BaseLogEntry) => log.log?.id || log['@timestamp'];
  
  // ────────────────────────────────────────────────────────────────
  // LOG LEVEL HELPERS
  // ────────────────────────────────────────────────────────────────
  
  getLevelIcon(level: string): string {
    const icons: Record<string, string> = {
      'TRACE': '🔍',
      'DEBUG': '🐛',
      'INFO': 'ℹ️',
      'WARN': '⚠️',
      'ERROR': '❌',
      'FATAL': '💀'
    };
    return icons[level] ?? '📝';
  }
  
  getLevelClass(level: string): string {
    return `level-${level.toLowerCase()}`;
  }
  
  getTypeIcon(type: string): string {
    const icons: Record<string, string> = {
      'http': '🌐',
      'http_error': '🚫',
      'error': '⚠️',
      'application': '📱',
      'system': '⚙️',
      'navigation': '🧭'
    };
    return icons[type] ?? '📄';
  }
  
  getSpanIdDisplay(span: BaseLogEntry['trace']['span']): string {
    if (!span?.id) return 'N/A';
    return span.id.length >= 8 ? span.id.substring(0, 8) + '...' : span.id;
  }
  
  // ────────────────────────────────────────────────────────────────
  // FORMAT HELPERS
  // ────────────────────────────────────────────────────────────────
  
  formatTimestamp(timestamp: string): string {
    const d = new Date(timestamp);
    return d.toLocaleTimeString('en-US', {
      hour12: false,
      hour: '2-digit',
      minute: '2-digit',
      second: '2-digit',
      fractionalSecondDigits: 3
    });
  }
  
  formatDuration(ms: number | undefined): string {
    if (!ms) return '—';
    if (ms < 1000) return `${ms}ms`;
    return `${(ms / 1000).toFixed(2)}s`;
  }
  
  formatJSON(obj: any): string {
    try {
      return obj ? JSON.stringify(obj, null, 2) : '—';
    } catch {
      return String(obj);
    }
  }
  
  truncateMessage(msg: string, max = 100): string {
    return msg.length <= max ? msg : msg.slice(0, max) + '…';
  }
  
  getStatusClass(status: number | undefined): string {
    if (!status) return '';
    if (status >= 200 && status < 300) return 'status-success';
    if (status >= 300 && status < 400) return 'status-redirect';
    if (status >= 400 && status < 500) return 'status-client-error';
    if (status >= 500) return 'status-server-error';
    return '';
  }
}