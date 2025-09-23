using Avancira.Application.Identity.Tokens.Dtos;
using Avancira.Domain.Identity;
using Mapster;

namespace Avancira.Infrastructure.Identity.Tokens.Mappers;

public class SessionMappingConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<UserSession, SessionDto>();
    }
}
