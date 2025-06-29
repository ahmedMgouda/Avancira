using System.ComponentModel.DataAnnotations;
using Avancira.Domain.Catalog.Enums;

public class PaymentScheduleDto
{
    [Required]
    public UserPaymentSchedule PaymentSchedule { get; set; }
}