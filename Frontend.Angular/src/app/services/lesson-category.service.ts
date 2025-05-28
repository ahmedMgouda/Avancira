import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import { environment } from '../environments/environment';
import { LessonCategory } from '../models/lesson-category';
import { PagedResult } from '../models/paged-result';

@Injectable({
  providedIn: 'root'
})
export class LessonCategoryService {
  private apiUrl = `${environment.apiUrl}/lesson/categories`;

  constructor(private http: HttpClient) { }

  createCategory(category: { name: string }): Observable<LessonCategory> {
    return this.http.post<LessonCategory>(`${this.apiUrl}`, category);
  }

  getFilteredCategories(searchText: string): Observable<PagedResult<LessonCategory>> {
    let url = this.apiUrl;
    if (searchText && searchText.trim() !== '') {
      url += `?query=${encodeURIComponent(searchText.trim())}`;
    }

    return this.http.get<PagedResult<LessonCategory>>(url);
  }
}
