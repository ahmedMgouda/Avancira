/// <reference types="@angular/localize" />

import { bootstrapApplication } from '@angular/platform-browser';
import { AppComponent } from './app/app.component';
import { registerLicense } from '@syncfusion/ej2-base';
import { appConfig } from './app/app.config';

registerLicense('ORg4AjUWIQA/Gnt2XVhhQlJHfV5AQmBIYVp/TGpJfl96cVxMZVVBJAtUQF1hTH5Qd0ZjXn5Yc3BcQ2hU'); 

bootstrapApplication(AppComponent, appConfig)
  .catch((err) => console.error(err));
