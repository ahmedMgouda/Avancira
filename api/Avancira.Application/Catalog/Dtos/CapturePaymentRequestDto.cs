using Avancira.Domain.Transactions;

public class CapturePaymentRequestDto
{
    public string Gateway { get; set; }
    public string PaymentId { get; set; }

    public int TransactionId { get; set; }
    public TransactionPaymentMethod PaymentMethod { get; set; }

    public CapturePaymentRequestDto()
    {
        Gateway = string.Empty;
        PaymentId = string.Empty;
    }
}