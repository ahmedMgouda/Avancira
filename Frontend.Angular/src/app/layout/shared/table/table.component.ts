import { CommonModule } from '@angular/common';
import { Component, EventEmitter, Input, Output, TemplateRef } from '@angular/core';
import { FormsModule } from '@angular/forms';

@Component({
  selector: 'app-table',
  imports: [CommonModule, FormsModule],
  templateUrl: './table.component.html',
  styleUrl: './table.component.scss'
})
export class TableComponent<T extends Record<string, any>> {
  @Input() data: T[] = [];
  @Input() columns: { key: string; label: string, formatter?: (value: any, item?: T) => string, cellTemplate?: TemplateRef<any>; }[] = [];
  @Input() actions: { label: string; icon: string; class: string; callback: (item: T) => void, condition?: (item: T) => boolean }[] = [];

  @Input() page: number = 1;
  @Input() pageSize: number = 10;
  @Input() totalResults: number = 0;
  @Input() pageSizeOptions: number[] = [5, 10, 50, 100];

  @Output() pageChange = new EventEmitter<number>();
  @Output() pageSizeChange = new EventEmitter<number>();

  get totalPages(): number {
    return Math.ceil(this.totalResults / this.pageSize);
  }

  onPageChange(newPage: number): void {
    this.pageChange.emit(newPage);
  }

  onPageSizeChange(event: any): void {
    this.pageSizeChange.emit(+event.target.value);
  }

  getValue(item: T, key: string): any {
    const column = this.columns.find(col => col.key === key);
    if (!column) return '';
    const value = key.split('.').reduce((obj, prop) => obj?.[prop], item);
    return column.formatter ? column.formatter(value, item) : value ?? '';
  }



  sortColumn: string = '';
  sortDirection: string = 'asc';

  toggleSort(columnKey: string) {
      if (this.sortColumn === columnKey) {
          this.sortDirection = this.sortDirection === 'asc' ? 'desc' : 'asc';
      } else {
          this.sortColumn = columnKey;
          this.sortDirection = 'asc';
      }
  }

  sortedData() {
      if (!this.sortColumn) {
          return this.data;
      }
      return [...this.data].sort((a, b) => {
          const aValue = this.getValue(a, this.sortColumn);
          const bValue = this.getValue(b, this.sortColumn);

          if (typeof aValue === 'string') {
              return this.sortDirection === 'asc' ? aValue.localeCompare(bValue) : bValue.localeCompare(aValue);
          } else {
              return this.sortDirection === 'asc' ? aValue - bValue : bValue - aValue;
          }
      });
  }

}
