import { CommonModule } from '@angular/common';
import { Component, OnInit } from '@angular/core';

import { SessionService } from '../../services/session.service';

import { DeviceSessions } from '../../models/device-sessions';
import { UserSession } from '../../models/session';

@Component({
  selector: 'app-sessions',
  imports: [CommonModule],
  templateUrl: './sessions.component.html',
  styleUrl: './sessions.component.scss'
})
export class SessionsComponent implements OnInit {
  deviceSessions: DeviceSessions[] = [];
  loading = false;

  selectedSessions = new Set<string>();

  constructor(private sessionService: SessionService) {}

  ngOnInit(): void {
    this.loadSessions();
  }

  loadSessions(): void {
    this.loading = true;
    this.sessionService.getSessions().subscribe({
      next: sessions => {
        this.deviceSessions = sessions;
        this.selectedSessions.clear();
        this.loading = false;
      },
      error: () => {
        this.loading = false;
      }
    });
  }

  onSelectSession(id: string, checked: boolean): void {
    if (checked) {
      this.selectedSessions.add(id);
    } else {
      this.selectedSessions.delete(id);
    }
  }

  isSessionSelected(id: string): boolean {
    return this.selectedSessions.has(id);
  }

  isDeviceFullySelected(device: DeviceSessions): boolean {
    return device.sessions.every(session => this.selectedSessions.has(session.id));
  }

  isDevicePartiallySelected(device: DeviceSessions): boolean {
    const selectedCount = device.sessions.filter(session => this.selectedSessions.has(session.id)).length;
    return selectedCount > 0 && selectedCount < device.sessions.length;
  }

  toggleDeviceSelection(device: DeviceSessions, checked: boolean): void {
    device.sessions.forEach(session => {
      if (checked) {
        this.selectedSessions.add(session.id);
      } else {
        this.selectedSessions.delete(session.id);
      }
    });
  }

  revokeSelected(): void {
    const ids = Array.from(this.selectedSessions);
    if (ids.length === 0) {
      return;
    }

    this.sessionService.revokeSessions(ids).subscribe({
      next: () => {
        this.selectedSessions.clear();
        this.loadSessions();
      }
    });
  }

  revoke(session: UserSession): void {
    this.sessionService.revokeSession(session.id).subscribe({
      next: () => {
        this.selectedSessions.delete(session.id);
        this.loadSessions();
      }
    });
  }

  formatDate(value?: string | null): string {
    return value ? new Date(value).toLocaleString() : '';
  }

  trackByDevice = (_index: number, device: DeviceSessions) => device.deviceId;

  trackBySession = (_index: number, session: UserSession) => session.id;
}
