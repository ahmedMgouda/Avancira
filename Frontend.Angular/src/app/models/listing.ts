import { Address } from "./user";

  export interface Listing {
    id: number; //  | null
    isVisible: any;
    // Personal Details
    tutorId: string;
    tutorName: string;
    tutorBio: string;
    // Address
    tutorAddress: Address | null;
    // Lesson Details
    lessonCategory: string | null;
    lessonCategoryId: number | null;
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
  