using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using loginpage.Services;
using Microsoft.AspNetCore.Authentication;

namespace loginpage.Middlewares
{
    public class EnsureUserActiveMiddleware
    {
        private readonly RequestDelegate _next;

        public EnsureUserActiveMiddleware(RequestDelegate next) => _next = next;

        public async Task InvokeAsync(HttpContext context, IUserService users)
        {
            var path = context.Request.Path.Value ?? "";

            // Excluded endpoints (registration/login/verify and static assets)
            var excludedStarts = new[] { "/auth/login", "/auth/register", "/auth/verify", "/favicon.ico", "/css/", "/js/", "/lib/", "/_framework" };
            if (excludedStarts.Any(s => path.StartsWith(s, StringComparison.OrdinalIgnoreCase)))
            {
                await _next(context);
                return;
            }

            if (context.User?.Identity?.IsAuthenticated == true)
            {
                // get user id from claim
                var idClaim = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (Guid.TryParse(idClaim, out var userId))
                {
                    var user = await users.GetByIdAsync(userId);
                    if (user == null || user.Status == loginpage.Models.UserStatus.Blocked)
                    {
                        // nota bene: if blocked or deleted, sign out and redirect to login
                        await context.SignOutAsync();
                        context.Response.Redirect("/auth/login.html");
                        return;
                    }
                }
                else
                {
                    await context.SignOutAsync();
                    context.Response.Redirect("/auth/login.html");
                    return;
                }
            }

            await _next(context);
        }
    }
}