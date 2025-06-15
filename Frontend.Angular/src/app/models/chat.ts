import { UserRole } from "./enums/user-role";

export interface Chat {
    id: number;
    listingId: string;
    studentId: string;
    recipientId: string;
    name: string;
    profileImagePath: string;
    lastMessage: string;
    timestamp: Date;
    details: string;
    messages: Message[];
    myRole: UserRole;
  }

  
export interface Message {
    sentBy: 'me' | 'contact';
    senderId: string;
    senderName: string;
    content: string;
    timestamp: Date | 'N/A';
  }
