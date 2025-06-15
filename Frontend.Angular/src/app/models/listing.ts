import { Address } from "./user";

export interface Listing {
    id: string; //  | null
    isVisible: any;
    // Personal Details
    tutorId: string;
    tutorName: string;
    tutorBio: string;
    // Address
    tutorAddress: Address | null;
    // Lesson Details
    lessonCategory: string | null;
    lessonCategoryId: string | null; // Changed from number to string to match API Guid
    title: string;
    aboutLesson: string;
    aboutYou: string;
    rates: {
      hourly: number;
      fiveHours: number;
      tenHours: number;
    };
    // Media & Social
    listingImagePath: string; //  | null
    listingImage: File | null;
    socialPlatforms: string[];
    // Locations
    locations: string[];
    // Ratings & Metrics
    reviews: number;
    contactedCount: number;
    rating: number | null;
  }
