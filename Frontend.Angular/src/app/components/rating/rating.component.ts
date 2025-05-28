import { CommonModule } from '@angular/common';
import { Component, EventEmitter, Input, Output } from '@angular/core';
import { FormsModule } from '@angular/forms';

@Component({
  selector: 'app-rating',
  imports: [CommonModule, FormsModule],
  templateUrl: './rating.component.html',
  styleUrl: './rating.component.scss'
})
export class RatingComponent {
  @Input() rating: number|null = 0;
  @Output() ratingChange = new EventEmitter<number>();

  hoveredStar: number = 0;

  onRatingChange(value: number): void {
    this.rating = value;
    this.ratingChange.emit(this.rating);
  }

  onHover(star: number): void {
    this.hoveredStar = star;
  }
}
