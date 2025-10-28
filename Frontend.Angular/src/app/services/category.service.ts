import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { BehaviorSubject, Observable, tap } from 'rxjs';

import { environment } from '../environments/environment';
import {
  Category,
  CategoryCreateDto,
  CategoryFilter,
  CategoryUpdateDto,
} from '../models/category';

@Injectable({
  providedIn: 'root',
})
export class CategoryService {
  private readonly apiUrl = `${environment.bffBaseUrl}/api/subject-categories`;
  private readonly categoriesSubject = new BehaviorSubject<Category[]>([]);

  readonly categories$ = this.categoriesSubject.asObservable();

  constructor(private readonly http: HttpClient) {}

  getAll(): Observable<Category[]> {
    return this.http.get<Category[]>(this.apiUrl).pipe(
      tap((categories) => this.categoriesSubject.next(categories))
    );
  }

  getById(id: number): Observable<Category> {
    return this.http.get<Category>(`${this.apiUrl}/${id}`);
  }

  create(dto: CategoryCreateDto): Observable<Category> {
    return this.http.post<Category>(this.apiUrl, dto).pipe(
      tap(() => this.refreshCategories())
    );
  }

  update(dto: CategoryUpdateDto): Observable<Category> {
    return this.http.put<Category>(`${this.apiUrl}/${dto.id}`, dto).pipe(
      tap(() => this.refreshCategories())
    );
  }

  delete(id: number): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${id}`).pipe(
      tap(() => this.refreshCategories())
    );
  }

  search(filter: CategoryFilter): Category[] {
    const categories = this.categoriesSubject.value;
    const term = filter.searchTerm?.trim().toLowerCase();

    return categories.filter((category) => {
      const matchesTerm = !term
        || category.name.toLowerCase().includes(term)
        || (category.description ?? '').toLowerCase().includes(term);
      const matchesActive =
        filter.isActive === undefined || category.isActive === filter.isActive;
      const matchesVisible =
        filter.isVisible === undefined || category.isVisible === filter.isVisible;
      const matchesFeatured =
        filter.isFeatured === undefined || category.isFeatured === filter.isFeatured;

      return matchesTerm && matchesActive && matchesVisible && matchesFeatured;
    });
  }

  private refreshCategories(): void {
    this.http.get<Category[]>(this.apiUrl).subscribe({
      next: (categories) => this.categoriesSubject.next(categories),
      error: (error) => {
        console.error('Failed to refresh categories', error);
      },
    });
  }
}
