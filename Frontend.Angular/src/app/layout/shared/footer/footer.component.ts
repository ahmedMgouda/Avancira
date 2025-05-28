import { CommonModule } from '@angular/common';
import { Component } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';

@Component({
  selector: 'app-footer',
  imports: [CommonModule, FormsModule, RouterModule],
  templateUrl: './footer.component.html',
  styleUrl: './footer.component.scss'
})
export class FooterComponent {
  currentYear: number = new Date().getFullYear();
  platformName = 'Avancira';
  footerLinks = {
    about: [
      { label: 'About Us', route: '/about' },
      { label: 'Terms & Conditions', route: '/terms' },
      { label: 'Privacy Policy', route: '/privacy-policy' },
      // { label: 'Avancira Global', route: '/global' },
      { label: 'Online Courses', route: '/online-courses' },
      { label: 'States', route: '/states' },
      { label: 'Careers', route: '/careers' }
    ],
    subjects: [
      { label: 'Arts & Hobbies', route: '/category/arts-hobbies' },
      { label: 'Professional Development', route: '/category/professional-development' },
      { label: 'Computer Sciences', route: '/category/computer-sciences' },
      { label: 'Languages', route: '/category/languages' },
      { label: 'Music', route: '/category/music' },
      { label: 'Health & Well-being', route: '/category/health-wellbeing' },
      { label: 'School Support', route: '/category/school-support' },
      { label: 'Sports', route: '/category/sports' }
    ],
    adventure: [
      { label: 'The Blog', route: '/blog' }
    ],
    help: [
      { label: 'Help Centre', route: '/help-centre' },
      // { label: 'Contact', route: '/contact' }
    ],
    social: [
      { icon: 'facebook', url: 'https://www.facebook.com/avancira' },
      // { icon: 'twitter', url: 'https://twitter.com' },
      // { icon: 'instagram', url: 'https://instagram.com' },
      { icon: 'linkedin', url: 'https://www.linkedin.com/company/avancira' }
    ]
  };
}
