namespace Avancira.Shared.Constants;

public static class SeedDefaults
{
    public static class Roles
    {
        public const string Admin = "Admin";
        public const string Tutor = "Tutor";
        public const string Student = "Student";

        // Helper array if you need to loop through them
        public static readonly string[] All = { Admin, Tutor, Student };
    }

    public static class AdminUser
    {
        public const string Email = "admin@avancira.com";
        public const string Username = "admin";
        public const string Password = "Admin@123456";
        public const string FirstName = "System";
        public const string LastName = "Administrator";
    }

    public static class TutorUser
    {
        public const string Email = "tutor@avancira.com";
        public const string Username = "tutor";
        public const string Password = "Tutor@123456";
        public const string FirstName = "Default";
        public const string LastName = "Tutor";
    }

    public static class StudentUser
    {
        public const string Email = "student@avancira.com";
        public const string Username = "student";
        public const string Password = "Student@123456";
        public const string FirstName = "Default";
        public const string LastName = "Student";
    }
}
