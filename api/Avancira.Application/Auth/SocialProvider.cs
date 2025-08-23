using System.Runtime.Serialization;

namespace Avancira.Application.Auth;

public enum SocialProvider
{
    [EnumMember(Value = "google")]
    Google,

    [EnumMember(Value = "facebook")]
    Facebook
}
