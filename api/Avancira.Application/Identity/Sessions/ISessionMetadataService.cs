namespace Avancira.Application.Identity.Sessions
{
    public interface ISessionMetadataService
    {
        Task<SessionMetadata> CollectAsync(HttpContext context, string deviceId, CancellationToken cancellationToken = default);
        Task<SessionMetadata> CollectAsync(string ipAddress, string userAgent, string deviceId, CancellationToken cancellationToken = default);
    }
}
