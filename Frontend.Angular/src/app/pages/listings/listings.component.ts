import { CommonModule } from '@angular/common';
import { AfterViewInit, Component, OnInit, TemplateRef, ViewChild } from '@angular/core';
import { FormsModule } from '@angular/forms';

import { CreateListingComponent } from '../../components/create-listing/create-listing.component';
import { EditListingComponent } from '../../components/edit-listing/edit-listing.component';
import { TableComponent } from '../../layout/shared/table/table.component';

import { AlertService } from '../../services/alert.service';
import { LessonCategoryService } from '../../services/lesson-category.service';
import { ListingService } from '../../services/listing.service';

import { Listing } from '../../models/listing';

@Component({
  selector: 'app-listings',
  imports: [CommonModule, FormsModule, CreateListingComponent, EditListingComponent, TableComponent],
  templateUrl: './listings.component.html',
  styleUrl: './listings.component.scss'
})
export class ListingsComponent implements OnInit, AfterViewInit {
  @ViewChild('visibilityCell', { static: false }) visibilityCell!: TemplateRef<any>;

  page: number = 1;
  pageSize: number = 10;
  pageSizeOptions: number[] = [5, 10, 50, 100];
  totalResults: number = 0;
  listings: Listing[] = []; // Listings array can contain null
  selectedListing: Listing | null = null; // Selected listing can be null
  listingColumns: any[] = [];
  listingActions = [
    {
      label: 'Edit',
      icon: 'fa-edit',
      class: 'btn-sm btn-outline-secondary',
      callback: (listing: any) => this.editListing(listing)
    },
    {
      label: 'Delete',
      icon: 'fa-trash',
      class: 'btn-sm btn-outline-danger',
      callback: (listing: any) => this.deleteListing(listing)
    }
  ];

  constructor(
    private alertService: AlertService,
    private lessonCategoryService: LessonCategoryService,
    private listingService: ListingService,
  ) { }

  ngOnInit(): void {
    this.loadListings();
  }

  ngAfterViewInit(): void {
    this.listingColumns = [
      { key: 'title', label: 'Title' },
      { key: 'lessonCategory', label: 'Category' },
      {
        key: 'locations',
        label: 'Locations',
        formatter: (value: any) => value.map((loc: string) => `<span class="badge bg-primary me-1">${loc}</span>`).join(' ') || 'N/A'
      },
      {
        key: 'rates.hourly',
        label: 'Hourly Rate',
        formatter: (value: any) => value ? `$${value}` : 'N/A'
      },
      {
        key: 'rates.fiveHours',
        label: '5-hour Pack',
        formatter: (value: any) => value ? `$${value}` : 'N/A'
      },
      {
        key: 'rates.tenHours',
        label: '10-hour Pack',
        formatter: (value: any) => value ? `$${value}` : 'N/A'
      },
      {
        key: 'isVisible',
        label: 'Visibility',
        cellTemplate: this.visibilityCell
      }];
  }

  loadListings(): void {
    this.listingService.getListings(this.page, this.pageSize).subscribe({
      next: (data) => {
        this.listings = data.results;
        this.totalResults = data.totalResults;
        if (this.listings.length > 0) {
          this.selectedListing = this.listings[0];
        }
      },
      error: (err) => {
        console.error('Failed to fetch listings:', err);
      },
    });
  }

  editListing(listing: Listing) {
    this.selectedListing = listing;
  }

  selectListing(listing: Listing) {
    this.selectedListing = listing;
  }


  toggleVisibility(listing: Listing) {
    const updatedVisibility = !listing.isVisible;

    this.listingService.updateListingVisibility(listing.id, updatedVisibility)
      .subscribe(
        () => {
          listing.isVisible = updatedVisibility;
        },
        error => {
          console.error('Error updating visibility:', error);
        }
      );
  }

  /** Delete a listing */
  async deleteListing(listing: Listing): Promise<void> {
    const confirmed = await this.alertService.confirm(
      `Are you sure you want to delete the listing: ${listing.title}?`,
      'Delete Listing',
      'Yes, delete it'
    );

    if (!confirmed) return;

    this.listingService.deleteListing(listing.id).subscribe({
      next: () => {
        this.listings = this.listings.filter(l => l.id !== listing.id);
        if (this.selectedListing?.id === listing.id) {
          this.selectedListing = this.listings.length > 0 ? this.listings[0] : null;
        }
        this.alertService.successAlert('Listing deleted successfully.', 'Success');
      },
      error: (err) => {
        console.error('Error deleting listing:', err);
        this.alertService.errorAlert('Failed to delete listing. Please try again.', 'Error');
      },
    });
  }

  editingSection: string | null = null;
  editSection(section: string): void {
    this.editingSection = section;
    this.isEditListingModalOpen = true;
  }

  isCreateListingModalOpen = false;

  openCreateListingModal(): void {
    this.isCreateListingModalOpen = true;
  }

  closeCreateListingModal(): void {
    this.loadListings();
    this.isCreateListingModalOpen = false;
  }


  isEditListingModalOpen = false;

  openEditListingModal(): void {
    this.isEditListingModalOpen = true;
  }

  closeEditListingModal(): void {
    this.isEditListingModalOpen = false;

    // Reload the updated listing
    if (this.selectedListing) {
      this.listingService.getListing(this.selectedListing.id).subscribe({
        next: (updatedListing) => {
          // Update the selected listing details
          this.selectedListing = updatedListing;

          // Update the listing inside the array
          const index = this.listings.findIndex(l => l.id === updatedListing.id);
          if (index !== -1) {
            this.listings[index] = updatedListing;
          }
        },
        error: (err) => {
          console.error('Failed to reload updated listing:', err);
        }
      });
    }
  }


  get totalPages(): number {
    return Math.ceil(this.totalResults / this.pageSize);
  }

  onPageChange(newPage: number) {
    this.page = newPage;
    this.loadListings();
  }

  onPageSizeChange(newSize: number) {
    this.pageSize = newSize;
    this.loadListings();
  }
}
