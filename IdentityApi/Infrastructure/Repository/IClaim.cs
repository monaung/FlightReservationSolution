using IdentityApi.Domain;
using System.Security.Claims;

namespace IdentityApi.Infrastructure.Repository
{
    public interface IClaim
    {
        Task<IEnumerable<Claim>> GetClaimsAsync(AppUser user);

        Task AssignClaims(AppUser user, IEnumerable<Claim> claims);
    }
}
