using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using loginpage.Models;
using loginpage.Services;

namespace loginpage.Controllers
{
    [Authorize]
    [Route("admin")]
    public class AdminController : Controller
    {
        private readonly IUserService _users;
        public AdminController(IUserService users) => _users = users;

        [HttpGet("users")]
        public async Task<IActionResult> ListUsers()
        {
            var list = await _users.ListSortedByLastLoginDescAsync();
            return Ok(list); // frontend will render table
        }

        [HttpPost("block")]
        public async Task<IActionResult> Block([FromBody] List<Guid> ids)
        {
            foreach (var id in ids) await _users.BlockAsync(id);
            return Ok(new { message = "Blocked selected users." });
        }

        [HttpPost("unblock")]
        public async Task<IActionResult> Unblock([FromBody] List<Guid> ids)
        {
            foreach (var id in ids) await _users.UnblockAsync(id);
            return Ok(new { message = "Unblocked selected users." });
        }

        [HttpPost("delete")]
        public async Task<IActionResult> Delete([FromBody] List<Guid> ids)
        {
            foreach (var id in ids) await _users.DeleteAsync(id);
            return Ok(new { message = "Deleted selected users." });
        }

        [HttpPost("delete-unverified")]
        public async Task<IActionResult> DeleteUnverified()
        {
            await _users.DeleteUnverifiedAsync();
            return Ok(new { message = "Deleted all unverified users." });
        }
    }
}