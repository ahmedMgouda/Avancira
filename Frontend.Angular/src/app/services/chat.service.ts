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

  getChatsLastMessage(): Observable<Message[]> {
    return this.getChats()
      .pipe(
        map((chats) =>
          chats.map((chat) => {
            const lastMessage = chat.messages[chat.messages.length - 1]; // Get the last message
            return {
              senderName: chat.name,
              content: lastMessage?.content || 'No messages yet', // Fallback if no messages
              timestamp: lastMessage?.timestamp || 'N/A', // Fallback if no timestamp
            } as Message;
          })
        )
      );
  }

  sendMessage(message: SendMessage): Observable<void> {
    return this.http.put<void>(`${this.apiUrl}/send`, message);
  }
}
