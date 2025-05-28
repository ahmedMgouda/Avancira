import { Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { RouterModule } from '@angular/router';

import { SidebarComponent } from "../shared/sidebar/sidebar.component";

@Component({
  selector: 'app-sidebar-layout',
  imports: [RouterOutlet, RouterModule, SidebarComponent],
  templateUrl: './sidebar-layout.component.html',
  styleUrl: './sidebar-layout.component.scss'
})
export class SidebarLayoutComponent {

}