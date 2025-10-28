import { CommonModule } from '@angular/common';
import { Component } from '@angular/core';
import { RouterModule } from '@angular/router';

@Component({
  selector: 'app-blank-shell',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './blank-shell.component.html',
  styleUrls: ['./blank-shell.component.scss']
})
export class BlankShellComponent {}