﻿using IdentityApi.Domain;
using IdentityApi.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;

namespace IdentityApi.Infrastructure.Repository
{
    public interface IRefreshToken
    {
        Task<bool> IsTokenValid(string refreshToken);
        string GenerateToken();
        void AddToken(RefreshToken token);
        void UpdateToken(RefreshToken token);
        Task<RefreshToken> GetRefreshTokenAsync(string refreshToken);
    }

    public class RefreshTokenManagement(AppDbContext context) : IRefreshToken
    {
        public void AddToken(RefreshToken token)
        {
            context.RefreshToken.Add(token);
        }

        public string GenerateToken()
        {
            var guid = Guid.NewGuid().ToString();
            using var sha256 = SHA256.Create();
            byte[] hashByte = sha256.ComputeHash(Encoding.UTF8.GetBytes(guid));
            return Convert.ToBase64String(hashByte);
        }

        public async Task<RefreshToken> GetRefreshTokenAsync(string refreshToken)
        {
            var _token = await context.RefreshToken.AsNoTracking().FirstOrDefaultAsync(x => x.Token == refreshToken);
            return _token ?? null!;
        }

        public async Task<bool> IsTokenValid(string token)
        {
            var _token = await GetRefreshTokenAsync(token);
            if (_token == null) return false;

            return _token.ExpiresAt >= DateTime.UtcNow;
        }

        public void UpdateToken(RefreshToken token)
        {
            context.RefreshToken.Update(token);
        }
    }
}
