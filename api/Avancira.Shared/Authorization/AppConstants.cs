using System.Collections.ObjectModel;

namespace Avancira.Shared.Authorization;
public static class AppConstants
{
    public static readonly Collection<string> SupportedImageFormats =
    [
        ".jpeg",
        ".jpg",
        ".png"
    ];
    public static readonly string StandardImageFormat = "image/jpeg";
    public static readonly int MaxImageWidth = 1500;
    public static readonly int MaxImageHeight = 1500;
    public static readonly long MaxAllowedSize = 1000000; // Allows Max File Size of 1 Mb.


    public const string EmailAddress = "admin@system.local";
    public const string AdminUserName = "ADMIN";
    public const string DefaultProfilePicture = "assets/defaults/profile-picture.webp";
    public const string DefaultPassword = "Pa$$w0rd!";
}
