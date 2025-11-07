import { Injectable } from '@angular/core';
import { Router } from '@angular/router';

import { ToastService } from '@core/toast/toast.service';

import { Lesson } from '../models/lesson';
import { User } from '../models/user';

@Injectable({
  providedIn: 'root',
})
export class JitsiService {
  private videoCallWindow: Window | null = null; // Track opened window

  constructor(private router: Router, private toastService: ToastService) {}

  startVideoCall(lesson: Lesson, user: User): void {
    try {
      // Validate meeting domain (should be present in the lesson)
      if (!lesson?.meetingDomain) {
        this.toastService.error('Error: Meeting domain is missing.');
        return;
      }

      // Validate user - Reject guests
      if (!user?.email || !user?.firstName || !user?.lastName) {
        this.toastService.error('Error: Unauthorized access. User must be authenticated.');
        return;
      }

      // Construct display name
      const displayName = `${user.firstName} ${user.lastName}`.trim();

      // Validate essential lesson details
      if (!lesson?.meetingRoomName || !lesson?.meetingUrl) {
        this.toastService.error('Error: Meeting details are missing.');
        return;
      }

      // Build secure URL query parameters
      const queryParams = new URLSearchParams({
        roomName: lesson.meetingRoomName,
        domain: lesson.meetingDomain, 
        meetingUrl: lesson.meetingUrl,
        jwt: lesson.meetingToken || '',
        displayName: encodeURIComponent(displayName),
      }).toString();

      // Define optimal window size settings
      const windowFeatures = this.getWindowFeatures();

      // Generate final meeting URL
      const meetingUrl = `/video-call-window?${queryParams}`;

      // Manage video call popup window
      if (this.videoCallWindow && !this.videoCallWindow.closed) {
        this.videoCallWindow.focus();
      } else {
        this.videoCallWindow = window.open(meetingUrl, '_blank', windowFeatures);

        if (!this.videoCallWindow) {
          throw new Error('Popup blocked or failed to open.');
        }
      }
    } catch (error) {
      this.handleError(error);
    }
  }

  
  private getWindowFeatures(): string {
    const width = Math.max(window.innerWidth || 1024, 800);
    const height = Math.max(window.innerHeight || 768, 600);
    return `width=${width},height=${height},toolbar=0,location=0,status=0,menubar=0,scrollbars=yes,resizable=yes`;
  }

 
  private handleError(error: any): void {
    console.error('JitsiService Error:', error);
    this.toastService.error('Failed to start video call. Please check your settings and try again.');
  }
}
