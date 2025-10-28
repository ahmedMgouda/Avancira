namespace Avancira.Infrastructure.Persistence;

public interface IDatabaseInitializer
{
    Task InitializeAsync(CancellationToken cancellationToken = default);
    Task<bool> IsInitializedAsync(CancellationToken cancellationToken = default);
}