export interface PagedResult<T> {
    totalResults: number;  // Matches C# TotalResults
    page: number;          // Matches C# Page
    pageSize: number;      // Matches C# PageSize
    totalPages: number;    // Matches C# TotalPages (calculated in C#)
    results: T[];          // The actual paginated data (can be Lessons, Listings, etc.)
  }
  