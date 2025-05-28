import { Component } from '@angular/core';

import { PlatformService } from '../../services/platform.service';

@Component({
  selector: 'app-privacy',
  imports: [],
  templateUrl: './privacy.component.html',
  styleUrl: './privacy.component.scss'
})
export class PrivacyComponent {
  platformInfo: any = {};

  constructor(private platformService: PlatformService) {}

  ngOnInit() {
    this.platformInfo = this.platformService.getPlatformInfo();
  }
}
