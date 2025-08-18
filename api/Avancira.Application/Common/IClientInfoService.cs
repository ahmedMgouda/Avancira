namespace Avancira.Application.Common;

public interface IClientInfoService
{
    Task<ClientInfo> GetClientInfoAsync();
}
