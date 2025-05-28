export enum NotificationEvent {
    // Authentication-related events
    ChangePassword,
    ForgotPassword,
    LoginAttempt,

    // Message and chat-related events
    NewMessage,
    ChatRequest,
    MessageRead,

    // Transaction and payment-related events
    PayoutProcessed,
    PaymentFailed,
    PaymentReceived,
    RefundProcessed,

    // Booking or scheduling-related events
    BookingConfirmed,
    BookingCancelled,
    BookingReminder,

    // Review-related events
    NewReviewReceived,

    // General notifications
    ProfileUpdated,
    SystemAlert,
    NewFeatureAnnouncement,
}