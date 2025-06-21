using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Avancira.Domain.Catalog.Enums
{
    public enum NotificationEvent
    {
        // Authentication-related events
        ConfirmEmail,
        ResetPassword,
        ChangePassword,
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

        // Subscription-related events
        SubscriptionCreated,       // User successfully subscribed
        SubscriptionRenewed,       // Subscription was renewed automatically
        SubscriptionCancelled,     // User canceled their subscription
        SubscriptionExpired,       // Subscription ended due to non-renewal
        SubscriptionFailed,        // Payment for subscription renewal failed
        SubscriptionPlanChanged,   // User switched from Monthly ↔ Annual

        // Booking or scheduling-related events
        BookingRequested,
        PropositionResponded,
        BookingConfirmed,
        BookingCancelled,
        BookingReminder,

        // Review-related events
        NewReviewReceived,
        NewRecommendationReceived,

        // General notifications
        ProfileUpdated,
        SystemAlert,
        NewFeatureAnnouncement,
    }
}
