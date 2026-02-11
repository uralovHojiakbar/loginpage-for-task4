using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
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
        public IActionResult Login() => Redirect("/auth/login.html");

        private sealed class RegisterRequest
        {
            public string? Name { get; set; }
            public string? Email { get; set; }
            public string? Password { get; set; }
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register()
        {
            RegisterRequest? req = null;

            if (Request.ContentType?.Contains("application/json", StringComparison.OrdinalIgnoreCase) == true)
            {
                try
                {
                    req = await JsonSerializer.DeserializeAsync<RegisterRequest>(Request.Body,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                }
                catch
                {
                    return BadRequest(new { error = "Invalid JSON payload." });
                }
            }
            else if (Request.HasFormContentType)
            {
                var form = await Request.ReadFormAsync();
                req = new RegisterRequest
                {
                    Name = form["name"],
                    Email = form["email"],
                    Password = form["password"]
                };
            }

            if (req == null) return BadRequest(new { error = "Missing payload." });

            var name = (req.Name ?? "").Trim();
            var email = (req.Email ?? "").Trim();
            var password = req.Password ?? "";

            if (string.IsNullOrWhiteSpace(name) ||
                string.IsNullOrWhiteSpace(email) ||
                string.IsNullOrWhiteSpace(password))
            {
                return BadRequest(new { error = "Name, email and password are required." });
            }

            var user = new User
            {
                Name = name,
                Email = email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
                Status = UserStatus.Unverified
            };

            try
            {
                await _users.CreateAsync(user);
            }
            catch
            {
                return BadRequest(new { error = "Registration failed. Possibly a duplicate e-mail." });
            }

            var verifyUrl = Url.Action("Verify", "Auth", new { id = user.Id }, Request.Scheme);

            // async send (email kelmasa ham register yiqilmaydi)
            _ = Task.Run(async () =>
            {
                try
                {
                    if (!string.IsNullOrWhiteSpace(verifyUrl))
                        await _emails.SendVerificationEmailAsync(user.Email, verifyUrl);
                }
                catch { }
            });

            // ✅ HAR DOIM verifyUrl qaytadi (SMTP ishlamasa ham)
            return Ok(new
            {
                message = "Registered. Verification email sent asynchronously.",
                verifyUrl
            });
        }

        [HttpGet("verify")]
        public async Task<IActionResult> Verify([FromQuery] Guid id)
        {
            var u = await _users.GetByIdAsync(id);
            if (u == null) return NotFound("User not found.");

            var before = u.Status;

            if (u.Status != UserStatus.Blocked)
            {
                u.Status = UserStatus.Active;
                await _users.UpdateAsync(u);
            }

            var after = await _users.GetByIdAsync(id);
            return Content($"Verified. Before={before}, After={after?.Status}");
        }

        [HttpPost("login")]
        public async Task<IActionResult> LoginPost()
        {
            string email = "";
            string password = "";

            if (Request.ContentType?.Contains("application/json", StringComparison.OrdinalIgnoreCase) == true)
            {
                try
                {
                    using var doc = await JsonDocument.ParseAsync(Request.Body);
                    if (doc.RootElement.TryGetProperty("email", out var e))
                        email = (e.GetString() ?? "").Trim();
                    if (doc.RootElement.TryGetProperty("password", out var p))
                        password = p.GetString() ?? "";
                }
                catch
                {
                    return BadRequest(new { error = "Invalid JSON payload." });
                }
            }
            else if (Request.HasFormContentType)
            {
                var form = await Request.ReadFormAsync();
                email = (form["email"].ToString() ?? "").Trim();
                password = form["password"].ToString() ?? "";
            }
            else
            {
                return BadRequest(new { error = "Unsupported content type." });
            }

            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
                return BadRequest(new { error = "Email and password are required." });

            var user = await _users.GetByEmailAsync(email);
            if (user == null) return BadRequest(new { error = "Invalid credentials." });
            if (user.Status == UserStatus.Blocked) return Unauthorized(new { error = "Account blocked." });

            if (user.Status != UserStatus.Active)
                return Unauthorized(new { error = "Account is not active. Please verify your email." });

            if (!BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
                return BadRequest(new { error = "Invalid credentials." });

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
