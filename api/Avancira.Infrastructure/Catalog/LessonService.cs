using Avancira.Application.Catalog;
using Avancira.Application.Catalog.Dtos;
using Avancira.Domain.Catalog.Enums;
using Avancira.Domain.Transactions;
using Avancira.Infrastructure.Persistence;
using Avancira.Domain.Lessons;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Avancira.Infrastructure.Catalog
{
    public class LessonService : ILessonService
    {
        const decimal platformFeeRate = 0.1m;
        private readonly AvanciraDbContext _dbContext;
        private readonly INotificationService _notificationService;
        private readonly IChatService _chatService;
        private readonly IPaymentService _paymentService;
        private readonly IJwtTokenService _jwtTokenService;
        private readonly ILogger<LessonService> _logger;

        public LessonService(
            AvanciraDbContext dbContext,
            INotificationService notificationService,
            IChatService chatService,
            IPaymentService paymentService,
            IJwtTokenService jwtTokenService,
            ILogger<LessonService> logger
        )
        {
            _dbContext = dbContext;
            _notificationService = notificationService;
            _chatService = chatService;
            _paymentService = paymentService;
            _jwtTokenService = jwtTokenService;
            _logger = logger;
        }

        public async Task<LessonDto> ProposeLessonAsync(LessonDto lessonDto, string userId)
        {
            try
            {
                if (lessonDto.StudentId == null || string.IsNullOrEmpty(lessonDto.StudentId))
                {
                    lessonDto.StudentId = userId;
                }

                // Step 1: Validate User
                var user = await _dbContext.Users.FindAsync(userId);
                if (user == null)
                    throw new KeyNotFoundException("User not found.");

                // Step 2: Get Tutor
                var tutor = await _dbContext.Listings
                    .Where(l => l.Id == lessonDto.ListingId)
                    .FirstOrDefaultAsync();

                if (tutor == null)
                    throw new KeyNotFoundException("Tutor listing not found.");

                var tutorId = tutor.UserId;

                // Step 3: Ensure chat exists
                var chat = _chatService.GetOrCreateChat(lessonDto.StudentId, tutorId ?? string.Empty, lessonDto.ListingId);

                // Step 4: Create a Transaction
                var transaction = new Transaction(
                    senderId: userId,
                    amount: lessonDto.Price,
                    platformFee: 0.0m,
                    paymentMethod: lessonDto.PaymentMethod,
                    paymentType: TransactionPaymentType.Lesson,
                    description: "Lesson Payment"
                );

                transaction.AssignRecipient(tutorId);
                transaction.AssignPayPalPaymentId(lessonDto.PayPalPaymentId);

                await _dbContext.Transactions.AddAsync(transaction);
                await _dbContext.SaveChangesAsync();

                // Step 5: Create a Lesson
                var lesson = Lesson.Create(
                    date: DateTime.SpecifyKind(lessonDto.Date, DateTimeKind.Utc),
                    duration: lessonDto.Duration,
                    hourlyRate: lessonDto.Price,
                    offeredPrice: lessonDto.Price,
                    studentId: lessonDto.StudentId,
                    listingId: lessonDto.ListingId,
                    transactionId: transaction.Id,
                    isStudentInitiated: lessonDto.StudentId == userId
                );

                await _dbContext.Lessons.AddAsync(lesson);
                await _dbContext.SaveChangesAsync();

                // Step 6: Notify the Tutor
                var student = await _dbContext.Users
                    .Where(u => u.Id == userId)
                    .Select(u => new { u.FirstName, u.LastName })
                    .FirstOrDefaultAsync();

                if (!string.IsNullOrEmpty(tutorId))
                {
                    var studentName = $"{student?.FirstName} {student?.LastName}".Trim();
                    if (string.IsNullOrEmpty(studentName)) studentName = "Student";

                    // Note: Notification events would need to be implemented
                    _logger.LogInformation("Lesson proposed successfully. Lesson ID: {LessonId}", lesson.Id);
                }

                return await MapToLessonDtoAsync(lesson, userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error proposing lesson for user: {UserId}", userId);
                throw;
            }
        }

        public async Task<PagedResult<LessonDto>> GetLessonsAsync(string contactId, string userId, int listingId, int page, int pageSize)
        {
            try
            {
                var listingGuid = new Guid(listingId.ToString().PadLeft(32, '0'));
                
                var queryable = _dbContext.Lessons
                    .Where(p => p.ListingId == listingGuid)
                    .Where(p => (p.StudentId == contactId) || (p.StudentId == userId))
                    .OrderByDescending(p => p.Date);

                // Get total count before pagination
                var totalResults = await queryable.CountAsync();

                // Apply pagination
                var lessons = await queryable
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                var results = new List<LessonDto>();
                foreach (var lesson in lessons)
                {
                    results.Add(await MapToLessonDtoAsync(lesson, userId));
                }

                return new PagedResult<LessonDto>(
                     results: results,
                     totalResults: totalResults,
                     page: page,
                     pageSize: pageSize
                 );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting lessons for contact: {ContactId}, user: {UserId}", contactId, userId);
                throw;
            }
        }

        public async Task<PagedResult<LessonDto>> GetAllLessonsAsync(string userId, LessonFilter filters)
        {
            try
            {
                var query = _dbContext.Lessons
                    .Where(l => l.StudentId == userId)
                    .AsNoTracking();

                if (!string.IsNullOrWhiteSpace(filters.Topic))
                {
                    var listingIds = await _dbContext.Listings
                        .Where(listing => listing.Name.Contains(filters.Topic))
                        .Select(listing => listing.Id)
                        .ToListAsync();
                    query = query.Where(l => listingIds.Contains(l.ListingId));
                }

                if (filters.StartDate.HasValue)
                    query = query.Where(l => l.Date >= filters.StartDate.Value);

                if (filters.EndDate.HasValue)
                    query = query.Where(l => l.Date <= filters.EndDate.Value);

                if (filters.MinPrice.HasValue)
                    query = query.Where(l => l.ActualPrice >= filters.MinPrice.Value);

                if (filters.MaxPrice.HasValue)
                    query = query.Where(l => l.ActualPrice <= filters.MaxPrice.Value);

                if (filters.Status != -1)
                    query = query.Where(l => l.Status == (LessonStatus)filters.Status);

                var page = Math.Max(filters.Page, 1);
                var pageSize = Math.Max(filters.PageSize, 10);

                var totalResults = await query.CountAsync();

                var lessons = await query
                       .OrderBy(l => l.Date)
                       .Skip((page - 1) * pageSize)
                       .Take(pageSize)
                       .ToListAsync();

                var results = new List<LessonDto>();
                foreach (var lesson in lessons)
                {
                    results.Add(await MapToLessonDtoAsync(lesson, userId));
                }

                return new PagedResult<LessonDto>(
                    results: results,
                    totalResults: totalResults,
                    page: page,
                    pageSize: pageSize
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all lessons for user: {UserId}", userId);
                throw;
            }
        }

        public async Task<PagedResult<LessonDto>> GetAllLessonsAsync(string userId, int page, int pageSize)
        {
            try
            {
                var queryable = _dbContext.Lessons
                    .Where(p => p.StudentId == userId)
                    .OrderByDescending(p => p.Date);

                // Get total count before pagination
                var totalResults = await queryable.CountAsync();

                // Apply pagination
                var lessons = await queryable
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                var results = new List<LessonDto>();
                foreach (var lesson in lessons)
                {
                    results.Add(await MapToLessonDtoAsync(lesson, userId));
                }

                return new PagedResult<LessonDto>(
                     results: results,
                     totalResults: totalResults,
                     page: page,
                     pageSize: pageSize
                 );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all lessons for user: {UserId}", userId);
                throw;
            }
        }

        public async Task<LessonDto> UpdateLessonStatusAsync(int lessonId, bool accept, string userId)
        {
            try
            {
                var lesson = await _dbContext.Lessons
                    .AsTracking()
                    .FirstOrDefaultAsync(l => l.Id.GetHashCode() == lessonId);

                if (lesson == null)
                {
                    throw new KeyNotFoundException("Lesson not found.");
                }

                var transaction = await _dbContext.Transactions.FindAsync(lesson.TransactionId);
                var listing = await _dbContext.Listings.FindAsync(lesson.ListingId);

                if (accept)
                {
                    try
                    {
                        // Process payment using the shared method
                        var capturedTransaction = await _paymentService.CapturePaymentAsync(lesson.TransactionId, transaction?.PaymentMethod.ToString() ?? "Unknown", listing?.UserId);

                        // Generate meeting token and URL
                        var student = await _dbContext.Users.FindAsync(lesson.StudentId);
                        var username = $"{student?.FirstName} {student?.LastName}".Trim();
                        if (string.IsNullOrEmpty(username)) username = "Student";
                        
                        var roomName = listing?.Name.Replace(" ", "_") ?? "Lesson";
                        var meeting = _jwtTokenService.GetMeeting(username, roomName);
                        
                        // Update lesson with meeting details
                        lesson.ChangeStatus(LessonStatus.Booked);
                    }
                    catch (Exception ex)
                    {
                        throw new Exception("Failed to process payment: " + ex.Message);
                    }
                }
                else
                {
                    if (lesson.Status == LessonStatus.Booked)
                    {
                        var isTutor = listing?.UserId == userId;

                        if (!isTutor && lesson.StudentId != userId)
                        {
                            throw new UnauthorizedAccessException("You are not authorized to cancel this lesson.");
                        }

                        // Calculate refund amount
                        var refundAmount = lesson.ActualPrice;
                        var lessonStartTime = lesson.Date;
                        var currentTime = DateTime.UtcNow;

                        decimal retainedAmount = 0;
                        if (!isTutor && lessonStartTime.Subtract(currentTime).TotalHours <= 24)
                        {
                            // If the student cancels less than 1 day before the lesson, retain tutor compensation
                            // Default to 20% retention for late cancellations
                            var tutorPercentage = 0.2m; // 20% retention
                            retainedAmount = refundAmount * tutorPercentage;
                            refundAmount -= retainedAmount; // Refund the remaining amount

                            _logger.LogInformation($"Partial refund applied. Retained: {retainedAmount:C}, Refunded: {refundAmount:C}.");
                        }
                        else
                        {
                            // Full refund if canceled more than 1 day before
                            _logger.LogInformation($"Full refund of {refundAmount:C} applied.");
                        }

                        // Perform refund
                        await _paymentService.RefundPaymentAsync(lesson.TransactionId, lesson.ActualPrice, 0); // Fully Refund

                        // Check if there is a retained amount to pay the tutor
                        if (retainedAmount > 0)
                        {
                            try
                            {
                                // Pay the retained amount to the tutor
                                var capturedTransaction = await _paymentService.CapturePaymentAsync(lesson.TransactionId, transaction?.PaymentMethod.ToString() ?? "Unknown", listing?.UserId);

                                _logger.LogInformation($"Retained amount of {retainedAmount:C} paid to tutor.");
                            }
                            catch (Exception ex)
                            {
                                throw new Exception("Failed to process retained amount payment: " + ex.Message);
                            }
                        }

                        // Log or notify about the refund
                        if (isTutor)
                        {
                            _logger.LogInformation($"Full refund of {lesson.ActualPrice:C} processed for student.");
                        }
                        else
                        {
                            _logger.LogInformation($"Partial refund of {refundAmount:C} processed. Tutor retained: {lesson.ActualPrice - refundAmount:C}.");
                        }
                    }
                    lesson.ChangeStatus(LessonStatus.Canceled);
                }

                _dbContext.Lessons.Update(lesson);
                await _dbContext.SaveChangesAsync();

                // Notify the student
                var studentId = lesson.StudentId;

                if (!string.IsNullOrEmpty(studentId))
                {
                    var tutor = await _dbContext.Users.FindAsync(listing?.UserId);
                    var tutorName = $"{tutor?.FirstName} {tutor?.LastName}".Trim();
                    var lessonTitle = listing?.Name ?? "the lesson";
                    
                    _logger.LogInformation("Lesson status updated. Student: {StudentId}, Status: {Status}", studentId, lesson.Status);
                }

                return await MapToLessonDtoAsync(lesson, userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating lesson status for lesson: {LessonId}", lessonId);
                throw;
            }
        }

        public async Task ProcessPastBookedLessons()
        {
            try
            {
                var currentDate = DateTime.UtcNow;

                var pastBookedLessons = await _dbContext.Lessons
                    .Where(l => l.Status == LessonStatus.Booked && l.Date < currentDate)
                    .AsTracking()
                    .ToListAsync();

                foreach (var lesson in pastBookedLessons)
                {
                    lesson.ChangeStatus(LessonStatus.Completed);
                    try
                    {
                        var listing = await _dbContext.Listings.FindAsync(lesson.ListingId);
                        var tutor = await _dbContext.Users.FindAsync(listing?.UserId);
                        
                        // Process payout for completed lesson
                        var platformFee = lesson.ActualPrice * platformFeeRate;
                        var netAmount = lesson.ActualPrice - platformFee;
                        await _paymentService.CreatePayoutAsync(tutor.Id, netAmount, "AUD", tutor.PaymentGateway);
                        lesson.ChangeStatus(LessonStatus.Paid);
                    }
                    catch (Exception ex)
                    {
                        // Log the failure but continue processing the rest of the lessons
                        _logger.LogError(ex, $"Failed to process lesson ID {lesson.Id}. Error: {ex.Message}");
                    }
                    _dbContext.Lessons.Update(lesson);
                }

                if (pastBookedLessons.Any())
                {
                    await _dbContext.SaveChangesAsync();
                    _logger.LogInformation("Processed {Count} past booked lessons", pastBookedLessons.Count);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing past booked lessons");
                throw;
            }
        }

        private async Task<LessonDto> MapToLessonDtoAsync(Lesson lesson, string userId)
        {
            // Get related entities
            var student = await _dbContext.Users.FindAsync(lesson.StudentId);
            var listing = await _dbContext.Listings.FindAsync(lesson.ListingId);
            var tutor = listing != null ? await _dbContext.Users.FindAsync(listing.UserId) : null;

            var studentName = $"{student?.FirstName} {student?.LastName}".Trim();
            if (string.IsNullOrEmpty(studentName)) studentName = "Unknown Student";

            var tutorName = $"{tutor?.FirstName} {tutor?.LastName}".Trim();
            if (string.IsNullOrEmpty(tutorName)) tutorName = "Unknown Tutor";

            return new LessonDto
            {
                Id = lesson.Id.GetHashCode(), // Convert Guid to int for compatibility
                Date = lesson.Date,
                Duration = lesson.Duration,
                Price = lesson.ActualPrice,
                StudentId = lesson.StudentId,
                StudentName = studentName,
                TutorName = tutorName,
                RecipientName = lesson.StudentId == userId ? tutorName : studentName,
                RecipientRole = lesson.StudentId == userId ? UserRole.Tutor : UserRole.Student,
                ListingId = lesson.ListingId,
                Type = lesson.Status == LessonStatus.Proposed ? LessonType.Proposition : LessonType.Lesson,
                Status = lesson.Status,
                Topic = listing?.Name ?? "Lesson",
                MeetingToken = lesson.MeetingToken,
                MeetingUrl = lesson.MeetingUrl,
                MeetingRoomName = lesson.MeetingRoomName
            };
        }
    }
}
