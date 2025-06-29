namespace Avancira.Domain.Catalog.Enums;

 [Flags]
public enum ListingLocationType
{
    None = 0,                   // 0000
    Webcam = 1 << 1,            // 0010
    TutorLocation = 1 << 2,     // 0100
    StudentLocation = 1 << 3,   // 1000
}