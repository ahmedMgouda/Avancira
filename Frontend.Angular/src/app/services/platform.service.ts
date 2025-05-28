import { Injectable } from '@angular/core';

export interface PlatformInfo {
  platformName: string;
  supportEmail: string;
  privacyEmail: string;
  dpoEmail: string;
  address: string;
  phone: string;
  registrationNumber: string;
  president: string;
  lastUpdated: string;
}

@Injectable({
  providedIn: 'root'
})
export class PlatformService {
  private platformInfo: PlatformInfo = {
    platformName: 'Avancira',
    supportEmail: 'support@avancira.com',
    privacyEmail: 'privacy@avancira.com',
    dpoEmail: 'dpo@avancira.com',
    address: '35 Cave Rd, Strathfield, Sydney, Australia',
    phone: '+61 4688 90 677',
    registrationNumber: '683 548 763',
    president: 'Amr Badr',
    lastUpdated: '9 January 2025'
  };

  constructor() { }

  getPlatformInfo(): any {
    return this.platformInfo;
  }
}
