import { CommonModule } from '@angular/common';
import { Component, EventEmitter, Input, OnInit, Output } from '@angular/core';
import { FormsModule } from '@angular/forms';

import { AutoCompleteInputComponent } from '../auto-complete-input/auto-complete-input.component';
import { ModalComponent } from '../modal/modal.component';

import { LessonCategoryService } from '../../services/lesson-category.service';
import { ListingService } from '../../services/listing.service';

import { LessonCategory } from '../../models/lesson-category';
import { Listing } from '../../models/listing';

@Component({
  selector: 'app-edit-listing',
  imports: [CommonModule, FormsModule, ModalComponent, AutoCompleteInputComponent],
  templateUrl: './edit-listing.component.html',
  styleUrl: './edit-listing.component.scss'
})
export class EditListingComponent implements OnInit {
  @Input() title: string = "Edit Listing";
  @Input() listing: Listing | null = null;
  @Input() editingSection: string | null = null;
  @Input() isModalOpen: boolean = false;
  @Output() onClose = new EventEmitter<void>();
  @Output() saveChanges = new EventEmitter<void>();
  lessonCategories: LessonCategory[] = [];
  selectedLessonCategory: string | null = null;
  selectedLocations: string[] = [];
  locationOptions: string[] = ['Webcam', 'TutorLocation', 'StudentLocation'];

  constructor(
    private lessonCategoryService: LessonCategoryService,
    private listingService: ListingService
  ) { }

  ngOnInit(): void {
    this.loadLessonCategories('');
    if (this.listing?.locations) {
      this.selectedLocations = [...this.listing.locations];
    }
  }

  loadLessonCategories(searchText: string): void {
    this.lessonCategoryService.getFilteredCategories(searchText).subscribe({
      next: (data) => {
        this.lessonCategories = data.results;

        // **Set selected lesson category from listing**
        if (this.listing?.lessonCategoryId) {
          this.selectedLessonCategory = this.listing.lessonCategoryId;
        }
      },
      error: (err) => {
        console.error('Failed to fetch lesson categories', err);
      }
    });
  }

  selectOption(selectedCategoryId: string): void {
    this.selectedLessonCategory = selectedCategoryId;
    if (this.listing) {
      this.listing.lessonCategoryId = selectedCategoryId;
    }
  }

  addNewLessonCategory(newCategoryName: string): void {
    this.lessonCategoryService.createCategory({ name: newCategoryName }).subscribe({
      next: (createdCategory) => {
        this.lessonCategories = [...this.lessonCategories, createdCategory];
        this.selectedLessonCategory = createdCategory.id;
      },
      error: (err) => {
        console.error('Failed to create new lesson category:', err);
      }
    });
  }


  save(): void {
    if (!this.listing || !this.editingSection) return;

    let updateObservable;

    switch (this.editingSection) {
      case 'title':
        updateObservable = this.listingService.updateListingTitle(this.listing.id, this.listing.title);
        break;

      case 'image':
        if (this.listing.listingImage) {
          updateObservable = this.listingService.updateListingImage(this.listing.id, this.listing.listingImage);
        }
        break;

      case 'Lesson Category':
        if (this.selectedLessonCategory) {
          updateObservable = this.listingService.updateListingCategory(this.listing.id, this.selectedLessonCategory);
        }
        break;

      case 'Locations':
          this.listing.locations = [...this.selectedLocations];
          updateObservable = this.listingService.updateListingLocations(this.listing.id, this.selectedLocations);
          break;
  
      case 'About Lesson':
      case 'About You':
        updateObservable = this.listingService.updateListingDescription(this.listing.id, this.listing.aboutLesson, this.listing.aboutYou);
        break;

      case 'Rates':
        updateObservable = this.listingService.updateListingRates(this.listing.id, this.listing.rates);
        break;

      default:
        console.warn('No valid section to update');
        return;
    }

    if (updateObservable) {
      updateObservable.subscribe({
        next: () => {
          console.log(`${this.editingSection} updated successfully`);
          this.saveChanges.emit();  // Emit event to notify parent
          this.onClose.emit();
        },
        error: (err) => console.error(`Error updating ${this.editingSection}:`, err)
      });
    }
  }



  closeModal(): void {
    this.onClose.emit();
  }

}
