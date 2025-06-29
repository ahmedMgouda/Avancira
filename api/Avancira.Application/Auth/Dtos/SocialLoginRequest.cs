public class SocialLoginRequest
{
    //[Required]
    public string Provider { get; set; } = string.Empty;

    //[Required]
    public string Token { get; set; } = string.Empty;
}
