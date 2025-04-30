using Avancira.Application.Catalog.Dtos;
using System.Threading.Tasks;

public interface IJwtTokenService
{
    //Task<string> GenerateTokenAsync(User user);
    Meeting GetMeeting(string userName, string roomName);
}

