namespace Avancira.Application.Persistence;
public interface IDbInitializer
{
    Task SeedAsync(CancellationToken cancellationToken);
}
