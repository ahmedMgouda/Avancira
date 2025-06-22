import { CommonModule } from '@angular/common';
import { Component, EventEmitter, Input, OnInit, Output } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';

import { AuthService } from '../../services/auth.service';
import { ChatService } from '../../services/chat.service';
import { UserService } from '../../services/user.service';

import { TimeAgoPipe } from "../../pipes/time-ago.pipe";

import { ImageFallbackDirective } from '../../directives/image-fallback.directive';

import { Chat } from '../../models/chat';
import { User } from '../../models/user';

@Component({
  selector: 'app-message-list',
  imports: [CommonModule, FormsModule, ImageFallbackDirective, TimeAgoPipe],
  templateUrl: './message-list.component.html',
  styleUrls: ['./message-list.component.scss']
})
export class MessageListComponent implements OnInit {
  loading = true;
  contacts: Chat[] = [];
  @Input() selectedContact: Chat | null = null;
  @Output() selectedContactChange = new EventEmitter<Chat>();
  user!: User;

  constructor(
    private router: Router,
    private userService: UserService,
    private authService: AuthService,
    private chatService: ChatService
  ) { }

  ngOnInit(): void {
    if (this.isLoggedIn()) {
      this.fetchUserInfo();
    }
    this.loadContacts();
  }

  isLoggedIn(): boolean {
    return !!this.authService.getToken();
  }

  logout() {
    this.authService.logout();
    this.router.navigate(['/']);
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
