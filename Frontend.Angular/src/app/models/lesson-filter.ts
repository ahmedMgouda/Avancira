export interface LessonFilter {
    recipientName?: string;
    topic?: string;
    dateRange?: Date[];
    startDate?: string;
    endDate?:string;
    minDuration? :number;
    maxDuration? :number;
    minPrice?: number;
    maxPrice?: number;
    status?: number;
}
