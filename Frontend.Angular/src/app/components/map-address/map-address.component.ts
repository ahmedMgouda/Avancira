/// <reference types="google.maps" />
import { CommonModule } from '@angular/common';
import { AfterViewInit, Component, EventEmitter,Input, Output } from '@angular/core';

import { GoogleMapsService } from '../../services/google-maps.service';

import { Address } from '../../models/user';

@Component({
  selector: 'app-map-address',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './map-address.component.html',
  styleUrls: ['./map-address.component.scss']
})
export class MapAddressComponent implements AfterViewInit {
  @Input() initialAddress: string | null = null;
  @Output() addressSelected = new EventEmitter<Address>();
  selectedAddress: string | null = null;

  constructor(private googleMapsService: GoogleMapsService) { }

  ngAfterViewInit(): void {
    this.googleMapsService.loadGoogleMaps().subscribe({
      next: () => this.initMap(),
      error: (error) => console.error('Google Maps loading error:', error),
    });
  }
  
  initMap(): void {
    // Check if Google Maps is available
    if (typeof google === 'undefined' || !google.maps || !google.maps.Map) {
      console.error('Google Maps API not loaded properly');
      return;
    }

    const mapElement = document.getElementById('map') as HTMLElement;
    if (!mapElement) {
      console.error('Map element not found');
      return;
    }

    const map = new google.maps.Map(mapElement, {
      center: { lat: -33.8688, lng: 151.2093 }, // Default to Sydney
      zoom: 13,
    });
    const input = document.getElementById('search-box') as HTMLInputElement;
    const searchBox = new google.maps.places.SearchBox(input);
    // If initialAddress is set, geocode and update the map
    if (this.initialAddress) {
      this.geocodeAddress(this.initialAddress, map);
    }

    searchBox.addListener('places_changed', () => {
      const places = searchBox.getPlaces();

      if (!places || places.length === 0) {
        console.warn('No places found');
        return;
      }

      const markers: google.maps.Marker[] = [];
      const bounds = new google.maps.LatLngBounds();

      places.forEach((place: google.maps.places.PlaceResult) => {
        if (!place.geometry || !place.geometry.location) {
          console.warn('Returned place contains no geometry');
          return;
        }

        markers.push(
          new google.maps.Marker({
            map,
            title: place.name,
            position: place.geometry.location,
          })
        );

        this.selectedAddress = place.formatted_address || place.name || 'Unknown Address';
        const lat = place.geometry.location.lat();
        const lng = place.geometry.location.lng();
        const addressComponents = place.address_components ?? [];
        const addressData = this.extractAddressComponents(addressComponents, lat, lng);
        this.addressSelected.emit(addressData);

        if (place.geometry.viewport) {
          bounds.union(place.geometry.viewport);
        } else {
          bounds.extend(place.geometry.location);
        }
      });

      map.fitBounds(bounds);
    });
  }

  geocodeAddress(address: string, map: google.maps.Map): void {
    const geocoder = new google.maps.Geocoder();

    geocoder.geocode({ address }, (results, status) => {
      if (status === 'OK' && results && results[0]?.geometry) {
        const location = results[0].geometry.location;
        map.setCenter(location);
        map.setZoom(13);

        new google.maps.Marker({
          map,
          position: location,
        });

        this.selectedAddress = results[0].formatted_address || address;
        const lat = results[0].geometry.location.lat();
        const lng = results[0].geometry.location.lng();
        const addressComponents = results[0].address_components;
        const addressData = this.extractAddressComponents(addressComponents, lat, lng);
        this.addressSelected.emit(addressData);

      } else if (!results) {
        console.warn('No results found for the provided address.');
      } else {
        console.error(`Geocode was not successful for the following reason: ${status}`);
      }
    });
  }

  // Helper function to extract address components
  extractAddressComponents(addressComponents: google.maps.GeocoderAddressComponent[], lat: number, lng: number): Address {
    let streetAddress = '';
    let city = '';
    let state = '';
    let country = '';
    let postalCode = '';

    addressComponents.forEach((component) => {
      const types = component.types;
      const longName = component.long_name;

      if (types.includes('street_address') || types.includes('route')) {
        streetAddress = longName;
      }
      if (types.includes('locality')) {
        city = longName;
      }
      if (types.includes('administrative_area_level_1')) {
        state = longName;
      }
      if (types.includes('country')) {
        country = longName;
      }
      if (types.includes('postal_code')) {
        postalCode = longName;
      }
    });

    return {
      streetAddress,
      city,
      state,
      country,
      postalCode,
      latitude: lat,
      longitude: lng,
      formattedAddress: this.selectedAddress || ''
    };
  }

}
