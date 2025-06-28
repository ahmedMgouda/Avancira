import { CommonModule } from '@angular/common';
import { AfterViewInit, ChangeDetectorRef, Component, ElementRef, EventEmitter, HostListener, Input, OnChanges, OnDestroy, OnInit, Output, ViewChild } from '@angular/core';
import { FormsModule } from '@angular/forms';

import { ManageLessonsComponent } from '../manage-lessons/manage-lessons.component';

import { ChatService } from '../../services/chat.service';
import { NotificationService } from '../../services/notification.service';

import { TimeAgoPipe } from "../../pipes/time-ago.pipe";

import { environment } from '../../environments/environment';
import { Chat } from '../../models/chat';

@Component({
  selector: 'app-message-thread',
  imports: [CommonModule, FormsModule, TimeAgoPipe, ManageLessonsComponent],
  templateUrl: './message-thread.component.html',
  styleUrls: ['./message-thread.component.scss']
})
export class MessageThreadComponent implements AfterViewInit, OnInit, OnChanges, OnDestroy {
  @Input() selectedContact: Chat | null = null;
  @Output() messageSent = new EventEmitter<void>();
  @ViewChild('chatMessages', { static: false }) chatMessagesContainer!: ElementRef;
  @ViewChild('slider') slider: ElementRef | undefined;
  newMessage: string = '';
  messageSuccess: boolean = false;
  private pollingId: any;


  constructor(
    private chatService: ChatService,
    private notificationService: NotificationService,
    private cdr: ChangeDetectorRef
  ) { }

  ngOnInit(): void {
    // Listen for notifications
    this.notificationService.onReceiveNotification((notification) => {
      if (this.selectedContact && notification.data.listingId === this.selectedContact.listingId && notification.data.senderId === this.selectedContact.recipientId) {
        this.selectedContact.messages.push({
          sentBy: 'contact',
          senderId: '',
          senderName: '',
          content: notification.data.content,
          timestamp: notification.data.timestamp,
        });

        // Scroll to the bottom to display the new message
        this.scrollToBottom();
      }
    });
    if (!environment.useSignalR) {
      this.startPolling();
    } else {
      setTimeout(() => {
        if (!this.notificationService.isConnected()) {
          this.startPolling();
        }
      }, 3000);
    }
  }

  ngAfterViewInit(): void {
    this.scrollToBottom();
  }


  ngOnChanges(): void {
    if (this.selectedContact) {
      this.scrollToBottom();
      if (this.pollingId) {
        this.startPolling();
      }
    }
  }

  sendMessage(): void {
    if (this.newMessage.trim() && this.selectedContact) {
      this.selectedContact?.messages.push({
        sentBy: 'me',
        senderId: '',
        senderName: '',
        content: this.newMessage,
        timestamp: new Date(),
      });
      this.scrollToBottom();
      this.messageSent.emit();
      var toBeSent = this.newMessage;
      this.newMessage = '';

      this.chatService.sendMessage({
        listingId: this.selectedContact.listingId,
        recipientId: this.selectedContact.recipientId,
        content: toBeSent,
      }).subscribe({
        next: () => {
          this.messageSuccess = true;
          setTimeout(() => (this.messageSuccess = false), 3000);
        },
        error: (err) => {
          console.error('Failed to send message:', err);
        },
      });
    }
  }

  toggleSlider(event: MouseEvent) {
    event.stopPropagation(); // Prevent the document click listener from firing
    const sliderElement = this.slider?.nativeElement as HTMLElement;
    if (window.innerWidth <= 991) {
      sliderElement.classList.toggle('show-slider');
    } else {
      sliderElement.classList.toggle('show-slider-desktop');
    }
  }

  @HostListener('document:click', ['$event'])
  onDocumentClick(event: MouseEvent) {
    const sliderElement = this.slider?.nativeElement as HTMLElement;
    if (sliderElement && !sliderElement.contains(event.target as Node) && sliderElement.classList.contains('show-slider')) {
      sliderElement.classList.remove('show-slider');
    }
  }

  private scrollToBottom(): void {
    this.cdr.detectChanges();
    if (this.chatMessagesContainer) {
      const element = this.chatMessagesContainer.nativeElement;
      element.scrollTop = element.scrollHeight;
    }
  }

  private startPolling(): void {
    this.stopPolling();
    this.pollingId = setInterval(() => this.refreshChat(), 5000);
  }

  private stopPolling(): void {
    if (this.pollingId) {
      clearInterval(this.pollingId);
      this.pollingId = null;
    }
  }

  private refreshChat(): void {
    if (this.selectedContact) {
      this.chatService.getChat(this.selectedContact.id).subscribe({
        next: (chat) => {
          this.selectedContact!.messages = chat.messages;
          this.scrollToBottom();
        },
        error: (err) => console.error('Failed to refresh chat:', err),
      });
    }
  }

  ngOnDestroy(): void {
    this.stopPolling();
  }
}
