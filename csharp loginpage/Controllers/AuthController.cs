using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using BCrypt.Net;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using loginpage.Models;
using loginpage.Services;

namespace loginpage.Controllers
{
    [Route("auth")]
    public class AuthController : Controller
    {
        private readonly IUserService _users;
        private readonly IEmailService _emails;

        public AuthController(IUserService users, IEmailService emails)
        {
            _users = users;
            _emails = emails;
        }

        [HttpGet("login")]
        public IActionResult Login() => View(); // create Razor page later

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromForm] string name, [FromForm] string email, [FromForm] string password)
        {
            // note: do not check for existing email in code; storage-level uniqueness required.
            var user = new User
            {
                Name = name ?? "",
                Email = email ?? "",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(password ?? " "),
                Status = UserStatus.Unverified
            };

            try
            {
                await _users.CreateAsync(user);
            }
            catch (Exception ex)
            {
                // important: unique constraint violation will be thrown here; surface friendly message
                return BadRequest(new { error = "Registration failed. Possibly a duplicate email." });
            }

            // send verification e-mail asynchronously (do not block)
            _ = Task.Run(async () =>
            {
                var verifyUrl = Url.Action("Verify", "Auth", new { id = user.Id }, Request.Scheme);
                try
                {
                    await _emails.SendVerificationEmailAsync(user.Email, verifyUrl!);
                }
                catch
                {
                    // nota bene: don't crash registration on email failure; log in real app
                }
            });

            return Ok(new { message = "Registered. Verification email sent asynchronously." });
        }

        [HttpGet("verify")]
        public async Task<IActionResult> Verify([FromQuery] Guid id)
        {
            var u = await _users.GetByIdAsync(id);
            if (u == null) return NotFound();
            if (u.Status != UserStatus.Blocked) // blocked stays blocked
            {
                u.Status = UserStatus.Active;
                await _users.UpdateAsync(u);
            }
            return Content("Verified. You may login.");
        }

        [HttpPost("login")]
        public async Task<IActionResult> LoginPost([FromForm] string email, [FromForm] string password)
        {
            var user = await _users.GetByEmailAsync(email ?? "");
            if (user == null) return BadRequest(new { error = "Invalid credentials." });
            if (user.Status == UserStatus.Blocked) return Unauthorized(new { error = "Account blocked." });

            if (!BCrypt.Net.BCrypt.Verify(password ?? "", user.PasswordHash))
                return BadRequest(new { error = "Invalid credentials." });

            // sign in
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Name),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim("status", user.Status.ToString())
            };
            var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme));
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

            user.LastLoginAt = DateTime.UtcNow;
            await _users.UpdateAsync(user);

            return Ok(new { message = "Logged in." });
        }

        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync();
            return Ok(new { message = "Logged out." });
        }
    }
}