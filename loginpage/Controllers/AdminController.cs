using loginpage.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace loginpage.Controllers
{
    [Authorize]
    [Route("admin")]
    public class AdminController : Controller
    {
        private readonly IUserService _users;

        public AdminController(IUserService users)
        {
            _users = users;
        }

        // GET /admin/panel
        [HttpGet("panel")]
        public async Task<IActionResult> Panel([FromQuery] string? q)
        {
            var items = await _users.ListSortedByLastLoginDescAsync();

            if (!string.IsNullOrWhiteSpace(q))
            {
                var query = q.Trim().ToLowerInvariant();
                items = items.Where(u =>
                        (u.Name ?? "").ToLowerInvariant().Contains(query) ||
                        (u.Email ?? "").ToLowerInvariant().Contains(query))
                    .ToList();
            }

            return View("Panel", items);
        }

        // POST /admin/bulk/block
        [HttpPost("bulk/block")]
        public async Task<IActionResult> BulkBlock([FromForm] Guid[] ids)
        {
            foreach (var id in ids.Distinct())
                await _users.BlockAsync(id);

            return RedirectToAction(nameof(Panel));
        }

        // POST /admin/bulk/unblock
        [HttpPost("bulk/unblock")]
        public async Task<IActionResult> BulkUnblock([FromForm] Guid[] ids)
        {
            foreach (var id in ids.Distinct())
                await _users.UnblockAsync(id);

            return RedirectToAction(nameof(Panel));
        }

        // POST /admin/bulk/delete
        [HttpPost("bulk/delete")]
        public async Task<IActionResult> BulkDelete([FromForm] Guid[] ids)
        {
            foreach (var id in ids.Distinct())
                await _users.DeleteAsync(id);

            return RedirectToAction(nameof(Panel));
        }

        // POST /admin/delete-unverified
        [HttpPost("delete-unverified")]
        public async Task<IActionResult> DeleteUnverified()
        {
            await _users.DeleteUnverifiedAsync();
            return RedirectToAction(nameof(Panel));
        }

        // POST /admin/logout
        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync();
            return Redirect("/auth/login.html");
        }
    }
}
