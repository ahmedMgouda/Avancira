using Avancira.Application.Catalog;

namespace Avancira.Application.Jobs;

public interface IPaymentJobService
{
    Task ProcessMonthlyPaymentsAsync();
    Task ProcessHourlyLessonsAsync();
    Task ProcessDailySubscriptionRenewalsAsync();
}

public class PaymentJobService : IPaymentJobService
{
    private readonly ILessonService _lessonService;
    private readonly IPaymentService _paymentService;
    private readonly ISubscriptionService _subscriptionService;
    private readonly INotificationService _notificationService;

    public PaymentJobService(
        ILessonService lessonService,
        IPaymentService paymentService,
        ISubscriptionService subscriptionService,
        INotificationService notificationService)
    {
        _lessonService = lessonService;
        _paymentService = paymentService;
        _subscriptionService = subscriptionService;
        _notificationService = notificationService;
    }

    public async Task ProcessMonthlyPaymentsAsync()
    {
        Console.WriteLine($"Starting monthly payment processing at {DateTime.UtcNow}");

        try
        {
            // Monthly payment processing logic will be implemented in the Infrastructure layer
            // where we have access to the DbContext and proper logging
            Console.WriteLine("Monthly payment processing logic - this will be handled by the Infrastructure layer");
            Console.WriteLine($"Monthly payment processing completed at {DateTime.UtcNow}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in monthly payment processing: {ex.Message}");
            throw;
        }
    }

    public async Task ProcessHourlyLessonsAsync()
    {
        Console.WriteLine($"Processing past booked lessons at {DateTime.UtcNow}");

        try
        {
            await _lessonService.ProcessPastBookedLessons();
            Console.WriteLine("Successfully processed past booked lessons.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error processing lessons: {ex.Message}");
            throw;
        }
    }

    public async Task ProcessDailySubscriptionRenewalsAsync()
    {
        Console.WriteLine($"Starting subscription renewal processing at {DateTime.UtcNow}");

        try
        {
            // Subscription renewal processing logic will be implemented in the Infrastructure layer
            // where we have access to the DbContext and proper logging
            Console.WriteLine("Subscription renewal processing logic - this will be handled by the Infrastructure layer");
            Console.WriteLine($"Subscription renewal processing completed at {DateTime.UtcNow}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in subscription renewal processing: {ex.Message}");
            throw;
        }
    }
}
