import { CommonModule } from '@angular/common';
import { Component } from '@angular/core';
import { RouterModule } from '@angular/router';

@Component({
  selector: 'app-check-email',
  templateUrl: './check-email.component.html',
  styleUrls: ['./check-email.component.scss'],
  imports: [CommonModule, RouterModule]
})
export class CheckEmailComponent {}

