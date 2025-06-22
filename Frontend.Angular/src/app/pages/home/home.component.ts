import { CommonModule } from '@angular/common';
import { AfterViewInit, Component, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterModule } from '@angular/router';
import * as AOS from 'aos';
import { OwlOptions } from 'ngx-owl-carousel-o';
import { CarouselModule } from 'ngx-owl-carousel-o';

import { GoogleMapsService } from '../../services/google-maps.service';
import { LandingService } from '../../services/landing.service';

import { LessonCategory } from '../../models/lesson-category';
@Component({
  selector: 'app-home',
  imports: [CommonModule, FormsModule, CarouselModule, RouterModule],
  templateUrl: './home.component.html',
  styleUrl: './home.component.scss'
})
export class HomeComponent implements OnInit, AfterViewInit {
  categories: LessonCategory[] = [];
  courses: any[] = [];
  trendingCourses: any[] = [];
  instructors: any[] = [];
  jobLocations: any[] = [];
  studentReviews: any[] = [];
  searchQuery: string = '';
  selectedLocation: { lat: number; lng: number } | null = null;
  selectedCategory: string = "All";
  filteredCourses: any[] = [];
  courseCategories: LessonCategory[] = [];
  totalCourses: number = 0;
  newCoursesToday: number = 0;
  faqs = [
    {
      question: 'How does Avancira work?',
      answer: 'Avancira connects students with expert tutors. Simply sign up, browse tutor profiles, book lessons, and manage your learning in one place.'
    },
    {
      question: 'Do I need a membership to book lessons?',
      answer: 'Yes, a membership is required to access tutors and book lessons. Membership offers exclusive discounts and priority booking.'
    },
    {
      question: 'How do I book a lesson?',
      answer: 'Sign in, select a tutor, choose your preferred date and time, confirm the booking, and complete the payment securely through Avancira.'
    },
    {
      question: 'Can I reschedule or cancel a lesson?',
      answer: 'Yes, you can reschedule or cancel a lesson from your dashboard. Cancellation policies may vary by tutor.'
    },
    {
      question: 'How do I pay for lessons?',
      answer: 'Payments are made securely via credit/debit cards, PayPal, or other supported methods. You can also buy lesson packages for discounts.'
    },
    {
      question: 'Can I message a tutor before booking?',
      answer: 'Yes, you can send a message to a tutor to ask any questions before booking a lesson.'
    },
    {
      question: 'Do tutors provide learning materials?',
      answer: 'Some tutors offer learning materials, but this depends on the tutor. You can check their profile for details.'
    },
    {
      question: 'What if I’m not satisfied with my lesson?',
      answer: 'If you are not satisfied, you can contact support for assistance. We strive to ensure a great learning experience.'
    },
    {
      question: 'Can I cancel my membership?',
      answer: 'Yes, you can cancel your membership at any time through your account settings.'
    },
    {
      question: 'Are the tutors qualified?',
      answer: 'Yes, all tutors are verified professionals with relevant experience in their respective subjects.'
    }
  ];

  customOptions: OwlOptions = {
    loop: true,
    margin: 30,
    mouseDrag: true,
    touchDrag: true,
    pullDrag: false,
    dots: false,
    dotsEach: false,
    navSpeed: 700,
    navText: ['<i class="fas fa-chevron-left"></i>', '<i class="fas fa-chevron-right"></i>'],
    responsive: {
      0: { items: 1 },
      400: { items: 2 },
      740: { items: 3 }
    },
    nav: false,
    autoHeight: false
  };

  constructor(
    private googleMapsService: GoogleMapsService,
    private landingService: LandingService,
    private router: Router
  ) { }

  ngOnInit(): void {
    AOS.init({ duration: 1200, once: true, });

    this.loadCourseCounts();
    this.loadCategories();
    this.loadListings();
    this.loadTrendingCourses();
    this.loadInstructors();
    this.loadJobLocations();
    this.loadStudentReviews();
  }

  ngAfterViewInit(): void {
    AOS.refresh();
    this.googleMapsService.loadGoogleMaps().subscribe({
      next: () => this.initializeAutocomplete(),
      error: (error) => console.error('Google Maps loading error:', error),
    });
  }

  initializeAutocomplete(): void {
    const input = document.getElementById('city-search') as HTMLInputElement;
    
    // Check if Google Maps API is loaded and PlaceAutocompleteElement is available
    if (typeof google !== 'undefined' && google.maps && google.maps.places && google.maps.places.PlaceAutocompleteElement) {
      // Use the new PlaceAutocompleteElement
      const autocompleteElement = new google.maps.places.PlaceAutocompleteElement({
        types: ['(regions)'],
        componentRestrictions: { country: 'AU' }
      });
      
      // Replace the input with the new element
      if (input && input.parentNode) {
        input.parentNode.replaceChild(autocompleteElement, input);
      }
      
      autocompleteElement.addEventListener('gmp-placeselect', (event: any) => {
        const place = event.place;
        if (place.geometry && place.geometry.location) {
          this.selectedLocation = {
            lat: place.geometry.location.lat(),
            lng: place.geometry.location.lng(),
          };
          console.log('Selected location:', this.selectedLocation);
        }
      });
    } else if (typeof google !== 'undefined' && google.maps && google.maps.places && google.maps.places.Autocomplete) {
      // Fallback to the legacy Autocomplete
      const autocomplete = new google.maps.places.Autocomplete(input, {
        types: ['(regions)'],
        componentRestrictions: { country: 'AU' }
      });

      autocomplete.addListener('place_changed', () => {
        const place = autocomplete.getPlace();
        if (place.geometry && place.geometry.location) {
          this.selectedLocation = {
            lat: place.geometry.location.lat(),
            lng: place.geometry.location.lng(),
          };
          console.log('Selected location:', this.selectedLocation);
        }
      });
    } else {
      console.warn('Google Maps API not fully loaded or Places library not available');
    }
  }

  loadCourseCounts(): void {
    this.landingService.getCourseStats().subscribe({
      next: (counts) => {
        this.totalCourses = counts.totalListings;
        this.newCoursesToday = counts.newListingsToday;
      },
      error: (error) => console.error('Error loading course counts:', error),
    });
  }

  loadCategories(): void {
    this.landingService.getCategories().subscribe({
      next: (categories) => {
        this.categories = categories;
        this.courseCategories = [
          { id: '0', name: 'All', courses: 0, image: '' },
          ...this.categories
        ];
      },
      error: (error) => console.error('Error loading categories:', error),
    });
  }

  loadListings(): void {
    this.landingService.getListings().subscribe({
      next: (listings) => {
        this.courses = listings;
        this.filteredCourses = [...this.courses]; // Default filtered courses
      },
      error: (error) => console.error('Error loading listings:', error),
    });
  }

  loadTrendingCourses(): void {
    this.landingService.getTrendingListings().subscribe({
      next: (trendingCourses) => {
        this.trendingCourses = trendingCourses;
      },
      error: (error) => console.error('Error loading trending courses:', error),
    });
  }

  loadInstructors(): void {
    this.landingService.getInstructors().subscribe({
      next: (instructors) => {
        this.instructors = instructors;
      },
      error: (error) => console.error('Error loading instructors:', error),
    });
  }

  loadJobLocations(): void {
    this.landingService.getJobLocations().subscribe({
      next: (locations) => {
        this.jobLocations = locations;
      },
      error: (error) => console.error('Error loading job locations:', error),
    });
  }

  loadStudentReviews(): void {
    this.landingService.getStudentReviews().subscribe({
      next: (reviews) => {
        this.studentReviews = reviews;
      },
      error: (error) => console.error('Error loading student reviews:', error),
    });
  }

  selectCategory(lessonCategory: string) {
    this.selectedCategory = lessonCategory;
    this.filteredCourses = lessonCategory === "All"
      ? this.courses
      : this.courses.filter(course => course.lessonCategory === lessonCategory);
  }

  performSearch(): void {
    if (!this.searchQuery.trim()) {
      return;
    }
    this.router.navigate(['/search-results'], { queryParams: { query: this.searchQuery } });
  }

  navigateToListing(listingId: string): void {
    this.router.navigate(['/listing', listingId]);
  }

  navigateToSearch(item: string): void {
    this.router.navigate(['/search-results'], { queryParams: { query: item } });
  }
}
