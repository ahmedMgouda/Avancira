import { CommonModule } from '@angular/common';
import { AfterViewInit, Component } from '@angular/core';
import { FormsModule } from '@angular/forms';

import { ManageLessonsComponent } from '../../components/manage-lessons/manage-lessons.component';
import { MessageListComponent } from '../../components/message-list/message-list.component';
import { MessageThreadComponent } from '../../components/message-thread/message-thread.component';

import { Chat } from '../../models/chat';

@Component({
  selector: 'app-messages',
  imports: [CommonModule, FormsModule, MessageListComponent, MessageThreadComponent, ManageLessonsComponent],
  templateUrl: './messages.component.html',
  styleUrls: ['./messages.component.scss'],
})
export class MessagesComponent implements AfterViewInit {
  selectedContact: Chat | null = null;

  ngAfterViewInit() {
    const chatAppTarget = document.querySelector('.chat-window') as HTMLElement;

    if (window.innerWidth > 991) {
      chatAppTarget.classList.remove('chat-slide');
    }

    document.addEventListener('click', (event) => {
      const target = event.target as HTMLElement;
      if (target.closest('.chat-window .chat-users-list a.media')) {
        if (window.innerWidth <= 991) {
          chatAppTarget.classList.add('chat-slide');
        }
        event.preventDefault();
      }
      if (target.closest('#back_user_list')) {
        if (window.innerWidth <= 991) {
          chatAppTarget.classList.remove('chat-slide');
        }
        event.preventDefault();
      }
    });
  }
}
