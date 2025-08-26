import { CommonModule } from '@angular/common';
import { Component, OnInit, TemplateRef, ViewChild } from '@angular/core';

import { TableComponent } from '../../layout/shared/table/table.component';
import { Session } from '../../models/session';
import { SessionService } from '../../services/session.service';

@Component({
  selector: 'app-sessions',
  imports: [CommonModule, TableComponent],
  templateUrl: './sessions.component.html',
  styleUrl: './sessions.component.scss'
})
export class SessionsComponent implements OnInit {
  sessions: Session[] = [];
  pagedSessions: Session[] = [];
  loading = false;

  page = 1;
  pageSize = 10;
  pageSizeOptions = [5, 10, 50, 100];
  totalResults = 0;
  @ViewChild('selectCell', { static: true }) selectCell!: TemplateRef<any>;

  sessionColumns: any[] = [];

  selectedSessions = new Set<string>();

  sessionActions = [
    {
      label: 'Revoke',
      icon: 'fa-times',
      class: 'btn-sm btn-outline-danger',
      callback: (session: Session) => this.revoke(session)
    }
  ];

  constructor(private sessionService: SessionService) {}

  ngOnInit(): void {
    this.sessionColumns = [
      { key: 'select', label: '', cellTemplate: this.selectCell },
      { key: 'device', label: 'Device' },
      { key: 'operatingSystem', label: 'OS' },
      { key: 'userAgent', label: 'Agent' },
      { key: 'ipAddress', label: 'IP Address' },
      { key: 'country', label: 'Country' },
      { key: 'city', label: 'City' },
      {
        key: 'lastActivityUtc',
        label: 'Last Active',
        formatter: (value: any) => value ? new Date(value).toLocaleString() : ''
      }
    ];
    this.loadSessions();
  }

  loadSessions(): void {
    this.loading = true;
    this.sessionService.getSessions().subscribe({
      next: s => {
        this.sessions = s;
        this.totalResults = s.length;
        this.updatePagedSessions();
        this.selectedSessions.clear();
        this.loading = false;
      },
      error: () => {
        this.loading = false;
      }
    });
  }

  updatePagedSessions(): void {
    const start = (this.page - 1) * this.pageSize;
    this.pagedSessions = this.sessions.slice(start, start + this.pageSize);
  }

  onPageChange(newPage: number): void {
    this.page = newPage;
    this.updatePagedSessions();
  }

  onPageSizeChange(newSize: number): void {
    this.pageSize = newSize;
    this.page = 1;
    this.updatePagedSessions();
  }

  onSelectSession(id: string, checked: boolean): void {
    if (checked) {
      this.selectedSessions.add(id);
    } else {
      this.selectedSessions.delete(id);
    }
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

  revoke(session: Session): void {
    this.sessionService.revokeSession(session.id).subscribe({
      next: () => {
        this.selectedSessions.delete(session.id);
        this.loadSessions();
      }
    });
  }
}
