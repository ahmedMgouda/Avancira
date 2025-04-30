public interface IPayPalAccountService
{
    // Create
    Task<bool> ConnectPayPalAccountAsync(string userId, string authCode);
}