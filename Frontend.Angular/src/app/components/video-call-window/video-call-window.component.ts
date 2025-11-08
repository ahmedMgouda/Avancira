import { Component, OnDestroy, OnInit } from '@angular/core';
import { ActivatedRoute } from '@angular/router';

import { ToastService } from '@core/toast/services/toast.service';


declare var JitsiMeetExternalAPI: any;

@Component({
  selector: 'app-video-call-window',
  templateUrl: './video-call-window.component.html',
  styleUrls: ['./video-call-window.component.scss']
})
export class VideoCallWindowComponent implements OnInit, OnDestroy {
  private jitsiApi: any | null = null;

  constructor(private route: ActivatedRoute, private toastService: ToastService) {}

  ngOnInit(): void {
    this.route.queryParams.subscribe(params => {
      const roomName = params['roomName'];
      const domain = params['domain'];
      const jwt = params['jwt'];
      const displayName = params['displayName'];

      if (!roomName || !domain) {
        this.toastService.error('Missing required meeting parameters.');
        return;
      }

      this.loadJitsi(roomName, domain, jwt, displayName);
    });
  }

  /**
   * Initializes the Jitsi Meet API and embeds the video call.
   * @param roomName - Name of the Jitsi room.
   * @param domain - Jitsi server domain.
   * @param jwt - Authentication token (optional).
   * @param displayName - Display name of the user.
   */
  private loadJitsi(roomName: string, domain: string, jwt: string | null, displayName: string | null): void {
    const container = document.getElementById('jitsi-container');
    if (!container) {
      this.toastService.error('Failed to find Jitsi container.');
      return;
    }

    try {
      this.jitsiApi = new JitsiMeetExternalAPI(domain, {
        roomName: roomName,
        parentNode: container,
        jwt: jwt || '',
        userInfo: {
          displayName: displayName || 'Guest'
        },
        configOverwrite: {
          enableWelcomePage: false,
          prejoinPageEnabled: false,
          startWithAudioMuted: true,
          startWithVideoMuted: true
        },
        interfaceConfigOverwrite: {
          filmStripOnly: false
        }
      });

      this.toastService.success('Video call started successfully.');

      this.jitsiApi.addEventListener('videoConferenceLeft', () => {
        this.toastService.info('You have left the meeting.');
        this.cleanupJitsi();
      });

    } catch (error) {
      console.error('Failed to initialize Jitsi Meet API:', error);
      this.toastService.error('Could not start the video call. Please try again.');
    }
  }

  /**
   * Cleans up Jitsi API instance and closes the window.
   */
  private cleanupJitsi(): void {
    if (this.jitsiApi) {
      this.jitsiApi.dispose();
      this.jitsiApi = null;
    }
    window.close();
  }

  ngOnDestroy(): void {
    this.cleanupJitsi();
  }
}
