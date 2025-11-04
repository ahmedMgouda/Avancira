/*********************************************************************
 * Angular test bootstrap file
 * Needed for Karma + Jasmine test discovery
 *********************************************************************/
import { getTestBed } from '@angular/core/testing';
import {
  BrowserDynamicTestingModule,
  platformBrowserDynamicTesting,
} from '@angular/platform-browser-dynamic/testing';

import 'zone.js/testing';

getTestBed().initTestEnvironment(
  BrowserDynamicTestingModule,
  platformBrowserDynamicTesting()
);

// Auto-discover all *.spec.ts files under /src
const context = (require as any).context('./', true, /\.spec\.ts$/);
context.keys().forEach(context);

