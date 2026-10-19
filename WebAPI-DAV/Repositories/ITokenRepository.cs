using Microsoft.AspNetCore.Identity;

namespace WebAPI_DAV.Repositories
{
    public interface ITokenRepository
    {
        string CreateJWTToken(IdentityUser user, List<string> roles);
    }
}