import { CommonModule } from '@angular/common';
import { Component, EventEmitter, OnInit, Output } from '@angular/core';
import { FormBuilder, FormGroup, FormsModule, ReactiveFormsModule, Validators } from '@angular/forms';

import { AutoCompleteInputComponent } from '../auto-complete-input/auto-complete-input.component';
import { MultiStepModalComponent } from '../multi-step-modal/multi-step-modal.component';

import { LessonCategoryService } from '../../services/lesson-category.service';
import { ListingService } from '../../services/listing.service';

import { LessonCategory } from '../../models/lesson-category';
import { Listing } from '../../models/listing';

@Component({
  selector: 'app-create-listing',
  imports: [CommonModule, FormsModule, ReactiveFormsModule, AutoCompleteInputComponent, MultiStepModalComponent],
  templateUrl: './create-listing.component.html',
  styleUrl: './create-listing.component.scss'
})
export class CreateListingComponent implements OnInit {
  @Output() onClose = new EventEmitter<void>();
  createListingForm!: FormGroup;
  locationOptions: string[] = ['Webcam', 'TutorLocation', 'StudentLocation'];
  socialPlatformOptions: string[] = ['Facebook', 'Instagram', 'Twitter', 'LinkedIn', 'Email'];
  lessonCategories: LessonCategory[] = [];
  selectedImageFile: File | null = null;

  constructor(
    private fb: FormBuilder,
    private lessonCategoryService: LessonCategoryService,
    private listingService: ListingService
  ) { }

  ngOnInit(): void {
    this.createListingForm = this.fb.group({
      title: ['', [Validators.required, Validators.minLength(3)]],
      listingImage: [null],
      lessonCategoryId: [null, Validators.required],
      locations: [[]],
      aboutLesson: ['', Validators.required],
      aboutYou: ['', Validators.required],
      rates: this.fb.group({
        hourly: [0, [Validators.min(0)]],
        fiveHours: [0],
        tenHours: [0],
      }),
      socialPlatforms: [[]]
    });

    this.loadLessonCategories('');
  }

  loadLessonCategories(searchText: string): void {
    this.lessonCategoryService.getFilteredCategories(searchText).subscribe({
      next: (data) => {
        this.lessonCategories = data.results;
      },
      error: (err) => {
        console.error('Failed to fetch lesson categories', err);
      }
    });
  }

  calculatePerHour(): void {
    const hourlyRate = this.createListingForm.get('rates.hourly')?.value || 0;
    const fiveHoursRate = hourlyRate * 5;
    const tenHoursRate = hourlyRate * 10;

    this.createListingForm.get('rates')?.patchValue({
      fiveHours: fiveHoursRate,
      tenHours: tenHoursRate
    });
  }

  calculatePerFiveHours(): void {
    const fiveHoursRate = this.createListingForm.get('rates.fiveHours')?.value || 0;
    const tenHoursRate = fiveHoursRate * 2;

    this.createListingForm.get('rates')?.patchValue({
      tenHours: tenHoursRate
    });
  }

  selectOption(selectedCategoryId: number): void {
    this.createListingForm.get('lessonCategoryId')?.patchValue(selectedCategoryId);
  }

  addNewLessonCategory(newCategoryName: string): void {
    this.lessonCategoryService.createCategory({ name: newCategoryName }).subscribe({
      next: (createdCategory) => {
        // this.lessonCategories.push(createdCategory);
        this.lessonCategories = [...this.lessonCategories, createdCategory];
        this.createListingForm.get('lessonCategoryId')?.patchValue(createdCategory.id);

      },
      error: (err) => {
        console.error('Failed to create new lesson category:', err);
      }
    });
  }

  onImageSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    if (input.files && input.files.length > 0) {
      this.selectedImageFile = input.files[0];
    }
  }

  submitCreateListing(): void {
    if (this.createListingForm.invalid) {
      return;
    }

    const formValues = this.createListingForm.value;

    const processedListing: Listing = {
      title: formValues.title,
      listingImage: this.selectedImageFile,
      lessonCategoryId: formValues.lessonCategoryId,
      locations: formValues.locations,
      aboutLesson: formValues.aboutLesson,
      aboutYou: formValues.aboutYou,
      rates: {
        hourly: formValues.rates.hourly,
        fiveHours: formValues.rates.fiveHours,
        tenHours: formValues.rates.tenHours
      },
      socialPlatforms: formValues.socialPlatforms || [],

      id: -1,
      isVisible: true,
      tutorId: "",
      tutorName: "",
      tutorBio: "",
      tutorAddress: null,
      contactedCount: 0,
      reviews: 0,
      rating: null,
      lessonCategory: "",
      listingImagePath: "",
    };


    this.listingService.createListing(processedListing).subscribe({
      next: () => {
        this.closeModal();
      },
      error: (err) => {
        console.error('Failed to create listing:', err);
      }
    });
  }

  stepLabels: string[] = [
    'Title & Image',
    'Lesson & Location',
    'About the Lesson & You',
    'Rates',
    'Social Platforms'
  ];
  step: number = 1;
  totalSteps = this.stepLabels.length;

  isStepValid = (): boolean => {
    switch (this.step) {
      case 1:
        return !!this.createListingForm.get('title')?.valid;
      case 2:
        return !!this.createListingForm.get('lessonCategoryId')?.valid && !!this.createListingForm.get('locations')?.valid;
      case 3:
        return !!this.createListingForm.get('aboutLesson')?.valid && !!this.createListingForm.get('aboutYou')?.valid;
      case 4:
        return !!this.createListingForm.get('rates.hourly')?.valid;
      case 5:
        return true; // No validation for step 5
      default:
        return false;
    }
  };
  
  closeModal(): void {
    this.onClose.emit();
  }
}
