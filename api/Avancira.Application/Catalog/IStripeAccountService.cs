public interface IStripeAccountService
{
    // Create
    Task<string> ConnectStripeAccountAsync(string userId);
}