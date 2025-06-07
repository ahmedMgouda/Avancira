import { CommonModule } from '@angular/common';
import { Component, EventEmitter, Input, OnChanges, Output } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Subject } from 'rxjs';
import { debounceTime, distinctUntilChanged } from 'rxjs/operators';

@Component({
  selector: 'app-auto-complete-input',
  imports: [CommonModule, FormsModule],
  templateUrl: './auto-complete-input.component.html',
  styleUrl: './auto-complete-input.component.scss'
})
export class AutoCompleteInputComponent implements OnChanges {
  @Input() options: { id: string; name: string }[] = [];
  @Output() selectedOption = new EventEmitter<string>();
  @Output() searchTextChanged = new EventEmitter<string>();
  @Output() newOptionCreated = new EventEmitter<string>();

  searchText = '';
  filteredOptions: { id: string; name: string }[] = [];
  selectedOptionId: string | null = null;

  // Subject for search text
  private searchTextSubject = new Subject<string>();

  constructor() {
    // Subscribe to debounced search text changes
    this.searchTextSubject
      .pipe(
        debounceTime(300),
        distinctUntilChanged()
      )
      .subscribe((searchTerm) => {
        // this.filterOptions(searchTerm);
        this.searchTextChanged.emit(searchTerm);
      });
  }

  ngOnChanges(): void {
    this.filterOptions();
  }
  onInputChange(): void {
    this.searchTextSubject.next(this.searchText);
  }

  filterOptions(): void {
    // Convert search text to lowercase for case-insensitive comparison
    const searchTextLower = this.searchText.toLowerCase();
  
    // Filter options that include the search text
    this.filteredOptions = this.options.filter(option =>
      option.name.toLowerCase().includes(searchTextLower)
    );
  
    // Determine the best match to select
    let bestMatch = null;
  
    // Check for an exact match
    for (const option of this.filteredOptions) {
      if (option.name.toLowerCase() === searchTextLower) {
        bestMatch = option;
        break;
      }
    }
  
    // If no exact match, find the shortest option that includes the search text
    if (!bestMatch && this.filteredOptions.length > 0) {
      bestMatch = this.filteredOptions.reduce((shortest, current) =>
        current.name.length < shortest.name.length ? current : shortest
      );
    }
  
    // Select the best match if found, otherwise reset selection
    if (bestMatch) {
      this.selectOption(bestMatch);
    } else {
      this.selectedOptionId = null;
    }
  }
  
  onOptionChange(event: Event): void {
    const selectElement = event.target as HTMLSelectElement;
    const selectedId = selectElement.value;
    const selectedOption = this.filteredOptions.find(option => option.id === selectedId);
    if (selectedOption) {
      this.selectOption(selectedOption);
    }
  }

  selectOption(option: { id: string; name: string }): void {
    // this.searchText = option.name;
    this.selectedOptionId = option.id;
    this.selectedOption.emit(option.id);
  }

  createNewOption(): void {
    if (this.searchText.trim()) {
      this.newOptionCreated.emit(this.searchText.trim());
      this.filterOptions();
    }
  }
}
