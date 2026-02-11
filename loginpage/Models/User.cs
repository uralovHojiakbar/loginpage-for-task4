using System;

namespace loginpage.Models
{
    public enum UserStatus
    {
        Unverified = 0,
        Active = 1,
        Blocked = 2
    }

    public class User
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = "";
        public string Email { get; set; } = "";
        public string PasswordHash { get; set; } = "";
        public UserStatus Status { get; set; } = UserStatus.Unverified;
        public DateTime RegisteredAt { get; set; } = DateTime.UtcNow;
        public DateTime? LastLoginAt { get; set; }
    }
}