import { NotificationEvent } from "./enums/notification-event";

export interface Notification {
  eventName: NotificationEvent,
  message: string,
  data: any
}
