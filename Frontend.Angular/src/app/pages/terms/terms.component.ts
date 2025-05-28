import { Component } from '@angular/core';

import { PlatformService } from '../../services/platform.service';

@Component({
  selector: 'app-terms',
  imports: [],
  templateUrl: './terms.component.html',
  styleUrl: './terms.component.scss'
})
export class TermsComponent {
  platformInfo: any = {};

  constructor(private platformService: PlatformService) {}

  ngOnInit() {
    this.platformInfo = this.platformService.getPlatformInfo();
  }
}
