import { CommonModule } from '@angular/common';
import { Component } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, ParamMap, Router } from '@angular/router';
import { Subscription } from 'rxjs';

import { FooterComponent } from '../../layout/shared/footer/footer.component';
import { HeaderComponent } from '../../layout/shared/header/header.component';

@Component({
  selector: 'app-categories',
  imports: [CommonModule, FormsModule, HeaderComponent, FooterComponent],
  templateUrl: './categories.component.html',
  styleUrl: './categories.component.scss'
})
export class CategoriesComponent {
  categoryName: string = '';
  subcategories: { title: string; items: string[] }[] = [];
  routeSubscription: Subscription | null = null;

  allData = {
    'arts-hobbies': [
      { title: 'Animal communication', items: ['Animal communication', 'Cat education', 'Dog education'] },
      { title: 'Brain teasers', items: ['Jigsaw', 'Rubik\'s Cube', 'Speed cubing'] },
      { title: 'Card Games', items: ['Blackjack', 'Bridge', 'Poker', 'Rummy', 'Tarot'] },
      { title: 'Cinematographic Arts', items: ['Audio-visual Arts', 'Cinema', 'Film Production', 'Scriptwriting'] },
      { title: 'Crafts', items: ['Origami', 'Knitting', 'Sewing', 'Pottery', 'Woodworking'] },
      { title: 'Performing Arts', items: ['Acting', 'Ballet', 'Contemporary Dance', 'Hip Hop Dance', 'Playwriting'] }
    ],
    'professional-development': [
      { title: 'Skills', items: ['Public Speaking', 'Time Management', 'Investing'] },
      { title: 'Business', items: ['Accounting', 'Business Basics'] },
      { title: 'Law', items: ['Criminal Law', 'Corporate Law', 'Family Law'] },
      { title: 'Psychology', items: ['Behavioral Psychology', 'Clinical Psychology', 'Organizational Psychology'] }
    ],
    'computer-sciences': [
      { title: 'Programming Languages', items: ['Python', 'C++', 'Java', 'JavaScript'] },
      { title: 'Data Science', items: ['Machine Learning', 'Data Analysis', 'Big Data', 'AI Models'] },
      { title: 'Development', items: ['Frontend Development', 'Backend Development', 'Full Stack Development'] },
      { title: 'Technologies', items: ['Blockchain', 'Crypto Currency', 'AWS', 'Cloud Computing'] }
    ],
    'languages': [
      { title: 'Popular Languages', items: ['English', 'Spanish', 'French', 'German', 'Italian'] },
      { title: 'Asian Languages', items: ['Japanese', 'Chinese', 'Korean', 'Vietnamese'] },
      { title: 'Other Languages', items: ['Arabic', 'Russian', 'Hebrew', 'Mandarin', 'Indonesian'] }
    ],
    'music': [
      { title: 'Instruments', items: ['Piano', 'Guitar', 'Bass Guitar', 'Drums', 'Violin', 'Cello'] },
      { title: 'Singing', items: ['Vocal Coaching', 'Choir Singing', 'Solo Singing'] },
      { title: 'Music Production', items: ['Audio Mixing', 'Music Composition', 'Sound Engineering'] }
    ],
    'health-wellbeing': [
      { title: 'Fitness', items: ['Personal Training', 'Yoga', 'Pilates', 'Aerobics'] },
      { title: 'Sports Therapy', items: ['Physical Therapy', 'Injury Prevention', 'Rehabilitation'] },
      { title: 'Mental Wellness', items: ['Meditation', 'Mindfulness', 'Stress Management'] }
    ],  
    'school-support': [
      { title: 'Math & Science', items: ['Maths', 'Physics', 'Chemistry', 'Biology', 'Science'] },
      { title: 'Test Preparation', items: ['IELTS', 'GAMSAT', 'UCAT', 'ESL'] },
      { title: 'Other Subjects', items: ['Geography', 'History', 'Literature'] }
    ],
    'sports': [
      { title: 'Popular Sports', items: ['Soccer', 'Basketball', 'Tennis', 'Swimming'] },
      { title: 'Martial Arts', items: ['Boxing', 'Judo', 'Karate', 'Taekwondo'] },
      { title: 'Outdoor Activities', items: ['Surfing', 'Hiking', 'Rock Climbing', 'Skiing'] }
    ]
  };
  
  friendlyCategoryNames: { [key: string]: string } = {
    'arts-hobbies': 'Arts & Hobbies',
    'professional-development': 'Professional Development',
    'computer-sciences': 'Computer Sciences',
    'languages': 'Languages',
    'music': 'Music',
    'health-wellbeing': 'Health & Well-being',
    'school-support': 'School Support',
    'sports': 'Sports'
  };  

  constructor(private route: ActivatedRoute, private router: Router) { }

  ngOnInit(): void {
    this.routeSubscription = this.route.paramMap.subscribe((params: ParamMap) => {
      const slug = params.get('name') || '';
      this.categoryName = this.friendlyCategoryNames[slug] || 'Unknown Category';
      this.subcategories = this.allData[slug as keyof typeof this.allData] || [];
    });
  }

  navigateToSearch(item: string): void {
    this.router.navigate(['/search-results'], { queryParams: { query: item } });
  }

  ngOnDestroy(): void {
    // Unsubscribe to avoid memory leaks
    this.routeSubscription?.unsubscribe();
  }
}

