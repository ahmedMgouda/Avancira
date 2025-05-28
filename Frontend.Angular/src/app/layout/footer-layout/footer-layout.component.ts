import { CommonModule } from '@angular/common';
import { Component } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';

import { FooterComponent } from '../shared/footer/footer.component';

@Component({
  selector: 'app-footer-layout',
  imports: [CommonModule, FormsModule, RouterModule, FooterComponent],
  templateUrl: './footer-layout.component.html',
  styleUrl: './footer-layout.component.scss'
})

export class FooterLayoutComponent {

}