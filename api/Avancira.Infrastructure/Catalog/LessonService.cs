//using Avancira.Application.Catalog.Dtos;
//using Avancira.Domain.Catalog.Enums;
//using Avancira.Domain.Transactions;
//using Avancira.Infrastructure.Persistence;
//using Backend.Domain.Lessons;
//using Microsoft.EntityFrameworkCore;
//using Microsoft.Extensions.Logging;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;

//namespace Avancira.Infrastructure.Catalog
//{
//    public class LessonService : ILessonService
//    {
//        const decimal platformFeeRate = 0.1m;
//        private readonly AvanciraDbContext _dbContext;
//        private readonly INotificationService _notificationService;
//        private readonly IChatService _chatService;
//        private readonly IPaymentService _paymentService;
//        private readonly IJwtTokenService _jwtTokenService;
//        private readonly ILogger<LessonService> _logger;

//        public LessonService(
//            AvanciraDbContext dbContext,
//            INotificationService notificationService,
//            IChatService chatService,
//            IPaymentService paymentService,
//            IJwtTokenService jwtTokenService,
//            ILogger<LessonService> logger
//        )
//        {
//            _dbContext = dbContext;
//            _notificationService = notificationService;
//            _chatService = chatService;
//            _paymentService = paymentService;
//            _jwtTokenService = jwtTokenService;
//            _logger = logger;
//        }

//        public async Task<LessonDto> ProposeLessonAsync(LessonDto lessonDto, string userId)
//        {
//            if (lessonDto.StudentId == null || string.IsNullOrEmpty(lessonDto.StudentId))
//            {
//                lessonDto.StudentId = userId;
//            }

//            // Step 1: Validate User
//            var user = await _dbContext.Users.FindAsync(userId);
//            if (user == null)
//                throw new KeyNotFoundException("User not found.");

//            // Step 2: Get Tutor
//            var tutor = await _dbContext.Listings
//                .Include(l => l.User)
//                .Where(l => l.Id == lessonDto.ListingId)
//                .FirstOrDefaultAsync();

//            if (tutor == null)
//                throw new KeyNotFoundException("Tutor listing not found.");

//            var tutorId = tutor.UserId;

//            // Step 3: Ensure chat exists
//            var chat = _chatService.GetOrCreateChat(lessonDto.StudentId, tutorId ?? string.Empty, lessonDto.ListingId);

//            // Step 4: Create a Transaction
//            var transaction = new Transaction(
//                senderId: userId,
//                amount: lessonDto.Price,
//                platformFee: 0.0m,
//                paymentMethod: lessonDto.PaymentMethod,
//                paymentType: TransactionPaymentType.Lesson,
//                description: "Lesson Payment"
//            );

//            transaction.AssignRecipient(tutorId);
//            transaction.AssignPayPalPaymentId(lessonDto.PayPalPaymentId);
//            //transaction.AssignStripeCustomer(user.StripeCustomerId);

//            await _dbContext.Transactions.AddAsync(transaction);
//            await _dbContext.SaveChangesAsync();

//            // Step 5: Create a Lesson
//            var lesson = Lesson.Create(
//                date: DateTime.SpecifyKind(lessonDto.Date, DateTimeKind.Utc),
//                duration: lessonDto.Duration,
//                hourlyRate: lessonDto.Price,
//                offeredPrice: lessonDto.Price,
//                studentId: lessonDto.StudentId,
//                listingId: lessonDto.ListingId,
//                transactionId: transaction.Id,
//                isStudentInitiated: lessonDto.StudentId == userId
//            );

//            await _dbContext.Lessons.AddAsync(lesson);
//            await _dbContext.SaveChangesAsync();

//            // Step 6: Notify the Tutor
//            var student = await _dbContext.Users
//                .Where(u => u.Id == userId)
//                .Select(u => new { u.FullName })
//                .FirstOrDefaultAsync();

//            if (!string.IsNullOrEmpty(tutorId))
//            {
//                var studentName = student?.FullName?.Trim() ?? "Student";

//                var eventData = new BookingRequestedEvent
//                {
//                    TutorId = tutorId,
//                    StudentId = lessonDto.StudentId,
//                    LessonId = lesson.Id,
//                    Date = lesson.Date,
//                    Duration = lesson.Duration,
//                    Price = lesson.Price,
//                    StudentName = studentName
//                };

//                await _notificationService.NotifyAsync(NotificationEvent.BookingRequested, eventData);
//            }

//            return MapToLessonDto(lesson, userId);
//        }


//        public async Task<PagedResult<LessonDto>> GetLessonsAsync(string contactId, string userId, int listingId, int page, int pageSize)
//        {
//            var queryable = _dbContext.Lessons
//                .Include(p => p.Student)
//                .Include(p => p.Listing).ThenInclude(p => p.User)
//                .Where(p => p.ListingId == listingId)
//                .Where(p => (p.StudentId == contactId && p.Listing.UserId == userId) || (p.StudentId == userId && p.Listing.UserId == contactId))
//                .OrderByDescending(p => p.Date);

//            // Get total count before pagination
//            var totalResults = await queryable.CountAsync();

//            // Apply pagination
//            var lessons = await queryable
//                .Skip((page - 1) * pageSize)
//                .Take(pageSize)
//                .ToListAsync();

//            var results = lessons.Select(lesson => MapToLessonDto(lesson, userId)).ToList();

//            return new PagedResult<LessonDto>(
//                 results: results,
//                 totalResults: totalResults,
//                 page: page,
//                 pageSize: pageSize
//             );
//        }
//        public async Task<PagedResult<LessonDto>> GetAllLessonsAsync(string userId, LessonFilter filters)
//        {
//            var query = _dbContext.Lessons
//                .Include(l => l.Listing)
//                .ThenInclude(l => l.User)
//                .Where(l => l.StudentId == userId || l.Listing.UserId == userId)
//                .AsNoTracking();

//            if (!string.IsNullOrWhiteSpace(filters.RecipientName))
//                query = query.Where(l => (l.Student.FirstName + " " + l.Student.LastName).Contains(filters.RecipientName));

//            if (!string.IsNullOrWhiteSpace(filters.Topic))
//                query = query.Where(l => l.Listing.Name.Contains(filters.Topic));

//            if (filters.StartDate.HasValue)
//                query = query.Where(l => l.Date >= filters.StartDate.Value);

//            if (filters.EndDate.HasValue)
//                query = query.Where(l => l.Date <= filters.EndDate.Value);

//            if (filters.MinPrice.HasValue)
//                query = query.Where(l => l.Price >= filters.MinPrice.Value);

//            if (filters.MaxPrice.HasValue)
//                query = query.Where(l => l.Price <= filters.MaxPrice.Value);

//            if (filters.Status != -1)
//                query = query.Where(l => l.Status == (LessonStatus)filters.Status);

//            var page = Math.Max(filters.Page, 1);
//            var pageSize = Math.Max(filters.PageSize, 10);

//            var totalResults = await query.CountAsync();

//            var results = await query
//                   .OrderBy(l => l.Date)
//                   .Skip((page - 1) * pageSize)
//                   .Take(pageSize)
//                   .Select(lesson => MapToLessonDto(lesson, userId))
//                   .ToListAsync();
//            return new PagedResult<LessonDto>(
//                results: results,
//                totalResults: totalResults,
//                page: page,
//                pageSize: pageSize
//            );
//        }
//        public async Task<PagedResult<LessonDto>> GetAllLessonsAsync(string userId, int page, int pageSize)
//        {
//            var queryable = _dbContext.Lessons
//                .Include(p => p.Student)
//                .Include(p => p.Listing).ThenInclude(p => p.User)
//                .Where(p => p.StudentId == userId || p.Listing.UserId == userId)
//                .OrderByDescending(p => p.Date);

//            // Get total count before pagination
//            var totalResults = await queryable.CountAsync();

//            // Apply pagination
//            var lessons = await queryable
//                .Skip((page - 1) * pageSize)
//                .Take(pageSize)
//                .ToListAsync();

//            var results = lessons.Select(lesson => MapToLessonDto(lesson, userId)).ToList();

//            return new PagedResult<LessonDto>(
//                 results: results,
//                 totalResults: totalResults,
//                 page: page,
//                 pageSize: pageSize
//             );
//        }
//        public async Task<LessonDto> UpdateLessonStatusAsync(int lessonId, bool accept, string userId)
//        {
//            var lesson = await _dbContext.Lessons
//                .Include(l => l.Transaction)
//                .Include(l => l.Student)
//                .Include(l => l.Listing).ThenInclude(l => l.User)
//                .AsTracking()
//                .FirstOrDefaultAsync(l => l.Id == lessonId);

//            if (lesson == null)
//            {
//                throw new KeyNotFoundException("Lesson not found.");
//            }

//            if (accept)
//            {
//                try
//                {
//                    // Process payment using the shared method
//                    var transaction = await _paymentService.CapturePaymentAsync(lesson.TransactionId, lesson.Transaction.PaymentMethod.ToString(), lesson.Listing?.UserId);

//                    lesson.TransactionId = transaction.Id;


//                    // Generate meeting token and URL
//                    var username = lesson.Student?.FullName ?? "Student";
//                    var roomName = lesson.Listing?.Name.Replace(" ", "_") ?? "Lesson";
//                    var meeting = _jwtTokenService.GetMeeting(username, roomName);
//                    lesson.MeetingToken = meeting.Token;
//                    lesson.MeetingDomain = meeting.Domain;
//                    lesson.MeetingUrl = meeting.ServerUrl;
//                    lesson.MeetingRoomName = meeting.RoomName;
//                    lesson.MeetingRoomUrl = meeting.MeetingUrl;
//                    lesson.Status = LessonStatus.Booked;
//                }
//                catch (Exception ex)
//                {
//                    throw new Exception("Failed to process payment: " + ex.Message);
//                }
//            }
//            else
//            {
//                if (lesson.Status == LessonStatus.Booked)
//                {
//                    var isTutor = lesson.Listing.UserId == userId;

//                    if (!isTutor && lesson.StudentId != userId)
//                    {
//                        throw new UnauthorizedAccessException("You are not authorized to cancel this lesson.");
//                    }

//                    // Calculate refund amount
//                    var refundAmount = lesson.Price;
//                    var lessonStartTime = lesson.Date;
//                    var currentTime = DateTime.UtcNow;

//                    decimal retainedAmount = 0;
//                    if (!isTutor && lessonStartTime.Subtract(currentTime).TotalHours <= 24)
//                    {
//                        // If the student cancels less than 1 day before the lesson, retain tutor compensation
//                        var tutorPercentage = lesson.Listing.User.TutorRefundRetention / 100m;
//                        retainedAmount = refundAmount * tutorPercentage;
//                        refundAmount -= retainedAmount; // Refund the remaining amount

//                        _logger.LogInformation($"Partial refund applied. Retained: {retainedAmount:C}, Refunded: {refundAmount:C}.");
//                    }
//                    else
//                    {
//                        // Full refund if canceled more than 1 day before
//                        _logger.LogInformation($"Full refund of {refundAmount:C} applied.");
//                    }

//                    // Perform refund
//                    await _paymentService.RefundPaymentAsync(lesson.TransactionId, lesson.Price, 0); // Fully Refund
//                                                                                                     // await _paymentService.RefundPaymentAsync(lesson.TransactionId ?? -1, refundAmount, retainedAmount);

//                    // Check if there is a retained amount to pay the tutor
//                    if (retainedAmount > 0)
//                    {
//                        try
//                        {
//                            // Temporarily set lesson price to retainedAmount for payment
//                            var originalPrice = lesson.Price;
//                            lesson.Price = retainedAmount;

//                            // Pay the retained amount to the tutor
//                            var transaction = await _paymentService.CapturePaymentAsync(lesson.TransactionId, lesson.Transaction.PaymentMethod.ToString(), lesson.Listing?.UserId);

//                            lesson.TransactionId = transaction.Id;
//                            _dbContext.Lessons.Update(lesson);
//                            await _dbContext.SaveChangesAsync();


//                            // Restore original price for lesson
//                            lesson.Price = originalPrice;

//                            _logger.LogInformation($"Retained amount of {retainedAmount:C} paid to tutor.");
//                        }
//                        catch (Exception ex)
//                        {
//                            throw new Exception("Failed to process retained amount payment: " + ex.Message);
//                        }
//                    }

//                    // Log or notify about the refund
//                    if (isTutor)
//                    {
//                        _logger.LogInformation($"Full refund of {lesson.Price:C} processed for student.");
//                    }
//                    else
//                    {
//                        _logger.LogInformation($"Partial refund of {refundAmount:C} processed. Tutor retained: {lesson.Price - refundAmount:C}.");
//                    }

//                }
//                lesson.Status = LessonStatus.Canceled;
//            }
//            _dbContext.Lessons.Update(lesson);
//            await _dbContext.SaveChangesAsync();

//            // Notify the student
//            var studentId = lesson.StudentId;

//            if (!string.IsNullOrEmpty(studentId))
//            {
//                var tutorName = $"{lesson.Listing.User.FullName}".Trim();
//                var lessonTitle = lesson.Listing.Name ?? "the lesson";
//                var eventData = new PropositionRespondedEvent
//                {
//                    StudentId = studentId,
//                    LessonId = lesson.Id,
//                    Status = lesson.Status,
//                    TutorName = tutorName,
//                    LessonTitle = lessonTitle,
//                    Date = lesson.Date,
//                    Duration = lesson.Duration,
//                    Price = lesson.Price,
//                    MeetingUrl = lesson.MeetingRoomUrl
//                };

//                await _notificationService.NotifyAsync(NotificationEvent.PropositionResponded, eventData);
//            }

//            return MapToLessonDto(lesson, userId);
//        }

//        public async Task ProcessPastBookedLessons()
//        {
//            var currentDate = DateTime.UtcNow;

//            var pastBookedLessons = await _dbContext.Lessons
//                .Include(l => l.Listing)
//                .Include(l => l.Listing.User)
//                .Include(l => l.Student)
//                .Where(l => l.Status == LessonStatus.Booked && l.Date < currentDate)
//                .AsTracking()
//                .ToListAsync();

//            foreach (var lesson in pastBookedLessons)
//            {
//                lesson.Status = LessonStatus.Completed;
//                try
//                {
//                    var tutor = lesson.Listing.User;
//                    if (tutor.PaymentSchedule == UserPaymentSchedule.PerLesson)
//                    {
//                        var platformFee = lesson.Price * platformFeeRate;
//                        var netAmount = lesson.Price - platformFee;
//                        await _paymentService.CreatePayoutAsync(tutor.Id, netAmount, "AUD", tutor.PaymentGateway);
//                        lesson.Status = LessonStatus.Paid;
//                    }
//                }
//                catch (Exception ex)
//                {
//                    // Log the failure but continue processing the rest of the lessons
//                    _logger.LogError(ex, $"Failed to process lesson ID {lesson.Id}. Error: {ex.Message}");
//                }
//                _dbContext.Lessons.Update(lesson);
//            }

//            if (pastBookedLessons.Any())
//            {
//                await _dbContext.SaveChangesAsync();
//            }
//        }
//        public async Task<Lesson> UpdateLessonStatusAsync(int lessonId, string userId)
//        {
//            var lesson = await _dbContext.Lessons
//                .Include(l => l.Transaction)
//                .Include(l => l.Student)
//                .Include(l => l.Listing).ThenInclude(l => l.User)
//                .AsTracking()
//                .FirstOrDefaultAsync(l => l.Id == lessonId);

//            if (lesson == null)
//            {
//                throw new KeyNotFoundException("Lesson not found.");
//            }

//            var isTutor = lesson.Listing.UserId == userId;

//            if (!isTutor && lesson.StudentId != userId)
//            {
//                throw new UnauthorizedAccessException("You are not authorized to cancel this lesson.");
//            }

//            if (lesson.Status != LessonStatus.Booked)
//            {
//                throw new InvalidOperationException("Only booked lessons can be canceled.");
//            }

//            // Calculate refund amount
//            var refundAmount = lesson.Price;
//            var lessonStartTime = lesson.Date;
//            var currentTime = DateTime.UtcNow;

//            decimal retainedAmount = 0;
//            if (!isTutor && lessonStartTime.Subtract(currentTime).TotalHours <= 24)
//            {
//                // If the student cancels less than 1 day before the lesson, retain tutor compensation
//                var tutorPercentage = lesson.Listing.User.TutorRefundRetention / 100m;
//                retainedAmount = refundAmount * tutorPercentage;
//                refundAmount -= retainedAmount; // Refund the remaining amount

//                _logger.LogInformation($"Partial refund applied. Retained: {retainedAmount:C}, Refunded: {refundAmount:C}.");
//            }
//            else
//            {
//                // Full refund if canceled more than 1 day before
//                _logger.LogInformation($"Full refund of {refundAmount:C} applied.");
//            }

//            // Perform refund
//            await _paymentService.RefundPaymentAsync(lesson.TransactionId, lesson.Price, 0); // Fully Refund
//                                                                                             // await _paymentService.RefundPaymentAsync(lesson.TransactionId ?? -1, refundAmount, retainedAmount);

//            // Check if there is a retained amount to pay the tutor
//            if (retainedAmount > 0)
//            {
//                try
//                {
//                    // Temporarily set lesson price to retainedAmount for payment
//                    var originalPrice = lesson.Price;
//                    lesson.Price = retainedAmount;

//                    // Pay the retained amount to the tutor
//                    var transaction = await _paymentService.CapturePaymentAsync(lesson.TransactionId, lesson.Transaction.PaymentMethod.ToString(), lesson.Listing?.UserId);

//                    lesson.TransactionId = transaction.Id;
//                    _dbContext.Lessons.Update(lesson);
//                    await _dbContext.SaveChangesAsync();


//                    // Restore original price for lesson
//                    lesson.Price = originalPrice;

//                    _logger.LogInformation($"Retained amount of {retainedAmount:C} paid to tutor.");
//                }
//                catch (Exception ex)
//                {
//                    throw new Exception("Failed to process retained amount payment: " + ex.Message);
//                }
//            }

//            // Update lesson status
//            lesson.Status = LessonStatus.Canceled;
//            lesson.UpdatedAt = DateTime.UtcNow;
//            _dbContext.Lessons.Update(lesson);
//            await _dbContext.SaveChangesAsync();

//            // Log or notify about the refund
//            if (isTutor)
//            {
//                _logger.LogInformation($"Full refund of {lesson.Price:C} processed for student.");
//            }
//            else
//            {
//                _logger.LogInformation($"Partial refund of {refundAmount:C} processed. Tutor retained: {lesson.Price - refundAmount:C}.");
//            }

//            return lesson;
//        }

//        private static LessonDto MapToLessonDto(Lesson lesson, string userId)
//        {
//            return new LessonDto
//            {
//                //Id = lesson.Id,
//                //Date = lesson.Date,
//                //Duration = lesson.Duration,
//                //Price = lesson.Price,
//                //StudentId = lesson.StudentId,
//                //StudentName = lesson.Student?.FullName ?? "Unkown Student",
//                //TutorName = lesson.Listing?.User?.FullName ?? "Unknown Tutor",
//                //RecipientName = lesson.StudentId == userId
//                //        ? lesson.Listing?.User?.FullName ?? "Unknown Tutor"
//                //        : lesson.Student?.FullName ?? "Unknown Student",
//                //RecipientRole = lesson.StudentId == userId
//                //        ? UserRole.Tutor
//                //        : UserRole.Student,
//                //ListingId = lesson.ListingId,
//                //Type = lesson.Status == LessonStatus.Proposed ? LessonType.Proposition : LessonType.Lesson,
//                //Status = lesson.Status,
//                //Topic = lesson.Listing?.Name ?? "Lesson",
//                //MeetingToken = lesson.MeetingToken,
//                //MeetingDomain = lesson.MeetingDomain,
//                //MeetingRoomName = lesson.MeetingRoomName,
//                //MeetingUrl = lesson.MeetingUrl,
//                //MeetingRoomUrl = lesson.MeetingRoomUrl
//            };
//        }
//    }
//}
