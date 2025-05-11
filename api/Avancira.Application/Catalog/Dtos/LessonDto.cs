using Avancira.Domain.Catalog.Enums;
using Avancira.Domain.Transactions;
using Backend.Domain.Lessons;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Avancira.Application.Catalog.Dtos
{
    public class LessonDto
    {
        public int Id { get; set; }
        public string? Topic { get; set; }
        public DateTime Date { get; set; }
        public TimeSpan Duration { get; set; }
        public decimal Price { get; set; }
        public string? StudentId { get; set; }
        public Guid ListingId { get; set; }
        public TransactionPaymentMethod PaymentMethod { get; set; }
        public LessonStatus? Status { get; set; }
        public string? MeetingToken { get; set; }
        public string? MeetingDomain { get; set; }
        public string? MeetingUrl { get; set; }
        public string? MeetingRoomUrl { get; set; }
        public string? MeetingRoomName { get; set; }
        public string? StudentName { get; set; }
        public string? TutorName { get; set; }
        public string? RecipientName { get; set; }
        public UserRole RecipientRole { get; set; }
        public LessonType Type { get; set; }
        public string? PayPalPaymentId { get; set; }
    }
}
