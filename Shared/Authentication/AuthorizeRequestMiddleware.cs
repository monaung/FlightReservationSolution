using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Authentication
{
    internal class AuthorizeRequestMiddleware(ITokenService tokenService) : IMiddleware
    {
        public async Task InvokeAsync(HttpContext context, RequestDelegate next)
        {
            var token = context.Request.Headers.Authorization.FirstOrDefault()!;

            if (string.IsNullOrWhiteSpace(token))
            {
                var _token = token.Split(" ")[1];

                bool isTokenValid = tokenService.ValidateToken(_token);
                if (!isTokenValid)
                {
                    await Unauthorized(context);
                    return;
                }

                var claims = tokenService.GetClaims(_token);
                var identity = new ClaimsIdentity(claims, "Bearer");
                context.User = new ClaimsPrincipal(identity);

               

                await next(context);
            }
            else
                await Unauthorized(context);

            return;
        }

        private async Task Unauthorized(HttpContext context)
        {
            context.Response.StatusCode = 401;
            context.Response.ContentType = "application/json";

            await context.Response.WriteAsJsonAsync(new ProblemDetails
            {
                Detail = "Access token is missing",
                Status = StatusCodes.Status401Unauthorized,
                Title = "You are not authorized to access this resource",
                Type = "Bearer authentication"
            });
        }
    }
}
