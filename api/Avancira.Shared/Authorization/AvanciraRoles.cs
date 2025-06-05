using System.Collections.ObjectModel;

namespace Avancira.Shared.Authorization;
public static class AvanciraRoles
{
    public const string Admin = nameof(Admin);
    public const string Tutor = nameof(Tutor);
    public const string Student = nameof(Student);

    public static IReadOnlyList<string> DefaultRoles { get; } = new ReadOnlyCollection<string>(new[]
    {
        Admin,
        Tutor,
        Student
    });

    public static bool IsDefault(string roleName) => DefaultRoles.Any(r => r == roleName);
}
