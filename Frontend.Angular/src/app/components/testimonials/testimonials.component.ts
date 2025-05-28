import { CommonModule } from '@angular/common';
import { Component } from '@angular/core';
import { FormsModule } from '@angular/forms';

@Component({
  selector: 'app-testimonials',
  imports: [CommonModule, FormsModule],
  templateUrl: './testimonials.component.html',
  styleUrl: './testimonials.component.scss'
})
export class TestimonialsComponent {
  testimonialList = [
    { name: 'Hector', subject: 'Physics', feedback: 'Excellent tutor!', reviewer: 'Vanessa' },
    { name: 'Farida', subject: 'Python', feedback: 'Highly engaging!', reviewer: 'Stacy' },
    // Add more testimonials as needed
  ];

  currentSlide = 0;

  slideRight() {
    const totalSlides = this.testimonialList.length;
    this.currentSlide = (this.currentSlide + 1) % totalSlides; // Loop to the beginning
    this.updateSlider();
  }

  slideLeft() {
    const totalSlides = this.testimonialList.length;
    this.currentSlide = (this.currentSlide - 1 + totalSlides) % totalSlides; // Loop to the end
    this.updateSlider();
  }

  updateSlider() {
    const track = document.querySelector('.testimonial-track') as HTMLElement;
    if (track) {
      track.style.transform = `translateX(-${this.currentSlide * 100}%)`;
    }
  }

}
