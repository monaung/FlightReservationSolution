using IdentityApi.Domain;

namespace IdentityApi.Infrastructure.Repository
{
    public interface IAppUser
    {
        Task<bool> CreateAsync(AppUser user);

        Task<bool> PasswordMatchAsync(AppUser user, string plainPassword);

        Task<AppUser> GetByEmail(string email);
        Task<AppUser> GetById(string userId);

    }
}
