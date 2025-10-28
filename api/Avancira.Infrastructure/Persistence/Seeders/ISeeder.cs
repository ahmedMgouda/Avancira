namespace Avancira.Infrastructure.Persistence.Seeders;

public interface ISeeder
{
    string Name { get; }
    Task SeedAsync(CancellationToken cancellationToken = default);
}