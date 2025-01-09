using IdentityApi.Domain;
using System.Security.Claims;

namespace IdentityApi.Infrastructure.Repository
{
    public interface IJwtToken
    {
        string GenerateToken(IEnumerable<Claim> claims);
    }
}
