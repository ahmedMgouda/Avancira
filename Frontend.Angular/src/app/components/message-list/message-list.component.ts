import { CommonModule } from '@angular/common';
import { Component, EventEmitter, Input, OnDestroy, OnInit, Output } from '@angular/core';
import { FormsModule } from '@angular/forms';

import { AuthService } from '../../services/auth.service';
import { ChatService } from '../../services/chat.service';
import { NotificationService } from '../../services/notification.service';
import { UserService } from '../../services/user.service';

import { TimeAgoPipe } from "../../pipes/time-ago.pipe";

import { ImageFallbackDirective } from '../../directives/image-fallback.directive';

import { Chat } from '../../models/chat';
import { User } from '../../models/user';
import { NotificationEvent } from '../../models/enums/notification-event';

@Component({
  selector: 'app-message-list',
  imports: [CommonModule, FormsModule, ImageFallbackDirective, TimeAgoPipe],
  templateUrl: './message-list.component.html',
  styleUrls: ['./message-list.component.scss']
})
export class MessageListComponent implements OnInit, OnDestroy {
  loading = true;
  contacts: Chat[] = [];
  @Input() selectedContact: Chat | null = null;
  @Output() selectedContactChange = new EventEmitter<Chat>();
  user!: User;

  constructor(
    private userService: UserService,
    private authService: AuthService,
    private chatService: ChatService,
    private notificationService: NotificationService
  ) { }

  ngOnInit(): void {
    if (this.isLoggedIn()) {
      this.fetchUserInfo();
    }
    this.loadContacts();
    
    // Listen for new message notifications to refresh the contact list
    this.notificationService.onReceiveNotification((notification) => {
      if (notification.eventName === NotificationEvent.NewMessage) {
        // Refresh the contacts list to show updated last message and timestamp
        this.loadContacts();
      }
    });
  }

  ngOnDestroy(): void {
    // Cleanup if needed
  }

  isLoggedIn(): boolean {
    return !!this.authService.isAuthenticated();
  }

  logout() {
    this.authService.logout();
  }

  fetchUserInfo() {
    this.userService.getUser().subscribe({
      next: (userData) => (this.user = userData),
      error: (err) => console.error('Failed to load user data:', err),
    });
  }

  loadContacts(): void {
    this.chatService.getChats().subscribe({
      next: (data) => {
        this.contacts = data;
        if (this.contacts.length > 0) {
          let contact = this.contacts[0];
          this.selectedContact = contact;
          this.selectContact(contact);
        }
        this.loading = false;
      },
      error: (err) => {
        console.error('Failed to fetch contacts:', err);
        this.loading = false;
      },
    });
  }

  selectContact(contact: Chat): void {
    this.selectedContact = contact;
    this.selectedContactChange.emit(contact);
  }

  // Reload a specific contact by ID
  selectContactById(contactId: string): void {
    const contact = this.contacts.find((c) => c.id === contactId);
    if (contact) {
      this.selectedContact = contact;
      this.selectContact(contact);
    }
  }

  onContactSelected(contactId: string): void {
    const contact = this.contacts.find((c) => c.id === contactId);
    if (contact) {
      this.selectedContact = contact;
      this.selectContact(contact);
    }
  }
}
