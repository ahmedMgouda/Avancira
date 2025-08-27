using System.IO;
using Avancira.Application.Identity.Tokens.Dtos;

namespace Avancira.Infrastructure.Auth;

public interface ITokenResponseParser
{
    Task<TokenPair> ParseAsync(Stream stream);
}

