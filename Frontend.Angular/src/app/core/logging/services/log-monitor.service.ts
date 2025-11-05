import { Injectable, signal, computed } from '@angular/core';
import { BaseLogEntry } from '../models/base-log-entry.model';

interface LogStats {
  totalLogs: number;
  totalErrors: number;
  totalHttp: number;
  errorRate: string;
}

@Injectable({ providedIn: 'root' })
export class LogMonitorService {
  private channel: BroadcastChannel | null = null;
  private readonly MAX_LOGS = 1000;
  
  readonly logs = signal<BaseLogEntry[]>([]);
  
  readonly stats = computed<LogStats>(() => {
    const allLogs = this.logs();
    const totalLogs = allLogs.length;
    const totalErrors = allLogs.filter(l => l.log.level === 'ERROR' || l.log.level === 'FATAL').length;
    const totalHttp = allLogs.filter(l => l.log.type === 'http').length;
    const errorRate = totalLogs > 0 
      ? `${((totalErrors / totalLogs) * 100).toFixed(1)}%`
      : '0%';
    
    return {
      totalLogs,
      totalErrors,
      totalHttp,
      errorRate
    };
  });
  
  constructor() {
    this.initializeBroadcastChannel();
  }
  
  private initializeBroadcastChannel(): void {
    try {
      this.channel = new BroadcastChannel('app-logs');
      
      this.channel.onmessage = (event) => {
        this.addLog(event.data);
      };
    } catch (error) {
      console.warn('[LogMonitor] BroadcastChannel not supported', error);
    }
  }
  
  broadcast(log: BaseLogEntry): void {
    this.addLog(log);
    
    if (this.channel) {
      try {
        this.channel.postMessage(log);
      } catch (error) {
        console.warn('[LogMonitor] Failed to broadcast log', error);
      }
    }
  }
  
  private addLog(log: BaseLogEntry): void {
    this.logs.update(logs => {
      const newLogs = [...logs, log];
      
      if (newLogs.length > this.MAX_LOGS) {
        return newLogs.slice(-this.MAX_LOGS);
      }
      
      return newLogs;
    });
  }
  
  clearLogs(): void {
    this.logs.set([]);
  }
  
  ngOnDestroy(): void {
    if (this.channel) {
      this.channel.close();
    }
  }
}
