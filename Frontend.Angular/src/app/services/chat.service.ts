import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { map, Observable } from 'rxjs';

import { environment } from '../environments/environment';
import { Chat, Message } from '../models/chat';
import { SendMessage } from '../models/send-message';

@Injectable({
  providedIn: 'root',
})
export class ChatService {
  private apiUrl = `${environment.apiUrl}/chats`;

  constructor(private http: HttpClient) { }

  getChats(): Observable<Chat[]> {
    return this.http.get<Chat[]>(this.apiUrl);
  }

  getChat(id: string): Observable<Chat> {
    return this.http.get<Chat>(`${this.apiUrl}/${id}`);
  }

  getChatsLastMessage(): Observable<Message[]> {
    return this.getChats()
      .pipe(
        map((chats) =>
            chats.map((chat) => ({
            senderName: chat.name,
            content: chat.lastMessage || 'No messages yet',
            timestamp: chat.timestamp || 'N/A',
          } as Message))
        )
      );
  }

  sendMessage(message: SendMessage): Observable<void> {
    return this.http.put<void>(`${this.apiUrl}/send`, message);
  }
}
