import { CommonModule } from '@angular/common';
import { Component, computed, DestroyRef, inject, signal } from '@angular/core';

import { LogEntry, LoggerService, LogLevel } from '@core/services/logger.service';

interface MetricStats {
  totalRequests: number;
  totalErrors: number;
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
  private readonly logger = inject(LoggerService);
  private readonly destroyRef = inject(DestroyRef);

  // ────────────────────────────────────────────────────────────────
  // STATE SIGNALS
  // ────────────────────────────────────────────────────────────────
  readonly maxLogs = signal(200);
  readonly isPaused = signal(false);
  readonly selectedLog = signal<LogEntry | null>(null);

  readonly allLogs = this.logger.logs;
  readonly logs = computed(() => this.allLogs().slice(-this.maxLogs()));

  // ────────────────────────────────────────────────────────────────
  // FILTERS
  // ────────────────────────────────────────────────────────────────
  readonly levelFilter = signal<LogLevel | 'all'>('all');
  readonly methodFilter = signal<string | 'all'>('all');
  readonly statusFilter = signal<number | 'all'>('all');
  readonly searchTerm = signal('');

  // Extract available HTTP methods/status codes dynamically from logs
  readonly availableMethods = computed(() => {
    const methods = new Set<string>();
    for (const log of this.allLogs()) {
      const method = log.context?.method;
      if (typeof method === 'string' && method.trim()) {
        methods.add(method.toUpperCase());
      }
    }
    return Array.from(methods).sort();
  });

  readonly availableStatuses = computed(() => {
    const statuses = new Set<number>();
    for (const log of this.allLogs()) {
      const status = log.context?.status;
      if (typeof status === 'number') {
        statuses.add(status);
      }
    }
    return Array.from(statuses).sort((a, b) => a - b);
  });

  // ────────────────────────────────────────────────────────────────
  // FILTERED LOGS (deep search + filters)
  // ────────────────────────────────────────────────────────────────
  readonly filteredLogs = computed(() => {
    const term = this.searchTerm().toLowerCase().trim();
    const level = this.levelFilter();
    const method = this.methodFilter();
    const status = this.statusFilter();
    let list = this.logs();

    if (level !== 'all') list = list.filter(l => l.level === level);
    if (method !== 'all') list = list.filter(l => l.context?.method?.toUpperCase() === method);
    if (status !== 'all') list = list.filter(l => l.context?.status === status);

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

      list = list.filter(l =>
        deepMatch(l.message) ||
        deepMatch(l.correlationId) ||
        deepMatch(l.url) ||
        deepMatch(l.context)
      );
    }

    return list;
  });

  // ────────────────────────────────────────────────────────────────
  // METRICS
  // ────────────────────────────────────────────────────────────────
  readonly metrics = computed<MetricStats>(() => this.logger.stats());
  readonly logLevels = computed(() => [
    { value: 'all', label: 'All', count: this.logs().length },
    ...Object.values(LogLevel)
      .filter(v => typeof v === 'number' && v !== LogLevel.None)
      .map(v => ({
        value: v as LogLevel,
        label: this.formatLevel(v as LogLevel),
        count: this.logs().filter(l => l.level === v).length
      }))
  ]);

  readonly hasLogs = computed(() => this.logs().length > 0);
  readonly hasFilteredLogs = computed(() => this.filteredLogs().length > 0);
  readonly isFiltering = computed(
    () =>
      this.levelFilter() !== 'all' ||
      this.methodFilter() !== 'all' ||
      this.statusFilter() !== 'all' ||
      !!this.searchTerm().trim()
  );

  // ────────────────────────────────────────────────────────────────
  // CLOCK SIGNAL
  // ────────────────────────────────────────────────────────────────
  readonly lastUpdated = signal(Date.now());
  private readonly clockInterval = setInterval(() => {
    if (!this.isPaused()) this.lastUpdated.set(Date.now());
  }, 1000);

  // ────────────────────────────────────────────────────────────────
  // INTERACTIONS
  // ────────────────────────────────────────────────────────────────
  togglePause(): void {
    this.isPaused.update(v => !v);
  }

  clearLogs(): void {
    if (confirm('Clear all logs?')) {
      this.logger.clearLogs();
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

  selectLog(log: LogEntry): void {
    this.selectedLog.set(log);
  }

  closeLogDetails(): void {
    this.selectedLog.set(null);
  }

  onSearchInput(value: string): void {
    this.searchTerm.set(value);
  }

  onLevelFilterChange(value: string): void {
    this.levelFilter.set(value === 'all' ? 'all' : (+value as LogLevel));
  }

  onMethodFilterChange(value: string): void {
    this.methodFilter.set(value === 'all' ? 'all' : value);
  }

  onStatusFilterChange(value: string): void {
    const num = +value;
    this.statusFilter.set(value === 'all' ? 'all' : num);
  }

  clearFilters(): void {
    this.searchTerm.set('');
    this.levelFilter.set('all');
    this.methodFilter.set('all');
    this.statusFilter.set('all');
  }

  onMaxLogsChange(value: string): void {
    const num = +value;
    if (num > 0 && num <= 1000) this.maxLogs.set(num);
  }

  // ────────────────────────────────────────────────────────────────
  // HELPERS
  // ────────────────────────────────────────────────────────────────
  copyLogToClipboard(log: LogEntry): void {
    navigator.clipboard
      .writeText(JSON.stringify(log, null, 2))
      .then(() => alert('Copied to clipboard'))
      .catch(() => alert('Failed to copy'));
  }

  trackByLog = (_: number, log: LogEntry) => log.id;

  // ────────────────────────────────────────────────────────────────
  // LOG LEVEL HELPERS
  // ────────────────────────────────────────────────────────────────
  getLevelIcon(level: LogLevel): string {
    const icons: Record<LogLevel, string> = {
      [LogLevel.Trace]: '🔍',
      [LogLevel.Debug]: '🐛',
      [LogLevel.Info]: 'ℹ️',
      [LogLevel.Warn]: '⚠️',
      [LogLevel.Error]: '❌',
      [LogLevel.Fatal]: '💀',
      [LogLevel.None]: '📝'
    };
    return icons[level] ?? '📝';
  }

  getLevelClass(level: LogLevel): string {
    return {
      [LogLevel.Trace]: 'level-trace',
      [LogLevel.Debug]: 'level-debug',
      [LogLevel.Info]: 'level-info',
      [LogLevel.Warn]: 'level-warn',
      [LogLevel.Error]: 'level-error',
      [LogLevel.Fatal]: 'level-fatal',
      [LogLevel.None]: 'level-none'
    }[level] ?? '';
  }

  countByLevel(level: LogLevel): number {
    return this.logs().filter(l => l.level === level).length;
  }

  setLevelFilter(level: LogLevel | 'all'): void {
    this.levelFilter.set(level);
  }

  // ────────────────────────────────────────────────────────────────
  // FORMAT HELPERS
  // ────────────────────────────────────────────────────────────────
  formatLevel(level: LogLevel): string {
    return {
      [LogLevel.Trace]: 'Trace',
      [LogLevel.Debug]: 'Debug',
      [LogLevel.Info]: 'Info',
      [LogLevel.Warn]: 'Warn',
      [LogLevel.Error]: 'Error',
      [LogLevel.Fatal]: 'Fatal',
      [LogLevel.None]: 'None'
    }[level] ?? 'Unknown';
  }

  formatTimestamp(timestamp: string | number): string {
    const d = new Date(timestamp);
    return d.toLocaleTimeString('en-US', {
      hour12: false,
      hour: '2-digit',
      minute: '2-digit',
      second: '2-digit',
      fractionalSecondDigits: 3
    });
  }

  formatContext(ctx: any): string {
    try {
      return ctx ? JSON.stringify(ctx, null, 2) : '—';
    } catch {
      return String(ctx);
    }
  }

  truncateMessage(msg: string, max = 100): string {
    return msg.length <= max ? msg : msg.slice(0, max) + '…';
  }
}
