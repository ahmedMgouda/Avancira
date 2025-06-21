namespace Avancira.Domain.Messaging;

public enum NotificationEvent
{
    UserRegistered,
    PaymentReceived,
    PaymentFailed,
    SubscriptionCreated,
    SubscriptionExpired,
    SubscriptionCancelled,
    LessonBooked,
    LessonCancelled,
    LessonCompleted,
    MessageReceived,
    SystemMaintenance,
    PasswordReset,
    EmailVerification,
    ProfileUpdated,
    WalletTopUp,
    TransactionCompleted
}
