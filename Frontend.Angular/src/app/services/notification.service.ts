import { Injectable } from '@angular/core';
import * as signalR from '@microsoft/signalr';

import { environment } from '../environments/environment';
import { Notification } from '../models/notification';

@Injectable({
  providedIn: 'root'
})
export class NotificationService {
  private hubConnection: signalR.HubConnection | null = null;

  // Start SignalR connection with token
  startConnection(token: string): void {
    if (!environment.useSignalR) {
      console.warn('SignalR connection disabled via environment.');
      return;
    }
    if (this.hubConnection) {
      console.warn('SignalR connection is already started.');
      return;
    }

    this.hubConnection = new signalR.HubConnectionBuilder()
      .withUrl(`${environment.bffBaseUrl}/notification`, {
        accessTokenFactory: () => token,
      })
      .build();

    this.hubConnection
      .start()
      .then(() => console.log('SignalR Connected'))
      .catch((err) => console.error('Error connecting to SignalR:', err));
  }

  // Subscribe to notifications
  onReceiveNotification(callback: (notification: Notification) => void): void {
    this.hubConnection?.on('ReceiveNotification', callback);
  }

  // Stop the connection (optional, for cleanup)
  stopConnection(): void {
    this.hubConnection?.stop().then(() => console.log('SignalR Disconnected'));
  }

  isConnected(): boolean {
    return this.hubConnection?.state === signalR.HubConnectionState.Connected;
  }
}
