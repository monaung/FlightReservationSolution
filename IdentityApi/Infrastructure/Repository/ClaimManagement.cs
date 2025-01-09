using IdentityApi.Domain;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;

namespace IdentityApi.Infrastructure.Repository
{
    public class ClaimManagement(UserManager<AppUser> userManager) : IClaim
    {
        public async Task AssignClaims(AppUser user, IEnumerable<Claim> claims)
        {
            await userManager.AddClaimsAsync(user, claims);
            return;
        }

        public async Task<IEnumerable<Claim>> GetClaimsAsync(AppUser user)
        {
            var claims = await userManager.GetClaimsAsync(user);
            return claims.Any() ? claims! : [];
        }
    }
}
