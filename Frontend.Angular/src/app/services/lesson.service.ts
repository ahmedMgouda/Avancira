import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import { environment } from '../environments/environment';
import { Lesson } from '../models/lesson';
import { LessonFilter } from '../models/lesson-filter';
import { PagedResult } from '../models/paged-result';
import { Proposition } from '../models/proposition';

@Injectable({
  providedIn: 'root',
})
export class LessonService {
  private readonly apiUrl = `${environment.bffBaseUrl}/api/lessons`;

  constructor(private http: HttpClient) { }

  proposeLesson(lesson: Proposition): Observable<void> {
    // Ensure duration is in "HH:mm:ss" format
    const formattedLesson = {
      ...lesson,
      duration: this.formatDuration(lesson.duration),
    };

    return this.http.post<void>(`${this.apiUrl}/proposeLesson`, formattedLesson);
  }

  getLessons(contactId: string, listingId: string): Observable<{ lessons: PagedResult<Lesson> }> {
    return this.http.get<{ lessons: PagedResult<Lesson> }>(`${this.apiUrl}/${contactId}/${listingId}`);
  }

  getAllLessons(
    page: number = 1, 
    pageSize: number = 10, 
    filters: LessonFilter = {}
  ): Observable<{ lessons: PagedResult<Lesson> }> {
  
    const { dateRange, ...filteredParams } = filters;
  
    // Add startDate & endDate if dateRange exists
    if (dateRange?.length === 2) {
      Object.assign(filteredParams, {
        startDate: dateRange[0].toISOString(),
        endDate: dateRange[1].toISOString(),
      });
    }
  
    // Remove undefined values
    const cleanFilters = Object.fromEntries(
      Object.entries(filteredParams).filter(([_, value]) => value != null)
    );
  
    const params = new HttpParams({ fromObject: { page, pageSize, ...cleanFilters } });
  
    return this.http.get<{ lessons: PagedResult<Lesson> }>(this.apiUrl, { params });
  }

  respondToProposition(propositionId: number, accept: boolean): Observable<void> {
    return this.http.put<void>(`${this.apiUrl}/respondToProposition/${propositionId}`, accept);
  }

  cancelLesson(lessonId: number): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${lessonId}/cancel`);
  }

  private formatDuration(hours: number): string {
    const totalSeconds = hours * 3600; // Convert hours to seconds
    const h = Math.floor(totalSeconds / 3600);
    const m = Math.floor((totalSeconds % 3600) / 60);
    const s = totalSeconds % 60;
    return `${h.toString().padStart(2, '0')}:${m
      .toString()
      .padStart(2, '0')}:${s.toString().padStart(2, '0')}`;
  }
}
