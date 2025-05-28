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
  private apiUrl = `${environment.apiUrl}/listings`;

  constructor(private http: HttpClient) { }

  createListing(newListing: Listing): Observable<Listing> {
    // Prepare FormData with the image and listing details
    const formData = new FormData();
    formData.append('listingImage', newListing.listingImage as File);
    formData.append('title', newListing.title || '');
    formData.append('aboutLesson', newListing.aboutLesson || '');
    formData.append('aboutYou', newListing.aboutYou || '');
    if (newListing.lessonCategoryId !== null && newListing.lessonCategoryId !== undefined) {
      formData.append('lessonCategoryId', String(newListing.lessonCategoryId));
    }
    if (newListing.lessonCategory !== null && newListing.lessonCategory !== undefined) {
      formData.append('lessonCategory', String(newListing.lessonCategory));
    }
    formData.append('locations', (newListing.locations || []).join(','));
    formData.append('rates.hourly', String(newListing.rates?.hourly || 0));
    formData.append('rates.fiveHours', String(newListing.rates?.fiveHours || 0));
    formData.append('rates.tenHours', String(newListing.rates?.tenHours || 0));

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

  getListing(listingId: number): Observable<Listing> {
    return this.http.get<Listing>(`${this.apiUrl}/${listingId}`);
  }

  updateListingVisibility(listingId: number, isVisible: boolean): Observable<void> {
    const body = { isVisible };

    return this.http.put<void>(`${this.apiUrl}/${listingId}/toggle-visibility`, body);
  }

  updateListingTitle(listingId: number, title: string): Observable<void> {
    return this.http.put<void>(`${this.apiUrl}/${listingId}/update-title`, { title });
  }

  updateListingImage(listingId: number, imageFile: File): Observable<void> {
    const formData = new FormData();
    formData.append('image', imageFile);

    return this.http.put<void>(`${this.apiUrl}/${listingId}/update-image`, formData);
  }

  updateListingLocations(listingId: number, locations: string[]): Observable<void> {
    return this.http.put<void>(`${this.apiUrl}/${listingId}/update-locations`, locations);
  }

  updateListingDescription(listingId: number, aboutLesson: string, aboutYou: string): Observable<void> {
    const body = { aboutLesson, aboutYou };
    return this.http.put<void>(`${this.apiUrl}/${listingId}/update-description`, body);
  }

  updateListingRates(listingId: number, rates: { hourly: number; fiveHours: number; tenHours: number }): Observable<void> {
    return this.http.put<void>(`${this.apiUrl}/${listingId}/update-rates`, rates);
  }

  updateListingCategory(listingId: number, lessonCategoryId: number): Observable<void> {
    return this.http.put<void>(`${this.apiUrl}/${listingId}/update-category`, { lessonCategoryId });
  }

  
  deleteListing(listingId: number): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${listingId}/delete`);
  }
}
