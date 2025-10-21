import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import { environment } from '../environments/environment';
import { Listing } from '../models/listing';
import { PagedResult } from '../models/paged-result';

@Injectable({
  providedIn: 'root'
})
export class ListingService {
  private readonly apiUrl = `${environment.bffBaseUrl}/api/listings`;

  constructor(private http: HttpClient) { }

  createListing(newListing: Listing): Observable<Listing> {
    // Prepare FormData with the image and listing details
    const formData = new FormData();
    
    // Add image if present
    if (newListing.listingImage) {
      formData.append('listingImage', newListing.listingImage as File);
    }
    
    // Map frontend fields to API expected fields
    formData.append('Name', newListing.title || '');
    formData.append('Description', `${newListing.aboutLesson || ''}\n\nAbout the tutor: ${newListing.aboutYou || ''}`);
    formData.append('HourlyRate', String(newListing.rates?.hourly || 0));
    
    // Handle category - API expects CategoryIds as array of GUIDs
    if (newListing.lessonCategoryId !== null && newListing.lessonCategoryId !== undefined) {
      // API expects CategoryIds as an array, so we append it as an array element
      formData.append('CategoryIds[0]', String(newListing.lessonCategoryId));
    }

    return this.http.post<Listing>(`${this.apiUrl}/create-listing`, formData);
  }


  searchListings(query: string, selectedCategories: string[], page: number = 1, pageSize: number = 10): Observable<PagedResult<Listing>> {
    const params = {
      query: query,
      categories: '',
      page: page.toString(),
      pageSize: pageSize.toString(),
    };

    // Include categories if selected
    if (selectedCategories.length > 0) {
      params.categories = selectedCategories.join(',');
    }

    return this.http.get<PagedResult<Listing>>(`${this.apiUrl}/search`, { params });
  }

  getListings(page: number = 1, pageSize: number = 10): Observable<PagedResult<Listing>> {
    const params = {
      page: page.toString(),
      pageSize: pageSize.toString(),
    };
    return this.http.get<PagedResult<Listing>>(this.apiUrl, { params });
  }

  getListing(listingId: string): Observable<Listing> {
    return this.http.get<Listing>(`${this.apiUrl}/${listingId}`);
  }

  updateListingVisibility(listingId: string, isVisible: boolean): Observable<void> {
    const body = { isVisible };

    return this.http.put<void>(`${this.apiUrl}/${listingId}/toggle-visibility`, body);
  }

  updateListingTitle(listingId: string, title: string): Observable<void> {
    return this.http.put<void>(`${this.apiUrl}/${listingId}/update-title`, { title });
  }

  updateListingImage(listingId: string, imageFile: File): Observable<void> {
    const formData = new FormData();
    formData.append('image', imageFile);

    return this.http.put<void>(`${this.apiUrl}/${listingId}/update-image`, formData);
  }

  updateListingLocations(listingId: string, locations: string[]): Observable<void> {
    return this.http.put<void>(`${this.apiUrl}/${listingId}/update-locations`, locations);
  }

  updateListingDescription(listingId: string, aboutLesson: string, aboutYou: string): Observable<void> {
    const body = { aboutLesson, aboutYou };
    return this.http.put<void>(`${this.apiUrl}/${listingId}/update-description`, body);
  }

  updateListingRates(listingId: string, rates: { hourly: number; fiveHours: number; tenHours: number }): Observable<void> {
    return this.http.put<void>(`${this.apiUrl}/${listingId}/update-rates`, rates);
  }

  updateListingCategory(listingId: string, lessonCategoryId: string): Observable<void> {
    return this.http.put<void>(`${this.apiUrl}/${listingId}/update-category`, { lessonCategoryId });
  }

  
  deleteListing(listingId: string): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${listingId}/delete`);
  }
}
