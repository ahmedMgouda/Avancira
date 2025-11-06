import { computed, Injectable, signal } from '@angular/core';
import { Observable, Subject } from 'rxjs';

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
  
  // Subject for observable pattern (backwards compatibility)
  private readonly logsSubject = new Subject<BaseLogEntry>();
  
  // Modern signal-based approach
  readonly logs = signal<BaseLogEntry[]>([]);
  
  // Observable for components that prefer subscription pattern
  readonly logs$: Observable<BaseLogEntry> = this.logsSubject.asObservable();
  
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
    
    // Emit to observable subscribers
    this.logsSubject.next(log);
    
    // Broadcast to other tabs
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
    this.logsSubject.complete();
  }
}