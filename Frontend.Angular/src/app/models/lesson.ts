import { LessonStatus } from "./enums/lesson-status";
import { LessonType } from "./enums/lesson-type";
import { UserRole } from "./enums/user-role";

export interface Lesson {
    recipientRole: UserRole;
    recipientName: string;
    id: number;
    topic: string; // e.g., "Math Basics"
    date: Date;
    price: number;
    duration: string; // e.g., "1 hour"
    status: LessonStatus; // e.g., "Completed", "Upcoming"
    type: LessonType,
    meetingToken: string;
    meetingDomain: string;
    meetingUrl: string;
    meetingRoomName: string;
    meetingRoomUrl: string;
  }
  