import { CommonModule } from '@angular/common';
import { Component, OnInit } from '@angular/core';

import { Session } from '../../models/session';
import { SessionService } from '../../services/session.service';

@Component({
  selector: 'app-sessions',
  imports: [CommonModule],
  templateUrl: './sessions.component.html',
  styleUrl: './sessions.component.scss'
})
export class SessionsComponent implements OnInit {
  sessions: Session[] = [];
  loading = false;

  constructor(private sessionService: SessionService) {}

  ngOnInit(): void {
    this.loadSessions();
  }

  loadSessions(): void {
    this.loading = true;
    this.sessionService.getSessions().subscribe({
      next: s => {
        this.sessions = s;
        this.loading = false;
      },
      error: () => {
        this.loading = false;
      }
    });
  }

  revoke(session: Session): void {
    this.sessionService.revokeSession(session.id).subscribe({
      next: () => {
        this.sessions = this.sessions.filter(x => x.id !== session.id);
      }
    });
  }
}
